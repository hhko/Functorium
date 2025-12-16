using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Functorium.Adapters.SourceGenerator.Generators;

public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false) : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;
    //private readonly Action<IncrementalGeneratorPostInitializationContext>? _registerPostInitializationSourceOutput = registerPostInitializationSourceOutput;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // DEBUG 빌드에서만 디버거 연결 지원
        // 디버깅 필요 시 AdapterPipelineGenerator에서 AttachDebugger: true로 설정
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        //if (_registerPostInitializationSourceOutput is not null)
        //{
        //    context.RegisterPostInitializationOutput(_registerPostInitializationSourceOutput);
        //}

        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        // 소스 생성
        _generate(context, displayValues);
    }
}
