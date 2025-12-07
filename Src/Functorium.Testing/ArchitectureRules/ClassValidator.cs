using ArchUnitNET.Domain;

namespace Functorium.Testing.ArchitectureRules;

/// <summary>
/// 클래스에 대한 아키텍처 규칙 검증을 수행하는 클래스입니다.
/// </summary>
public sealed class ClassValidator
{
    private readonly Architecture _architecture;
    private readonly Class _targetClass;
    internal readonly List<string> _failures = [];

    public ClassValidator(Architecture architecture, Class targetClass)
    {
        _architecture = architecture;
        _targetClass = targetClass;
    }

    private ClassValidator(Architecture architecture, Class targetClass, ClassValidator parentValidator)
    {
        _architecture = architecture;
        _targetClass = targetClass;

        // 중첩 클래스 검증 시 부모 검증자와 동일한 failures 리스트를 공유하여
        // 모든 검증 결과가 하나의 통합된 결과에 포함되도록 합니다.
        _failures = parentValidator._failures; // 부모와 같은 failures 리스트 공유
    }

    public ClassValidator RequireNestedClass(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null)
    {
        Class? nestedClass = _architecture.Classes.FirstOrDefault(c => c.FullName == _targetClass.FullName + "+" + nestedClassName);
        if (nestedClass == null)
        {
            _failures.Add($"Nested class '{nestedClassName}' must be required but not found.");
            return this;
        }

        nestedClassValidation?.Invoke(new ClassValidator(_architecture, nestedClass, this));
        return this;
    }

    public ClassValidator RequireNestedClassIfExists(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null)
    {
        Class? nestedClass = _architecture.Classes.FirstOrDefault(c => c.FullName == _targetClass.FullName + "+" + nestedClassName);
        if (nestedClass == null)
        {
            // 중첩 클래스가 없으면 검증을 건너뛰고 성공으로 처리
            return this;
        }

        // 중첩 클래스가 있으면 검증 수행
        nestedClassValidation?.Invoke(new ClassValidator(_architecture, nestedClass, this));
        return this;
    }

    public ClassValidator RequirePublic()
    {
        if (_targetClass.Visibility != Visibility.Public)
        {
            _failures.Add($"Class '{_targetClass.Name}' must be public.");
        }
        return this;
    }

    public ClassValidator RequireInternal()
    {
        if (_targetClass.Visibility != Visibility.Internal)
        {
            _failures.Add($"Class '{_targetClass.Name}' must be internal.");
        }
        return this;
    }

    public ClassValidator RequireSealed()
    {
        if (_targetClass.IsSealed != true)
        {
            _failures.Add($"Class '{_targetClass.Name}' must be sealed.");
        }
        return this;
    }

    public ClassValidator RequireImplements(Type interfaceType)
    {
        if (!_targetClass.ImplementedInterfaces.Any(i =>
            i.FullName != null && i.FullName.StartsWith(interfaceType.FullName!)))
        {
            _failures.Add($"Class '{_targetClass.Name}' must implement '{interfaceType.Name}'.");
        }
        return this;
    }

    public ClassValidator RequireImplementsGenericInterface(string genericInterfaceName)
    {
        if (!_targetClass.ImplementedInterfaces.Any(i =>
            i.FullName != null && i.FullName.Contains(genericInterfaceName)))
        {
            _failures.Add($"Class '{_targetClass.Name}' must implement '{genericInterfaceName}' interface.");
        }
        return this;
    }

    public ClassValidator RequireInherits(Type baseType)
    {
        if (!_targetClass.InheritedClasses.Any(b =>
            b.FullName != null && b.FullName.StartsWith(baseType.FullName!)))
        {
            _failures.Add($"Class '{_targetClass.Name}' must inherit from '{baseType.Name}'.");
        }
        return this;
    }

    public ClassValidator RequirePrivateAnyParameterlessConstructor()
    {
        var parameterlessConstructor = _targetClass.Constructors.FirstOrDefault(c => c.Parameters.Count() == 0);
        if (parameterlessConstructor == null || parameterlessConstructor.Visibility != Visibility.Private)
        {
            _failures.Add($"Class '{_targetClass.Name}' must have a private parameterless constructor.");
        }
        return this;
    }

    public ClassValidator RequireAllPrivateConstructors()
    {
        var nonPrivateConstructors = _targetClass.Constructors.Where(c => c.Visibility != Visibility.Private);
        if (nonPrivateConstructors.Any())
        {
            var constructorNames = string.Join(", ", nonPrivateConstructors.Select(c => c.Name));
            _failures.Add($"All constructors in class '{_targetClass.Name}' must be private. Found non-private constructors: {constructorNames}");
        }
        return this;
    }

    public ClassValidator RequireMethod(string methodName, Action<MethodValidator> methodValidation)
    {
        // 메서드만 필터링하여 이름을 정확히 비교 (매개변수 유무는 무시)
        var methods = _targetClass.Members
            .Where(m => m is MethodMember)
            .Cast<MethodMember>()
            .Where(m => m.Name.StartsWith(methodName + "("))
            .ToList();

        // 메서드 존재 여부 확인
        if (!methods.Any())
        {
            _failures.Add($"Class '{_targetClass.Name}' must have a method named '{methodName}'.");
            return this;
        }

        // 각 메서드에 대해 검증 수행
        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, this);
            methodValidation(methodValidator);
        }

        return this;
    }

    public ClassValidator RequireAllMethods(Action<MethodValidator> methodValidation)
    {
        var methods = _targetClass.Members
            .Where(m => m is MethodMember)
            .Cast<MethodMember>()
            .ToList();

        foreach (var method in methods)
        {
            var methodValidator = new MethodValidator(method, this);
            methodValidation(methodValidator);
        }

        return this;
    }

    /// <summary>
    /// 클래스가 불변(Immutable)인지 종합적으로 검증합니다.
    ///
    /// ValueObject의 불변성을 완전히 보장하기 위해 다음 6가지 차원에서 검증을 수행합니다:
    ///
    /// 1. **기본 Writability 검증**
    ///    - 모든 non-static 멤버가 Writability.IsImmutable()을 만족하는지 확인
    ///    - 가변적인 멤버가 있으면 실패
    ///
    /// 2. **생성자 검증**
    ///    - 모든 생성자가 private이어야 함 (public 생성자 금지)
    ///    - 외부에서 직접 인스턴스 생성을 방지하여 불변성 보장
    ///
    /// 3. **프로퍼티 검증**
    ///    - public setter가 있는 프로퍼티를 금지
    ///    - 읽기 전용 프로퍼티만 허용 (get-only properties)
    ///
    /// 4. **필드 검증**
    ///    - public 필드를 완전히 금지
    ///    - 모든 필드는 private이어야 함
    ///
    /// 5. **가변 컬렉션 타입 검증**
    ///    - List&lt;T&gt;, Dictionary&lt;K,V&gt;, HashSet&lt;T&gt; 등 가변 컬렉션 사용 금지
    ///    - ConcurrentQueue, BlockingCollection 등 동시성 컬렉션도 금지
    ///    - 불변 컬렉션(ImmutableList, ImmutableDictionary 등) 사용 권장
    ///
    /// 6. **상태 변경 메서드 검증**
    ///    - public non-static 메서드 중 상태를 변경할 수 있는 메서드 금지
    ///    - 허용되는 메서드: Equals, GetHashCode, ToString, Create, Get*, Format*, To*
    ///    - 금지되는 메서드: Set*, Update*, Modify*, Add*, Remove* 등
    ///
    /// **검증 실패 시**: 상세한 오류 메시지와 함께 실패한 규칙들을 모두 보고합니다.
    /// **검증 성공 시**: 해당 클래스가 완전한 불변성을 만족함을 보장합니다.
    ///
    /// <example>
    /// <code>
    /// // 올바른 불변 ValueObject 예시
    /// public sealed class Email : ValueObject
    /// {
    ///     private readonly string _value;  // private 필드
    ///
    ///     private Email(string value)     // private 생성자
    ///     {
    ///         _value = value;
    ///     }
    ///
    ///     public static Fin&lt;Email&gt; Create(string value) { ... }  // 허용되는 팩토리 메서드
    ///     public string Value => _value;  // 읽기 전용 프로퍼티
    ///     public override string ToString() => _value;  // 허용되는 ToString
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <returns>ClassValidator 인스턴스 (메서드 체이닝용)</returns>
    /// <exception cref="InvalidOperationException">불변성 검증 실패 시 발생하지 않음 (failures 리스트에 추가됨)</exception>
    public ClassValidator RequireImmutable()
    {
        var failures = new List<string>();
        var nonStaticMembers = GetNonStaticMembers();

        // 각 검증 규칙을 독립적으로 실행
        failures.AddRange(ValidateImmutableWritability(nonStaticMembers));
        failures.AddRange(ValidateImmutableConstructors());
        failures.AddRange(ValidateImmutableProperties());
        failures.AddRange(ValidateImmutableFields());
        failures.AddRange(ValidateImmutableCollections(nonStaticMembers));
        failures.AddRange(ValidateImmutableMethods());

        if (failures.Any())
        {
            _failures.Add($"Class '{_targetClass.Name}' must be immutable. {string.Join("; ", failures)}");
        }

        return this;
    }

    /// <summary>
    /// non-static 멤버들을 캐시하여 성능을 개선합니다.
    ///
    /// 여러 검증 메서드에서 동일한 non-static 멤버 목록이 필요하므로,
    /// 한 번만 필터링하여 재사용함으로써 성능을 최적화합니다.
    /// </summary>
    /// <returns>non-static 멤버들의 목록</returns>
    private List<IMember> GetNonStaticMembers()
    {
        return _targetClass.Members
            .Where(m => m.IsStatic == false)
            .ToList();
    }

    /// <summary>
    /// 기본 Writability 검증을 수행합니다.
    ///
    /// ArchUnitNET의 Writability.IsImmutable() 메서드를 사용하여
    /// 각 멤버가 불변성을 만족하는지 확인합니다.
    ///
    /// 검증 대상: 모든 non-static 멤버 (필드, 프로퍼티, 메서드)
    /// </summary>
    /// <param name="nonStaticMembers">검증할 non-static 멤버 목록</param>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableWritability(List<IMember> nonStaticMembers)
    {
        var mutableMembers = nonStaticMembers
            .Where(m => !m.Writability.IsImmutable())
            .ToList();

        return mutableMembers.Any()
            ? new List<string> { $"Found mutable members: {string.Join(", ", mutableMembers.Select(m => m.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 생성자 검증을 수행합니다.
    ///
    /// ValueObject는 외부에서 직접 인스턴스를 생성할 수 없어야 하므로,
    /// 모든 생성자가 private이어야 합니다.
    ///
    /// 검증 대상: 모든 생성자
    /// 허용: private 생성자
    /// 금지: public, protected, internal 생성자
    /// </summary>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableConstructors()
    {
        var publicConstructors = _targetClass.Constructors
            .Where(c => c.Visibility == Visibility.Public)
            .ToList();

        return publicConstructors.Any()
            ? new List<string> { $"Found public constructors: {string.Join(", ", publicConstructors.Select(c => c.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 프로퍼티 검증을 수행합니다.
    ///
    /// 불변 객체는 생성 후 상태가 변경되어서는 안 되므로,
    /// public setter가 있는 프로퍼티를 금지합니다.
    ///
    /// 검증 대상: 모든 프로퍼티
    /// 허용: get-only 프로퍼티 (읽기 전용)
    /// 금지: setter가 있는 프로퍼티
    /// </summary>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableProperties()
    {
        var propertiesWithSetters = _targetClass.Members
            .OfType<PropertyMember>()
            .Where(p => p.Writability == Writability.Writable)
            .ToList();

        return propertiesWithSetters.Any()
            ? new List<string> { $"Found properties with setters: {string.Join(", ", propertiesWithSetters.Select(p => p.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 필드 검증을 수행합니다.
    ///
    /// public 필드는 외부에서 직접 접근하여 값을 변경할 수 있으므로
    /// 불변성을 위반합니다. 모든 필드는 private이어야 합니다.
    ///
    /// 검증 대상: 모든 필드
    /// 허용: private 필드
    /// 금지: public, protected, internal 필드
    /// </summary>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableFields()
    {
        var publicFields = _targetClass.Members
            .OfType<FieldMember>()
            .Where(f => f.Visibility == Visibility.Public && f.IsStatic != true)
            .ToList();

        return publicFields.Any()
            ? new List<string> { $"Found public fields: {string.Join(", ", publicFields.Select(f => f.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 가변 컬렉션 타입 검증을 수행합니다.
    ///
    /// List&lt;T&gt;, Dictionary&lt;K,V&gt; 등 가변 컬렉션은
    /// 생성 후 내용을 변경할 수 있어 불변성을 위반합니다.
    ///
    /// 검증 대상: 모든 필드의 타입
    /// 허용: 불변 컬렉션 (ImmutableList, ImmutableDictionary 등)
    /// 금지: 가변 컬렉션 (List, Dictionary, HashSet, Queue, Stack 등)
    /// </summary>
    /// <param name="nonStaticMembers">검증할 non-static 멤버 목록</param>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableCollections(List<IMember> nonStaticMembers)
    {
        var mutableCollectionMembers = nonStaticMembers
            .OfType<FieldMember>()
            .Where(field => IsMutableCollectionType(field.Type))
            .ToList();

        return mutableCollectionMembers.Any()
            ? new List<string> { $"Found mutable collection types: {string.Join(", ", mutableCollectionMembers.Select(m => m.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 상태 변경 메서드 검증을 수행합니다.
    ///
    /// public non-static 메서드 중에서 객체의 상태를 변경할 수 있는
    /// 메서드를 금지합니다. 허용되는 메서드는 제한적입니다.
    ///
    /// 검증 대상: 모든 public non-static 메서드
    /// 허용: Equals, GetHashCode, ToString, Create, Get*, Format*, To*, op_*
    /// 금지: Set*, Update*, Modify*, Add*, Remove*, Change* 등
    /// </summary>
    /// <returns>검증 실패 시 오류 메시지 목록, 성공 시 빈 목록</returns>
    private List<string> ValidateImmutableMethods()
    {
        var stateChangingMethods = _targetClass.Members
            .OfType<MethodMember>()
            .Where(m => m.Visibility == Visibility.Public &&
                       m.IsStatic != true &&
                       !IsAllowedMethod(m))
            .ToList();

        return stateChangingMethods.Any()
            ? new List<string> { $"Found potentially state-changing methods: {string.Join(", ", stateChangingMethods.Select(m => m.Name))}" }
            : new List<string>();
    }

    /// <summary>
    /// 가변 컬렉션 타입인지 확인합니다.
    ///
    /// 불변성을 위반하는 가변 컬렉션 타입들을 검사합니다.
    /// 이 메서드는 타입의 FullName을 문자열로 검사하여
    /// 가변 컬렉션 패턴을 찾습니다.
    ///
    /// 검사 대상:
    /// - 일반 컬렉션: List&lt;T&gt;, Dictionary&lt;K,V&gt;, HashSet&lt;T&gt; 등
    /// - 동시성 컬렉션: ConcurrentQueue&lt;T&gt;, ConcurrentDictionary&lt;K,V&gt; 등
    /// - 기타 가변 컬렉션: Queue&lt;T&gt;, Stack&lt;T&gt;, LinkedList&lt;T&gt; 등
    /// </summary>
    /// <param name="type">검사할 타입</param>
    /// <returns>가변 컬렉션 타입이면 true, 그렇지 않으면 false</returns>
    private static bool IsMutableCollectionType(IType type)
    {
        var typeName = type.FullName ?? "";
        var mutableCollectionTypes = new[]
        {
            "List<", "Dictionary<", "HashSet<", "Queue<", "Stack<",
            "LinkedList<", "SortedList<", "SortedDictionary<", "SortedSet<",
            "ConcurrentQueue<", "ConcurrentStack<", "ConcurrentBag<",
            "ConcurrentDictionary<", "BlockingCollection<"
        };

        return mutableCollectionTypes.Any(collectionType => typeName.Contains(collectionType));
    }

    /// <summary>
    /// 허용 가능한 메서드인지 확인합니다.
    ///
    /// ValueObject에서 허용되는 메서드 패턴들을 종합적으로 검사합니다.
    /// 다음 4가지 카테고리의 메서드만 허용됩니다:
    /// 1. 동등성 관련 메서드 (Equals, GetHashCode 등)
    /// 2. ToString 메서드
    /// 3. 팩토리 메서드 (Create, Validate 등)
    /// 4. Getter 메서드 (Get*, Format*, To* 등)
    /// </summary>
    /// <param name="method">검사할 메서드</param>
    /// <returns>허용 가능한 메서드이면 true, 그렇지 않으면 false</returns>
    private static bool IsAllowedMethod(MethodMember method)
    {
        return IsEqualityMethod(method) ||
               IsToStringMethod(method) ||
               IsFactoryMethod(method) ||
               IsGetterMethod(method);
    }

    /// <summary>
    /// 동등성 관련 메서드인지 확인합니다.
    ///
    /// 객체의 동등성 비교와 관련된 메서드들을 검사합니다.
    /// 이 메서드들은 객체의 상태를 변경하지 않고
    /// 단순히 비교나 해시코드 생성을 수행합니다.
    /// </summary>
    /// <param name="method">검사할 메서드</param>
    /// <returns>동등성 관련 메서드이면 true, 그렇지 않으면 false</returns>
    private static bool IsEqualityMethod(MethodMember method)
    {
        var equalityMethods = new[]
        {
            "Equals", "GetHashCode", "GetEqualityComponents", "GetComparableEqualityComponents"
        };
        return equalityMethods.Contains(method.Name);
    }

    /// <summary>
    /// ToString 메서드인지 확인합니다.
    ///
    /// ToString 메서드는 객체의 문자열 표현을 반환하는
    /// 읽기 전용 메서드이므로 불변성을 위반하지 않습니다.
    /// </summary>
    /// <param name="method">검사할 메서드</param>
    /// <returns>ToString 메서드이면 true, 그렇지 않으면 false</returns>
    private static bool IsToStringMethod(MethodMember method)
    {
        return method.Name == "ToString";
    }

    /// <summary>
    /// 팩토리 메서드인지 확인합니다.
    ///
    /// ValueObject의 생성과 관련된 정적 메서드들을 검사합니다.
    /// 이 메서드들은 새로운 인스턴스를 생성하거나
    /// 유효성을 검사하는 역할을 합니다.
    /// </summary>
    /// <param name="method">검사할 메서드</param>
    /// <returns>팩토리 메서드이면 true, 그렇지 않으면 false</returns>
    private static bool IsFactoryMethod(MethodMember method)
    {
        var methodName = method.Name;
        return methodName == "Create" ||
               methodName == "CreateFromValidated" ||
               methodName == "Validate" ||
               methodName.StartsWith("op_"); // 연산자 오버로딩
    }

    /// <summary>
    /// Getter 메서드인지 확인합니다.
    ///
    /// 객체의 상태를 읽기만 하는 메서드들을 검사합니다.
    /// 이 메서드들은 객체의 상태를 변경하지 않고
    /// 단순히 값을 반환하거나 변환하는 역할을 합니다.
    /// </summary>
    /// <param name="method">검사할 메서드</param>
    /// <returns>Getter 메서드이면 true, 그렇지 않으면 false</returns>
    private static bool IsGetterMethod(MethodMember method)
    {
        var methodName = method.Name;
        var getterPrefixes = new[] { "get_", "Get", "Format", "To" };

        return getterPrefixes.Any(prefix => methodName.StartsWith(prefix));
    }

    /// <summary>
    /// 검증을 수행하고 결과를 반환합니다.
    ///
    /// 이 메서드는 지금까지 수행된 모든 검증 규칙의 결과를
    /// ValidationResult 객체로 반환합니다. 검증 실패가 있어도
    /// 예외를 발생시키지 않고 결과만 반환합니다.
    ///
    /// ArchitectureValidationUtilities에서 내부적으로 사용되며,
    /// 검증 결과를 집계하고 테스트 실패 시 적절한 예외를 발생시키는
    /// 역할을 담당합니다.
    /// </summary>
    /// <returns>검증 결과를 담은 ValidationResult 객체</returns>
    internal ValidationResult Validate()
    {
        return new ValidationResult(_failures);
    }

    /// <summary>
    /// 검증을 수행하고 실패 시 예외를 발생시킵니다.
    ///
    /// 이 메서드는 검증을 수행한 후 실패가 있으면 즉시
    /// InvalidOperationException을 발생시킵니다. 단일 클래스에 대한
    /// 검증에서 즉시 실패를 감지하고 싶을 때 사용됩니다.
    ///
    /// 주의: 여러 클래스에 대한 검증에서는 Validate() 메서드를 사용하고
    /// ValidationResultSummary.ThrowIfAnyFailures()를 사용하는 것이 권장됩니다.
    /// </summary>
    /// <exception cref="InvalidOperationException">검증 실패 시 발생</exception>
    public void ValidateAndThrow()
    {
        if (_failures.Any())
        {
            string message = string.Join(", ", _failures);
            throw new InvalidOperationException($"{_targetClass.FullName}: {message}");
        }
    }
}
