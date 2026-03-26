using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Functorium.SourceGenerators.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using static Functorium.SourceGenerators.Abstractions.Constants;
using static Functorium.SourceGenerators.Abstractions.Selectors;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)  // ⚠️ 디버깅 필요 시 true로 변경 (자세한 내용: DEBUGGING_SOURCE_GENERATOR.md 참조)
{
    private const string AttributeName = "GenerateObservablePort";
    private const string AttributeNamespace = "Functorium.Adapters.SourceGenerators";
    private const string FullyQualifiedAttributeName = $"{AttributeNamespace}.{AttributeName}Attribute";
    private const string ObservablePortIgnoreAttributeFullName = "Functorium.Adapters.SourceGenerators.ObservablePortIgnoreAttribute";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor DuplicateParameterTypeDiagnostic = new(
        id: "FUNCTORIUM001",
        title: "Duplicate parameter types in observable constructor",
        messageFormat: "Observable constructor for '{0}' contains multiple parameters of the same type '{1}'. This may cause issues with dependency injection resolution.",
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        //
        // [GenerateObservablePort] 속성이 붙은 "클래스"만 대상으로 필터링
        // 속성은 Functorium 라이브러리에 정의되어 있음
        //
        return context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: IsClass,
                transform: MapToObservableClassInfo)
            .Where(x => x != ObservableClassInfo.None);
    }

    private static ObservableClassInfo MapToObservableClassInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        // 클래스가 없을 때
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return ObservableClassInfo.None;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 클래스가 있을 때

        // 클래스 이름과 네임스페이스
        string className = classSymbol.Name;
        string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToString();

        // IObservablePort를 상속받은 모든 인터페이스의 메서드를 직접 추출
        // 클래스 구현을 찾을 필요 없이 인터페이스 정의에서 바로 가져옴
        var methods = classSymbol.AllInterfaces
            .Where(ImplementsIObservablePort)
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Where(m => !m.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == ObservablePortIgnoreAttributeFullName))
            .Where(m => m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)
                .Contains("FinT<", StringComparison.Ordinal))
            .Select(m => new MethodInfo(
                m.Name,
                m.Parameters.Select(p =>
                {
                    string typeFullName = p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
                    bool isCollection = CollectionTypeHelper.IsCollectionType(typeFullName);
                    bool isComplex = !isCollection && CollectionTypeHelper.IsComplexType(p.Type);
                    bool needsToString = !isCollection && !isComplex
                        && CollectionTypeHelper.ImplementsValueObjectOrEntityId(p.Type);
                    return new ParameterInfo(p.Name, typeFullName, p.RefKind, isComplex, needsToString);
                }).ToList(),
                m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
            .ToList();

        // IObservablePort를 구현하지 않은 경우 (메서드가 없음) - Observable 생성하지 않음
        if (methods.Count == 0)
        {
            return ObservableClassInfo.None;
        }

        // 생성자 파라미터 추출
        // 우선순위: 1. 타겟 클래스 자체의 생성자, 2. 부모 클래스의 생성자
        var baseConstructorParameters = ConstructorParameterExtractor.ExtractParameters(classSymbol);

        // 원본 소스 위치 (IDE 진단용)
        Location? location = context.TargetNode.GetLocation();

        return new ObservableClassInfo(@namespace, className, methods, baseConstructorParameters, location);
    }

    /// <summary>
    /// 인터페이스가 IObservablePort를 상속받았는지 확인합니다.
    /// </summary>
    /// <param name="interfaceSymbol">확인할 인터페이스 심볼</param>
    /// <returns>IObservablePort를 상속받았으면 true, 아니면 false</returns>
    private static bool ImplementsIObservablePort(INamedTypeSymbol interfaceSymbol)
    {
        // IObservablePort 자체인지 확인
        if (interfaceSymbol.Name == "IObservablePort")
        {
            return true;
        }

        // IObservablePort를 상속받은 인터페이스인지 확인
        return interfaceSymbol.AllInterfaces.Any(i => i.Name == "IObservablePort");
    }

    // 매핑된 ObservableClassInfo로부터 소스 파일을 생성합니다.
    private static void Generate(SourceProductionContext context, ImmutableArray<ObservableClassInfo> observableClasses)
    {
        foreach (var observableClass in observableClasses)
        {
            // 생성자 파라미터 타입 중복 체크
            var allParameters = new List<ParameterInfo>();
            allParameters.AddRange(observableClass.BaseConstructorParameters);

            // Observable 클래스 생성자 파라미터 (ActivitySource, ILogger, IMeterFactory, IOptions<OpenTelemetryOptions>)
            allParameters.Add(new ParameterInfo("activitySource", "global::System.Diagnostics.ActivitySource", RefKind.None));
            allParameters.Add(new ParameterInfo("logger", $"global::Microsoft.Extensions.Logging.ILogger<{observableClass.Namespace}.{observableClass.ClassName}Observable>", RefKind.None));
            allParameters.Add(new ParameterInfo("meterFactory", "global::System.Diagnostics.Metrics.IMeterFactory", RefKind.None));
            allParameters.Add(new ParameterInfo("openTelemetryOptions", "global::Microsoft.Extensions.Options.IOptions<global::Functorium.Adapters.Observabilities.OpenTelemetryOptions>", RefKind.None));

            // 동일한 타입의 파라미터가 있는지 체크
            var duplicateTypes = allParameters
                .GroupBy(p => p.Type)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTypes.Any())
            {
                // IDE 친화적 진단 리포트 (원본 소스 파일 위치 표시)
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateParameterTypeDiagnostic,
                    observableClass.Location,
                    observableClass.ClassName,
                    string.Join(", ", duplicateTypes)));

                continue;  // 에러 시 코드 생성 건너뛰기
            }
            else
            {
                // 정상적인 Observable 클래스 생성
                StringBuilder sb = new();
                string source = GenerateObservableClassSource(observableClass, sb);

                // 네임스페이스의 마지막 부분 추출 (예: Observability.Adapters.Infrastructure.Repositories -> Repositories)
                string namespaceSuffix = string.Empty;
                if (!string.IsNullOrEmpty(observableClass.Namespace))
                {
                    var lastDotIndex = observableClass.Namespace.LastIndexOf('.');
                    if (lastDotIndex >= 0)
                    {
                        namespaceSuffix = observableClass.Namespace.Substring(lastDotIndex + 1) + ".";
                    }
                }

                context.AddSource(
                    $"{namespaceSuffix}{observableClass.ClassName}Observable.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateObservableClassSource(ObservableClassInfo classInfo, StringBuilder sb)
    {
        sb.Append(Header)
            .AppendLine()
            .AppendLine("using System.Diagnostics;")
            .AppendLine("using System.Diagnostics.Metrics;")
            .AppendLine("using Functorium.Adapters.Observabilities;")
            .AppendLine("using Functorium.Adapters.Observabilities.Naming;")
            .AppendLine("using Functorium.Domains.Observabilities;")
            .AppendLine()
            .AppendLine("using LanguageExt;")
            .AppendLine("using Microsoft.Extensions.Logging;")
            .AppendLine("using Microsoft.Extensions.Options;")
            .AppendLine()
            .AppendLine($"namespace {classInfo.Namespace};")
            .AppendLine()
            .AppendLine($"public class {classInfo.ClassName}Observable : {classInfo.ClassName}")
            .AppendLine("{")
            .AppendLine("    private readonly ActivitySource _activitySource;")
            .AppendLine($"    private readonly ILogger<{classInfo.ClassName}Observable> _logger;")
            .AppendLine()
            .AppendLine("    // Metrics")
            .AppendLine("    private readonly Counter<long> _requestCounter;")
            .AppendLine("    private readonly Counter<long> _responseCounter;")
            .AppendLine("    private readonly Histogram<double> _durationHistogram;")
            .AppendLine()
            .AppendLine($"    private const string RequestHandler = nameof({classInfo.ClassName});")
            .AppendLine()
            .AppendLine("    private readonly string _requestCategoryLowerCase;")
            .AppendLine()
            .AppendLine("    private readonly bool _isDebugEnabled;")
            .AppendLine("    private readonly bool _isInformationEnabled;")
            .AppendLine("    private readonly bool _isWarningEnabled;")
            .AppendLine("    private readonly bool _isErrorEnabled;")
            .AppendLine()
            .Append($"    public {classInfo.ClassName}Observable(")
            .AppendLine()
            .AppendLine("        ActivitySource activitySource,")
            .AppendLine($"        ILogger<{classInfo.ClassName}Observable> logger,")
            .AppendLine("        IMeterFactory meterFactory,")
            .Append("        IOptions<OpenTelemetryOptions> openTelemetryOptions");

        string baseParams = GenerateBaseConstructorParameters(classInfo.BaseConstructorParameters);
        if (!string.IsNullOrEmpty(baseParams))
        {
            sb.Append(baseParams);
        }

        sb.Append(")");

        string baseCall = GenerateBaseConstructorCall(classInfo.BaseConstructorParameters);
        if (!string.IsNullOrEmpty(baseCall))
        {
            sb.AppendLine()
                .Append(baseCall);
        }
        else
        {
            sb.AppendLine();
        }

        sb.AppendLine()
            .AppendLine("    {")
            .AppendLine("        global::System.ArgumentNullException.ThrowIfNull(activitySource);")
            .AppendLine("        global::System.ArgumentNullException.ThrowIfNull(meterFactory);")
            .AppendLine("        global::System.ArgumentNullException.ThrowIfNull(openTelemetryOptions);")
            .AppendLine()
            .AppendLine("        _activitySource = activitySource;")
            .AppendLine("        _logger = logger;")
            .AppendLine()
            .AppendLine("        // RequestCategory 캐싱 (성능 최적화: 한 번만 ToLowerInvariant 호출)")
            .AppendLine("        _requestCategoryLowerCase = this.RequestCategory?.ToLowerInvariant() ?? ObservabilityNaming.Categories.Unknown;")
            .AppendLine()
            .AppendLine("        // Meter 및 Metrics 초기화")
            .AppendLine("        // Meter 이름: {service.namespace}.adapter.{category}")
            .AppendLine("        string serviceNamespace = openTelemetryOptions.Value.ServiceNamespace;")
            .AppendLine("        string meterName = $\"{serviceNamespace}.adapter.{_requestCategoryLowerCase}\";")
            .AppendLine("        var meter = meterFactory.Create(meterName);")
            .AppendLine("        _requestCounter = meter.CreateCounter<long>($\"adapter.{_requestCategoryLowerCase}.requests\", \"{request}\", \"Total number of adapter requests\");")
            .AppendLine("        _responseCounter = meter.CreateCounter<long>($\"adapter.{_requestCategoryLowerCase}.responses\", \"{response}\", \"Total number of adapter responses\");")
            .AppendLine("        _durationHistogram = meter.CreateHistogram<double>($\"adapter.{_requestCategoryLowerCase}.duration\", \"s\", \"Duration of adapter execution in seconds\");")
            .AppendLine()
            .AppendLine("        _isDebugEnabled = logger.IsEnabled(LogLevel.Debug);")
            .AppendLine("        _isInformationEnabled = logger.IsEnabled(LogLevel.Information);")
            .AppendLine("        _isWarningEnabled = logger.IsEnabled(LogLevel.Warning);")
            .AppendLine("        _isErrorEnabled = logger.IsEnabled(LogLevel.Error);")
            .AppendLine("    }")
            .AppendLine();

        // 헬퍼 메서드들 추가
        GenerateHelperMethods(sb, classInfo);

        // 메서드들 생성
        foreach (var method in classInfo.Methods)
        {
            GenerateMethod(sb, classInfo, method);
        }

        sb.AppendLine("}")
            .AppendLine()
            .AppendLine($"internal static class {classInfo.ClassName}ObservableLoggers")
            .AppendLine("{");

        // 로깅 확장 메서드들 생성
        foreach (var method in classInfo.Methods)
        {
            GenerateLoggingMethods(sb, classInfo, method);
        }

        sb.AppendLine("}")
            .AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// 부모 생성자 파라미터 선언을 생성합니다.
    /// </summary>
    private static string GenerateBaseConstructorParameters(List<ParameterInfo> baseConstructorParameters)
    {
        if (baseConstructorParameters.Count == 0)
        {
            return string.Empty;
        }

        var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);

        var parameters = resolvedParams
            .Select(p => $",\n        {p.Original.Type} {p.ResolvedName}")
            .ToList();

        return string.Join("", parameters);
    }

    /// <summary>
    /// 부모 생성자 호출 코드를 생성합니다.
    /// </summary>
    private static string GenerateBaseConstructorCall(List<ParameterInfo> baseConstructorParameters)
    {
        if (baseConstructorParameters.Count == 0)
        {
            return string.Empty;
        }

        var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);
        var parameterNames = resolvedParams.Select(p => p.ResolvedName);

        return $"        : base({string.Join(", ", parameterNames)})";
    }

    /// <summary>
    /// FinT&lt;A, B&gt; 형태에서 실제 반환 타입 B를 추출합니다.
    /// </summary>
    private static string ExtractActualReturnType(string returnType)
    {
        return TypeExtractor.ExtractSecondTypeParameter(returnType);
    }

    private static void GenerateHelperMethods(StringBuilder sb, ObservableClassInfo classInfo)
    {
        sb.AppendLine("    private static global::LanguageExt.IO<A> FinTToIO<A>(global::LanguageExt.FinT<global::LanguageExt.IO, A> finT) =>")
            .AppendLine("        ((global::LanguageExt.IO<global::LanguageExt.IO<A>>)finT.Match(")
            .AppendLine("            Succ: value => global::LanguageExt.IO.pure(value),")
            .AppendLine("            Fail: global::LanguageExt.IO.fail<A>")
            .AppendLine("        )).Flatten();")
            .AppendLine()
            .AppendLine("    private global::LanguageExt.IO<A> ExecuteWithSpan<A>(")
            .AppendLine("            string requestHandler,")
            .AppendLine("            string requestHandlerMethod,")
            .AppendLine("            global::LanguageExt.IO<A> operation,")
            .AppendLine("            global::System.Func<global::LanguageExt.IO<global::LanguageExt.Unit>> requestLog,")
            .AppendLine("            global::System.Action<string, string, A, double> responseLogSuccess,")
            .AppendLine("            global::System.Action<string, string, global::LanguageExt.Common.Error, double> responseLogFailure,")
            .AppendLine("            long startTimestamp)")
            .AppendLine("    {")
            .AppendLine("        global::Functorium.Adapters.Observabilities.ObservableSignalScope? observableSignalScope = null;")
            .AppendLine("        return AcquireActivity(requestHandler, requestHandlerMethod)")
            .AppendLine("            .Bracket(")
            .AppendLine("                Use: activity =>")
            .AppendLine("                    from _logged in requestLog()")
            .AppendLine("                    from _scope in global::LanguageExt.IO.lift(() =>")
            .AppendLine("                    {")
            .AppendLine("                        observableSignalScope = global::Functorium.Adapters.Observabilities.ObservableSignalScope.Begin(")
            .AppendLine("                            _logger, ObservabilityNaming.Layers.Adapter, _requestCategoryLowerCase,")
            .AppendLine("                            requestHandler, requestHandlerMethod);")
            .AppendLine("                        return global::LanguageExt.Unit.Default;")
            .AppendLine("                    })")
            .AppendLine("                    from result in ExecuteOperationWithErrorHandling(")
            .AppendLine("                        requestHandler,")
            .AppendLine("                        requestHandlerMethod,")
            .AppendLine("                        operation,")
            .AppendLine("                        activity,")
            .AppendLine("                        responseLogSuccess,")
            .AppendLine("                        responseLogFailure,")
            .AppendLine("                        startTimestamp)")
            .AppendLine("                    select result,")
            .AppendLine("                Fin: activity => global::LanguageExt.IO.lift(() =>")
            .AppendLine("                {")
            .AppendLine("                    observableSignalScope?.Dispose();")
            .AppendLine("                    activity?.Dispose();")
            .AppendLine("                    return global::LanguageExt.Unit.Default;")
            .AppendLine("                }));")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    private global::LanguageExt.IO<A> ExecuteOperationWithErrorHandling<A>(")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        global::LanguageExt.IO<A> operation,")
            .AppendLine("        Activity? activity,")
            .AppendLine("        global::System.Action<string, string, A, double> responseLogSuccess,")
            .AppendLine("        global::System.Action<string, string, global::LanguageExt.Common.Error, double> responseLogFailure,")
            .AppendLine("        long startTimestamp) =>")
            .AppendLine("        operation")
            .AppendLine("            .Bind(result =>")
            .AppendLine("                from _ in global::LanguageExt.IO.lift(() =>")
            .AppendLine("                {")
            .AppendLine("                    double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);")
            .AppendLine("                    responseLogSuccess(requestHandler, requestHandlerMethod, result, elapsed);")
            .AppendLine("                    RecordActivitySuccess(activity, requestHandlerMethod, elapsed);")
            .AppendLine("                    return global::LanguageExt.Unit.Default;")
            .AppendLine("                })")
            .AppendLine("                select result)")
            .AppendLine("            .IfFail(error =>")
            .AppendLine("                HandleOperationFailure<A>(requestHandler, requestHandlerMethod, error, activity, startTimestamp, responseLogFailure));")
            .AppendLine()
            .AppendLine("    private global::LanguageExt.IO<A> HandleOperationFailure<A>(")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        global::LanguageExt.Common.Error error,")
            .AppendLine("        Activity? activity,")
            .AppendLine("        long startTimestamp,")
            .AppendLine("        global::System.Action<string, string, global::LanguageExt.Common.Error, double> responseLogFailure)")
            .AppendLine("    {")
            .AppendLine("        double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);")
            .AppendLine("        responseLogFailure(requestHandler, requestHandlerMethod, error, elapsed);")
            .AppendLine("        RecordActivityFailure(activity, requestHandlerMethod, error, elapsed);")
            .AppendLine("        return global::LanguageExt.IO.fail<A>(error);")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    private global::LanguageExt.IO<Activity?> AcquireActivity(string requestHandler, string requestHandlerMethod) =>")
            .AppendLine("        global::LanguageExt.IO.lift(() =>")
            .AppendLine("        {")
            .AppendLine("            string operationName = ObservabilityNaming.Spans.OperationName(")
            .AppendLine("                ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                _requestCategoryLowerCase,")
            .AppendLine("                requestHandler,")
            .AppendLine("                requestHandlerMethod);")
            .AppendLine()
            .AppendLine("            // TagList: 구조체로 스택에 할당되어 GC 부담 최소화")
            .AppendLine("            TagList tags = new()")
            .AppendLine("            {")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },")
            .AppendLine($"                {{ ObservabilityNaming.CustomAttributes.RequestCategoryName, _requestCategoryLowerCase }},")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestHandlerName, requestHandler },")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod }")
            .AppendLine("            };")
            .AppendLine()
            .AppendLine("            // Activity.Current를 부모로 사용 (표준 OpenTelemetry 동작)")
            .AppendLine("            var parentContext = Activity.Current?.Context ?? default;")
            .AppendLine("            var activity = _activitySource.StartActivity(")
            .AppendLine("                operationName,")
            .AppendLine("                ActivityKind.Internal,")
            .AppendLine("                parentContext,")
            .AppendLine("                tags);")
            .AppendLine()
            .AppendLine("            // Metrics 기록")
            .AppendLine("            TagList metricTags = new()")
            .AppendLine("            {")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },")
            .AppendLine($"                {{ ObservabilityNaming.CustomAttributes.RequestCategoryName, _requestCategoryLowerCase }},")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestHandlerName, requestHandler },")
            .AppendLine("                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod }")
            .AppendLine("            };")
            .AppendLine("            _requestCounter.Add(1, metricTags);")
            .AppendLine()
            .AppendLine("            return activity;")
            .AppendLine("        });")
            .AppendLine()
            .AppendLine("    private void RecordActivitySuccess(Activity? activity, string requestHandlerMethod, double elapsed)")
            .AppendLine("    {")
            .AppendLine("        TagList metricTags = new()")
            .AppendLine("        {")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },")
            .AppendLine($"            {{ ObservabilityNaming.CustomAttributes.RequestCategoryName, _requestCategoryLowerCase }},")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestHandlerName, RequestHandler },")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod },")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success }")
            .AppendLine("        };")
            .AppendLine("        _responseCounter.Add(1, metricTags);")
            .AppendLine("        _durationHistogram.Record(elapsed, metricTags);")
            .AppendLine()
            .AppendLine("        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);")
            .AppendLine("        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);")
            .AppendLine("        activity?.SetStatus(ActivityStatusCode.Ok);")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    private void RecordActivityFailure(Activity? activity, string requestHandlerMethod, global::LanguageExt.Common.Error error, double elapsed)")
            .AppendLine("    {")
            .AppendLine("        var (errorType, errorCode) = GetErrorInfo(error);")
            .AppendLine()
            .AppendLine("        TagList metricTags = new()")
            .AppendLine("        {")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },")
            .AppendLine($"            {{ ObservabilityNaming.CustomAttributes.RequestCategoryName, _requestCategoryLowerCase }},")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestHandlerName, RequestHandler },")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod },")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure },")
            .AppendLine("            { ObservabilityNaming.OTelAttributes.ErrorType, errorType },")
            .AppendLine("            { ObservabilityNaming.CustomAttributes.ErrorCode, errorCode }")
            .AppendLine("        };")
            .AppendLine("        _responseCounter.Add(1, metricTags);")
            .AppendLine("        _durationHistogram.Record(elapsed, metricTags);")
            .AppendLine()
            .AppendLine("        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);")
            .AppendLine("        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);")
            .AppendLine("        activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);")
            .AppendLine("        activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);")
            .AppendLine("        activity?.SetStatus(ActivityStatusCode.Error, $\"{errorType}: {errorCode}\");")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    /// <summary>")
            .AppendLine("    /// 에러 정보(타입, 코드)를 추출합니다.")
            .AppendLine("    /// </summary>")
            .AppendLine("    private static (string ErrorType, string ErrorCode) GetErrorInfo(global::LanguageExt.Common.Error error)")
            .AppendLine("    {")
            .AppendLine("        return error switch")
            .AppendLine("        {")
            .AppendLine("            // ManyErrors - 복합 에러")
            .AppendLine("            global::LanguageExt.Common.ManyErrors many => (")
            .AppendLine("                ErrorType: ObservabilityNaming.ErrorTypes.Aggregate,")
            .AppendLine("                ErrorCode: GetPrimaryErrorCode(many)")
            .AppendLine("            ),")
            .AppendLine("            // IHasErrorCode - ErrorCode가 있는 에러 (IsExceptional로 타입 구분)")
            .AppendLine("            global::Functorium.Abstractions.Errors.IHasErrorCode hasErrorCode => (")
            .AppendLine("                ErrorType: error.IsExceptional")
            .AppendLine("                    ? ObservabilityNaming.ErrorTypes.Exceptional")
            .AppendLine("                    : ObservabilityNaming.ErrorTypes.Expected,")
            .AppendLine("                ErrorCode: hasErrorCode.ErrorCode")
            .AppendLine("            ),")
            .AppendLine("            // Fallback - 알 수 없는 에러 타입")
            .AppendLine("            _ => (")
            .AppendLine("                ErrorType: error.IsExceptional")
            .AppendLine("                    ? ObservabilityNaming.ErrorTypes.Exceptional")
            .AppendLine("                    : ObservabilityNaming.ErrorTypes.Expected,")
            .AppendLine("                ErrorCode: error.GetType().Name")
            .AppendLine("            )")
            .AppendLine("        };")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    /// <summary>")
            .AppendLine("    /// ManyErrors에서 대표 에러 코드를 선정합니다.")
            .AppendLine("    /// 우선순위: Exceptional > First > \"ManyErrors\"")
            .AppendLine("    /// </summary>")
            .AppendLine("    private static string GetPrimaryErrorCode(global::LanguageExt.Common.ManyErrors many)")
            .AppendLine("    {")
            .AppendLine("        // 1순위: Exceptional 에러 (시스템 에러가 더 심각)")
            .AppendLine("        foreach (var e in many.Errors)")
            .AppendLine("        {")
            .AppendLine("            if (e.IsExceptional)")
            .AppendLine("                return GetErrorCode(e);")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        // 2순위: 첫 번째 에러")
            .AppendLine("        return many.Errors.Head.Match(")
            .AppendLine("            Some: GetErrorCode,")
            .AppendLine("            None: () => nameof(global::LanguageExt.Common.ManyErrors));")
            .AppendLine("    }")
            .AppendLine()
            .AppendLine("    /// <summary>")
            .AppendLine("    /// 단일 에러에서 에러 코드를 추출합니다.")
            .AppendLine("    /// </summary>")
            .AppendLine("    private static string GetErrorCode(global::LanguageExt.Common.Error error)")
            .AppendLine("    {")
            .AppendLine("        return error switch")
            .AppendLine("        {")
            .AppendLine("            global::Functorium.Abstractions.Errors.IHasErrorCode hasErrorCode => hasErrorCode.ErrorCode,")
            .AppendLine("            _ => error.GetType().Name")
            .AppendLine("        };")
            .AppendLine("    }")
            .AppendLine();
    }

    private static void GenerateMethod(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        // 파라미터에서 반환 타입 추출 (FinT<IO, ReturnType>에서 ReturnType 추출)
        string actualReturnType = ExtractActualReturnType(method.ReturnType);

        // 메서드 시그니처
        sb.AppendLine($"    public override {method.ReturnType} {method.Name}(");
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            var param = method.Parameters[i];
            var comma = i < method.Parameters.Count - 1 ? "," : "";
            sb.AppendLine($"        {param.Type} {param.Name}{comma}");
        }
        var actualReturnTypeForMethod = ExtractActualReturnType(method.ReturnType);
        sb.AppendLine("    ) =>")
            .AppendLine($"        global::LanguageExt.FinT.lift<global::LanguageExt.IO, {actualReturnTypeForMethod}>(")
            .AppendLine("            (from result in ExecuteWithSpan(")
            .AppendLine($"                requestHandler: RequestHandler,")
            .AppendLine($"                requestHandlerMethod: nameof({method.Name}),")
            .AppendLine($"                operation: FinTToIO(base.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))})),")
            .AppendLine($"                requestLog: () => AdapterRequestLog_{classInfo.ClassName}_{method.Name}(RequestHandler, nameof({method.Name}){(method.Parameters.Count > 0 ? ", " + string.Join(", ", method.Parameters.Select(p => p.Name)) : "")}),")
            .AppendLine($"                responseLogSuccess: AdapterResponseSuccessLog_{classInfo.ClassName}_{method.Name},")
            .AppendLine($"                responseLogFailure: AdapterResponseFailureLog_{classInfo.ClassName}_{method.Name},")
            .AppendLine("                startTimestamp: ElapsedTimeCalculator.GetCurrentTimestamp())")
            .AppendLine($"            select result).Map(r => global::LanguageExt.Fin.Succ(r)));")
            .AppendLine();

        // Request logging helper
        var parameterDeclarations = method.Parameters.Count > 0
            ? ",\n        " + string.Join(",\n        ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))
            : "";

        string requestParamsExpr = BuildRequestParamsExpression(method.Parameters);
        string requestMessageExpr = BuildRequestMessageExpression(method.Parameters);

        sb.AppendLine($"    private global::LanguageExt.IO<global::LanguageExt.Unit> AdapterRequestLog_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        string requestHandler,")
            .Append("        string requestHandlerMethod")
            .AppendLine($"{parameterDeclarations}) =>")
            .AppendLine("        global::LanguageExt.IO.lift(() =>")
            .AppendLine("        {")
            .AppendLine("            if (_isDebugEnabled)")
            .AppendLine("            {")
            .AppendLine($"                var requestParams = {requestParamsExpr};")
            .AppendLine($"                var requestMessage = {requestMessageExpr};")
            .AppendLine($"                _logger.LogAdapterRequestDebug_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                    ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                    _requestCategoryLowerCase,")
            .AppendLine("                    requestHandler,")
            .AppendLine("                    requestHandlerMethod,")
            .AppendLine("                    requestParams,")
            .AppendLine("                    requestMessage);")
            .AppendLine("            }")
            .AppendLine("            else if (_isInformationEnabled)")
            .AppendLine("            {")
            .AppendLine($"                var requestParams = {requestParamsExpr};")
            .AppendLine($"                _logger.LogAdapterRequest_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                    ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                    _requestCategoryLowerCase,")
            .AppendLine("                    requestHandler,")
            .AppendLine("                    requestHandlerMethod,")
            .AppendLine("                    requestParams);")
            .AppendLine("            }")
            .AppendLine("            return global::LanguageExt.Unit.Default;")
            .AppendLine("        });")
            .AppendLine();

        // Response success logging helper
        sb.AppendLine($"    private void AdapterResponseSuccessLog_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine($"        {actualReturnType} result,")
            .AppendLine("        double elapsed)")
            .AppendLine("    {")
            .AppendLine("        if (_isDebugEnabled)")
            .AppendLine("        {")
            .AppendLine($"            _logger.LogAdapterResponseSuccessDebug_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                _requestCategoryLowerCase,")
            .AppendLine("                requestHandler,")
            .AppendLine("                requestHandlerMethod,")
            .AppendLine("                ObservabilityNaming.Status.Success,")
            .AppendLine("                result,")
            .AppendLine("                elapsed);")
            .AppendLine("        }")
            .AppendLine("        else if (_isInformationEnabled)")
            .AppendLine("        {")
            .AppendLine($"            _logger.LogAdapterResponseSuccess_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                _requestCategoryLowerCase,")
            .AppendLine("                requestHandler,")
            .AppendLine("                requestHandlerMethod,")
            .AppendLine("                ObservabilityNaming.Status.Success,")
            .AppendLine("                elapsed);")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine();

        // Response failure logging helper
        sb.AppendLine($"    private void AdapterResponseFailureLog_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        global::LanguageExt.Common.Error error,")
            .AppendLine("        double elapsed)")
            .AppendLine("    {")
            .AppendLine("        var (errorType, errorCode) = GetErrorInfo(error);")
            .AppendLine()
            .AppendLine("        if (error.IsExceptional && _isErrorEnabled)")
            .AppendLine("        {")
            .AppendLine($"            _logger.LogAdapterResponseError_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                _requestCategoryLowerCase,")
            .AppendLine("                requestHandler,")
            .AppendLine("                requestHandlerMethod,")
            .AppendLine("                ObservabilityNaming.Status.Failure,")
            .AppendLine("                elapsed,")
            .AppendLine("                errorType,")
            .AppendLine("                errorCode,")
            .AppendLine("                error);")
            .AppendLine("        }")
            .AppendLine("        else if (_isWarningEnabled)")
            .AppendLine("        {")
            .AppendLine($"            _logger.LogAdapterResponseWarning_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("                ObservabilityNaming.Layers.Adapter,")
            .AppendLine($"                _requestCategoryLowerCase,")
            .AppendLine("                requestHandler,")
            .AppendLine("                requestHandlerMethod,")
            .AppendLine("                ObservabilityNaming.Status.Failure,")
            .AppendLine("                elapsed,")
            .AppendLine("                errorType,")
            .AppendLine("                errorCode,")
            .AppendLine("                error);")
            .AppendLine("        }")
            .AppendLine("    }");
    }

    /// <summary>
    /// @request.params 익명 객체 생성 코드를 만듭니다.
    /// 타입 필터링: 스칼라 → 값, 컬렉션 → count, IValueObject/IEntityId → .ToString(), 복합 타입 → 제외
    /// </summary>
    private static string BuildRequestParamsExpression(List<ParameterInfo> parameters)
    {
        var fields = new List<string>();
        foreach (var param in parameters)
        {
            if (param.IsComplexType && !param.NeedsToString)
                continue;

            string snakeName = SnakeCaseConverter.ToSnakeCase(param.Name);

            if (param.IsCollection)
            {
                string countExpr = CollectionTypeHelper.GetCountExpression(param.Name, param.Type) ?? "0";
                fields.Add($"{snakeName}_count = {countExpr}");
            }
            else if (param.NeedsToString)
            {
                fields.Add($"{snakeName} = {param.Name}.ToString()");
            }
            else
            {
                fields.Add($"{snakeName} = {param.Name}");
            }
        }

        if (fields.Count == 0)
            return ObservableGeneratorConstants.EmptyRequestObjectLiteral;

        return $"new {{ {string.Join(", ", fields)} }}";
    }

    /// <summary>
    /// @request.message 익명 객체 생성 코드를 만듭니다.
    /// 모든 파라미터를 포함합니다 (필터링 없음).
    /// </summary>
    private static string BuildRequestMessageExpression(List<ParameterInfo> parameters)
    {
        if (parameters.Count == 0)
            return ObservableGeneratorConstants.EmptyRequestObjectLiteral;

        return $"new {{ {string.Join(", ", parameters.Select(p => p.Name))} }}";
    }

    private static void GenerateLoggingMethods(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        string actualReturnType = ExtractActualReturnType(method.ReturnType);

        // ===== Static delegate fields for LoggerMessage.Define =====
        GenerateLoggerMessageDefineFields(sb, classInfo, method);

        // ===== LogRequestDebug (@request.params + @request.message, 6 params → 항상 LoggerMessage.Define) =====
        sb.AppendLine($"    public static void LogAdapterRequestDebug_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        object requestParams,")
            .AppendLine("        object requestMessage)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Debug))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine($"        _logAdapterRequestDebug_{classInfo.ClassName}_{method.Name}(logger, requestLayer, requestCategory, requestHandler, requestHandlerMethod, requestParams, requestMessage, null);")
            .AppendLine("    }")
            .AppendLine();

        // ===== LogRequest (@request.params, 5 params → LoggerMessage.Define) =====
        sb.AppendLine($"    public static void LogAdapterRequest_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        object requestParams)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Information))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine($"        _logAdapterRequest_{classInfo.ClassName}_{method.Name}(logger, requestLayer, requestCategory, requestHandler, requestHandlerMethod, requestParams, null);")
            .AppendLine("    }")
            .AppendLine();

        // ===== LogResponseDebug (@response.message, 7 params → fallback) =====
        sb.AppendLine($"    public static void LogAdapterResponseSuccessDebug_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        string status,")
            .AppendLine($"        {actualReturnType} responseMessage,")
            .AppendLine("        double elapsed)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Debug))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine("        logger.LogDebug(")
            .AppendLine("            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseSuccess,")
            .AppendLine("            message: \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}\",")
            .AppendLine("            requestLayer,")
            .AppendLine("            requestCategory,")
            .AppendLine("            requestHandler,")
            .AppendLine("            requestHandlerMethod,")
            .AppendLine("            status,")
            .AppendLine("            elapsed,")
            .AppendLine("            responseMessage);")
            .AppendLine("    }")
            .AppendLine();

        // ===== LogResponse (result 제외, 6 params → LoggerMessage.Define) =====
        sb.AppendLine($"    public static void LogAdapterResponseSuccess_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        string status,")
            .AppendLine("        double elapsed)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Information))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine($"        _logAdapterResponseSuccess_{classInfo.ClassName}_{method.Name}(logger, requestLayer, requestCategory, requestHandler, requestHandlerMethod, status, elapsed, null);")
            .AppendLine("    }")
            .AppendLine();

        // ===== LogResponseWarning =====
        sb.AppendLine($"    public static void LogAdapterResponseWarning_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        string status,")
            .AppendLine("        double elapsed,")
            .AppendLine("        string errorType,")
            .AppendLine("        string errorCode,")
            .AppendLine("        global::LanguageExt.Common.Error error)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Warning))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine("        logger.LogWarning(")
            .AppendLine("            ObservabilityNaming.EventIds.Adapter.AdapterResponseWarning,")
            .AppendLine("            \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}\",")
            .AppendLine("            requestLayer,")
            .AppendLine("            requestCategory,")
            .AppendLine("            requestHandler,")
            .AppendLine("            requestHandlerMethod,")
            .AppendLine("            status,")
            .AppendLine("            elapsed,")
            .AppendLine("            errorType,")
            .AppendLine("            errorCode,")
            .AppendLine("            error);")
            .AppendLine("    }")
            .AppendLine();

        // ===== LogResponseError =====
        sb.AppendLine($"    public static void LogAdapterResponseError_{classInfo.ClassName}_{method.Name}(")
            .AppendLine("        this ILogger logger,")
            .AppendLine("        string requestLayer,")
            .AppendLine("        string requestCategory,")
            .AppendLine("        string requestHandler,")
            .AppendLine("        string requestHandlerMethod,")
            .AppendLine("        string status,")
            .AppendLine("        double elapsed,")
            .AppendLine("        string errorType,")
            .AppendLine("        string errorCode,")
            .AppendLine("        global::LanguageExt.Common.Error error)")
            .AppendLine("    {")
            .AppendLine("        if (!logger.IsEnabled(LogLevel.Error))")
            .AppendLine("            return;")
            .AppendLine()
            .AppendLine("        logger.LogError(")
            .AppendLine("            ObservabilityNaming.EventIds.Adapter.AdapterResponseError,")
            .AppendLine("            \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}\",")
            .AppendLine("            requestLayer,")
            .AppendLine("            requestCategory,")
            .AppendLine("            requestHandler,")
            .AppendLine("            requestHandlerMethod,")
            .AppendLine("            status,")
            .AppendLine("            elapsed,")
            .AppendLine("            errorType,")
            .AppendLine("            errorCode,")
            .AppendLine("            error);")
            .AppendLine("    }");
    }

    /// <summary>
    /// LoggerMessage.Define을 사용한 정적 delegate 필드들을 생성합니다.
    /// </summary>
    private static void GenerateLoggerMessageDefineFields(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        sb.AppendLine();
        sb.AppendLine($"    // ===== LoggerMessage.Define delegates for {method.Name} =====");

        // 1. LogRequest (5 params: layer, category, handler, method, @request.params)
        GenerateLogRequestDelegate(sb, classInfo, method);

        // 2. LogRequestDebug (6 params: layer, category, handler, method, @request.params, @request.message)
        GenerateLogRequestDebugDelegate(sb, classInfo, method);

        // 3. LogResponse (6 params: layer, category, handler, method, status, elapsed)
        GenerateLogResponseDelegate(sb, classInfo, method);

        // Note: LogResponseDebug는 항상 7 params → fallback (delegate 없음)
        // Note: LogResponseWarning/Error는 직접 호출 방식으로 delegate가 필요 없음

        sb.AppendLine();
    }

    private static void GenerateLogRequestDelegate(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        // 5 params: string (layer), string (category), string (handler), string (method), object (@request.params)
        sb.AppendLine($"    private static readonly global::System.Action<ILogger, string, string, string, string, object, global::System.Exception?> _logAdapterRequest_{classInfo.ClassName}_{method.Name} =");
        sb.AppendLine("        LoggerMessage.Define<string, string, string, string, object>(");
        sb.AppendLine("            LogLevel.Information,");
        sb.AppendLine("            ObservabilityNaming.EventIds.Adapter.AdapterRequest,");
        sb.AppendLine("            \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.params}\");");
        sb.AppendLine();
    }

    private static void GenerateLogRequestDebugDelegate(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        // 6 params: string (layer), string (category), string (handler), string (method), object (@request.params), object (@request.message)
        // → 항상 LoggerMessage.Define 가능 (6개 이하)
        sb.AppendLine($"    private static readonly global::System.Action<ILogger, string, string, string, string, object, object, global::System.Exception?> _logAdapterRequestDebug_{classInfo.ClassName}_{method.Name} =");
        sb.AppendLine("        LoggerMessage.Define<string, string, string, string, object, object>(");
        sb.AppendLine("            LogLevel.Debug,");
        sb.AppendLine("            ObservabilityNaming.EventIds.Adapter.AdapterRequest,");
        sb.AppendLine("            \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.params} {@request.message}\");");
        sb.AppendLine();
    }

    private static void GenerateLogResponseDelegate(StringBuilder sb, ObservableClassInfo classInfo, MethodInfo method)
    {
        sb.AppendLine($"    private static readonly global::System.Action<ILogger, string, string, string, string, string, double, global::System.Exception?> _logAdapterResponseSuccess_{classInfo.ClassName}_{method.Name} =");
        sb.AppendLine("        LoggerMessage.Define<string, string, string, string, string, double>(");
        sb.AppendLine("            LogLevel.Information,");
        sb.AppendLine("            ObservabilityNaming.EventIds.Adapter.AdapterResponseSuccess,");
        sb.AppendLine("            \"{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s\");");
        sb.AppendLine();
    }

}
