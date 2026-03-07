---
title: "INamedTypeSymbol"
---

## 개요

앞 장에서 `ForAttributeWithMetadataName`의 `transform` 콜백에서 `ctx.TargetSymbol`을 통해 심볼에 접근했습니다. 이 심볼이 바로 `INamedTypeSymbol`입니다. ObservablePortGenerator는 이 심볼 하나에서 클래스 이름, 네임스페이스, 구현 인터페이스, 그리고 메서드 목록까지 코드 생성에 필요한 모든 정보를 추출합니다. 이번 장에서는 이 API 각각이 **왜** 필요하고, 우리 프로젝트에서 **어떻게** 사용되는지를 함께 살펴봅니다.

## 학습 목표

### 핵심 학습 목표
1. **INamedTypeSymbol의** 기본 정보 추출 API를 이해한다
   - Name, ContainingNamespace, TypeKind의 역할과 사용법
2. **AllInterfaces와 GetMembers()를** 활용한 심층 분석을 습득한다
   - 인터페이스 계층 탐색과 멤버 필터링
3. **ObservablePortGenerator의** `MapToObservableClassInfo`에서 이 API들이 어떻게 조합되는지 학습한다

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

소스 생성기가 코드를 생성하려면 먼저 대상 클래스의 이름과 네임스페이스를 알아야 합니다. 생성되는 `UserRepositoryObservable` 클래스의 이름과 `namespace` 선언이 모두 여기서 나옵니다.

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

ObservablePortGenerator가 래핑할 메서드를 찾으려면, 대상 클래스가 `IObservablePort`를 구현하는지, 그리고 어떤 인터페이스 계층을 통해 구현하는지를 알아야 합니다. 여기서 `AllInterfaces`와 `Interfaces`의 차이가 중요해집니다.

### AllInterfaces vs Interfaces

```csharp
// Interfaces: 직접 구현한 인터페이스만
var directInterfaces = classSymbol.Interfaces;

// AllInterfaces: 직접 + 상속받은 모든 인터페이스
var allInterfaces = classSymbol.AllInterfaces;

// 예시:
// public interface IUserRepository : IObservablePort { }
// public class UserRepository : IUserRepository { }

// classSymbol.Interfaces → [IUserRepository]
// classSymbol.AllInterfaces → [IUserRepository, IObservablePort]
```

### IObservablePort 구현 확인

```csharp
// ObservablePortGenerator.cs에서
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

// 사용
var adapterInterfaces = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort);
```

---

## 멤버 분석

인터페이스를 찾았다면, 그 안의 메서드를 추출해야 합니다. `GetMembers()`는 타입의 모든 멤버(메서드, 프로퍼티, 필드 등)를 반환하며, `OfType<T>()`으로 원하는 종류만 필터링할 수 있습니다. 아래 "인터페이스에서 메서드 추출" 코드가 우리 프로젝트의 핵심 로직입니다.

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
// ObservablePortGenerator.cs의 실제 코드
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
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

생성된 `Observable` 클래스는 원본 클래스를 상속하므로, 부모의 생성자 파라미터를 그대로 전달해야 합니다. `Constructors` 프로퍼티로 생성자 목록에 접근하고, 각 생성자의 파라미터를 분석하여 생성 코드에 반영합니다.

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
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
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

## 실제 활용: ObservableClassInfo 생성

지금까지 개별 API를 살펴보았습니다. 이제 이 API들이 `MapToObservableClassInfo` 메서드에서 어떻게 조합되어 하나의 `ObservableClassInfo`를 만들어내는지 전체 흐름을 확인합니다. 이 메서드가 `ForAttributeWithMetadataName`의 `transform` 콜백으로 사용됩니다.

```csharp
private static ObservableClassInfo MapToObservableClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 1. 타입 심볼 확인
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
    {
        return ObservableClassInfo.None;
    }

    cancellationToken.ThrowIfCancellationRequested();

    // 2. 기본 정보 추출
    string className = classSymbol.Name;
    string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
        ? string.Empty
        : classSymbol.ContainingNamespace.ToString();

    // 3. 인터페이스에서 메서드 추출
    var methods = classSymbol.AllInterfaces
        .Where(ImplementsIObservablePort)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        .Where(m => m.MethodKind == MethodKind.Ordinary)
        .Select(m => MapToMethodInfo(m))
        .ToList();

    // 4. 메서드가 없으면 생성 불필요
    if (methods.Count == 0)
    {
        return ObservableClassInfo.None;
    }

    // 5. 생성자 파라미터 추출
    var baseConstructorParameters =
        ConstructorParameterExtractor.ExtractParameters(classSymbol);

    // 6. 결과 반환
    return new ObservableClassInfo(
        @namespace, className, methods, baseConstructorParameters);
}
```

---

## 요약

`INamedTypeSymbol`은 소스 생성기에서 타입 정보를 추출하는 핵심 도구입니다. 우리 프로젝트에서는 `Name`과 `ContainingNamespace`로 생성 클래스의 이름과 네임스페이스를 결정하고, `AllInterfaces`로 `IObservablePort` 구현 여부를 확인한 뒤, `GetMembers()`로 래핑할 메서드를 추출하며, `Constructors`로 부모 생성자 파라미터를 전달합니다.

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

`INamedTypeSymbol`로 클래스와 인터페이스 수준의 정보를 추출하는 방법을 이해했습니다. 다음 장에서는 한 단계 더 들어가서, 각 메서드의 시그니처(이름, 파라미터, 반환 타입)를 분석하는 `IMethodSymbol`을 살펴봅니다. 이 정보가 로깅 코드와 파이프라인 래퍼의 생성 근거가 됩니다.

→ [06. IMethodSymbol](../06-IMethodSymbol/)
