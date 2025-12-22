# INamedTypeSymbol

## 학습 목표

- INamedTypeSymbol로 클래스/인터페이스 정보 추출
- AllInterfaces, GetMembers() 활용법 습득
- 실제 AdapterPipelineGenerator에서의 활용 패턴 학습

---

## INamedTypeSymbol이란?

**INamedTypeSymbol**은 **이름이 있는 타입**(클래스, 인터페이스, 구조체, 열거형, 델리게이트)을 나타내는 심볼입니다.

```csharp
// 소스 생성기에서 얻는 방법
GeneratorAttributeSyntaxContext ctx = ...;

if (ctx.TargetSymbol is INamedTypeSymbol classSymbol)
{
    // 클래스/인터페이스/구조체 등의 정보에 접근
}
```

---

## 기본 정보 추출

### 이름과 네임스페이스

```csharp
INamedTypeSymbol classSymbol = ...;

// 짧은 이름
string name = classSymbol.Name;  // "UserRepository"

// 네임스페이스
string @namespace = classSymbol.ContainingNamespace.ToString();
// "MyApp.Infrastructure.Repositories"

// 글로벌 네임스페이스 확인
bool isGlobal = classSymbol.ContainingNamespace.IsGlobalNamespace;

// 실제 코드에서 네임스페이스 처리
string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
    ? string.Empty
    : classSymbol.ContainingNamespace.ToString();
```

### 타입 종류 확인

```csharp
// TypeKind로 타입 종류 확인
switch (classSymbol.TypeKind)
{
    case TypeKind.Class:
        Console.WriteLine("클래스입니다");
        break;
    case TypeKind.Interface:
        Console.WriteLine("인터페이스입니다");
        break;
    case TypeKind.Struct:
        Console.WriteLine("구조체입니다");
        break;
    case TypeKind.Enum:
        Console.WriteLine("열거형입니다");
        break;
}
```

### 수정자 확인

```csharp
// 접근성
Accessibility accessibility = classSymbol.DeclaredAccessibility;
// Public, Internal, Private 등

// 추상/봉인/정적
bool isAbstract = classSymbol.IsAbstract;
bool isSealed = classSymbol.IsSealed;
bool isStatic = classSymbol.IsStatic;

// 제네릭
bool isGeneric = classSymbol.IsGenericType;
```

---

## 인터페이스 분석

### AllInterfaces vs Interfaces

```csharp
// Interfaces: 직접 구현한 인터페이스만
var directInterfaces = classSymbol.Interfaces;

// AllInterfaces: 직접 + 상속받은 모든 인터페이스
var allInterfaces = classSymbol.AllInterfaces;

// 예시:
// public interface IUserRepository : IAdapter { }
// public class UserRepository : IUserRepository { }

// classSymbol.Interfaces → [IUserRepository]
// classSymbol.AllInterfaces → [IUserRepository, IAdapter]
```

### IAdapter 구현 확인

```csharp
// AdapterPipelineGenerator.cs에서
private static bool ImplementsIAdapter(INamedTypeSymbol interfaceSymbol)
{
    // IAdapter 자체인지 확인
    if (interfaceSymbol.Name == "IAdapter")
    {
        return true;
    }

    // IAdapter를 상속받은 인터페이스인지 확인
    return interfaceSymbol.AllInterfaces.Any(i => i.Name == "IAdapter");
}

// 사용
var adapterInterfaces = classSymbol.AllInterfaces
    .Where(ImplementsIAdapter);
```

---

## 멤버 분석

### GetMembers()

```csharp
// 모든 멤버 가져오기
var allMembers = classSymbol.GetMembers();

// 특정 이름의 멤버
var namedMembers = classSymbol.GetMembers("GetUser");

// 타입별 필터링
var methods = classSymbol.GetMembers()
    .OfType<IMethodSymbol>();

var properties = classSymbol.GetMembers()
    .OfType<IPropertySymbol>();

var fields = classSymbol.GetMembers()
    .OfType<IFieldSymbol>();
```

### 인터페이스에서 메서드 추출

```csharp
// AdapterPipelineGenerator.cs의 실제 코드
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIAdapter)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)  // 일반 메서드만
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

---

## 생성자 분석

### Constructors 프로퍼티

```csharp
// 모든 생성자
var constructors = classSymbol.Constructors;

// public 생성자만
var publicConstructors = classSymbol.Constructors
    .Where(c => c.DeclaredAccessibility == Accessibility.Public);

// 파라미터가 가장 많은 생성자
var primaryConstructor = classSymbol.Constructors
    .OrderByDescending(c => c.Parameters.Length)
    .FirstOrDefault();
```

### Primary Constructor (C# 12+)

```csharp
// Primary Constructor 예시
public class UserRepository(ILogger<UserRepository> logger) : IAdapter
{
}

// ConstructorParameterExtractor.cs의 실제 코드
public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
{
    // 1. 클래스 자체의 생성자에서 파라미터 찾기
    var constructor = classSymbol.Constructors
        .Where(c => c.DeclaredAccessibility == Accessibility.Public)
        .OrderByDescending(c => c.Parameters.Length)
        .FirstOrDefault();

    if (constructor is not null && constructor.Parameters.Length > 0)
    {
        return constructor.Parameters
            .Select(p => new ParameterInfo(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                p.RefKind))
            .ToList();
    }

    // 2. 부모 클래스의 생성자에서 찾기 (재귀)
    if (classSymbol.BaseType is not null
        && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
    {
        return ExtractParameters(classSymbol.BaseType);
    }

    return [];
}
```

---

## 상속 계층 분석

### BaseType

```csharp
// 부모 클래스
INamedTypeSymbol? baseType = classSymbol.BaseType;

// 상속 계층 탐색
void PrintHierarchy(INamedTypeSymbol type, int indent = 0)
{
    Console.WriteLine(new string(' ', indent * 2) + type.Name);

    if (type.BaseType is not null
        && type.BaseType.SpecialType != SpecialType.System_Object)
    {
        PrintHierarchy(type.BaseType, indent + 1);
    }
}

// 예시 출력:
// UserRepository
//   RepositoryBase
//     object (생략됨)
```

---

## 제네릭 타입 처리

```csharp
// 제네릭 타입 확인
if (classSymbol.IsGenericType)
{
    // 타입 파라미터 (T, TValue 등)
    var typeParams = classSymbol.TypeParameters;

    // 타입 인수 (int, string 등 - 바인딩된 제네릭)
    var typeArgs = classSymbol.TypeArguments;

    // 원본 정의 (unbounded)
    var original = classSymbol.OriginalDefinition;
}

// 예시: List<int>
// TypeParameters: [T]
// TypeArguments: [int]
// OriginalDefinition: List<T>
```

---

## 실제 활용: PipelineClassInfo 생성

```csharp
private static PipelineClassInfo MapToPipelineClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 1. 타입 심볼 확인
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
    {
        return PipelineClassInfo.None;
    }

    cancellationToken.ThrowIfCancellationRequested();

    // 2. 기본 정보 추출
    string className = classSymbol.Name;
    string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
        ? string.Empty
        : classSymbol.ContainingNamespace.ToString();

    // 3. 인터페이스에서 메서드 추출
    var methods = classSymbol.AllInterfaces
        .Where(ImplementsIAdapter)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        .Where(m => m.MethodKind == MethodKind.Ordinary)
        .Select(m => MapToMethodInfo(m))
        .ToList();

    // 4. 메서드가 없으면 생성 불필요
    if (methods.Count == 0)
    {
        return PipelineClassInfo.None;
    }

    // 5. 생성자 파라미터 추출
    var baseConstructorParameters =
        ConstructorParameterExtractor.ExtractParameters(classSymbol);

    // 6. 결과 반환
    return new PipelineClassInfo(
        @namespace, className, methods, baseConstructorParameters);
}
```

---

## 요약

| 속성/메서드 | 용도 | 반환 |
|-------------|------|------|
| `Name` | 짧은 이름 | string |
| `ContainingNamespace` | 네임스페이스 | INamespaceSymbol |
| `TypeKind` | 타입 종류 | TypeKind |
| `AllInterfaces` | 모든 인터페이스 | ImmutableArray |
| `Interfaces` | 직접 구현 인터페이스 | ImmutableArray |
| `GetMembers()` | 모든 멤버 | ImmutableArray |
| `Constructors` | 생성자들 | ImmutableArray |
| `BaseType` | 부모 클래스 | INamedTypeSymbol? |

---

## 다음 단계

다음 섹션에서는 IMethodSymbol을 상세히 학습합니다.

➡️ [02. IMethodSymbol](02-imethodsymbol.md)
