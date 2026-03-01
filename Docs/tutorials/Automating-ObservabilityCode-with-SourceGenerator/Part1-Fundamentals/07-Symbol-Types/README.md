# 07: Symbol Types

Roslyn 심볼 타입 계층(INamedTypeSymbol, IMethodSymbol, IPropertySymbol)을 탐색합니다.

## 핵심 개념

- `INamedTypeSymbol` — 타입 정보 (인터페이스, 네임스페이스, 생성자)
- `IMethodSymbol` — 메서드 정보 (파라미터, 반환 타입, 접근성)
- `IPropertySymbol` — 속성 정보 (Get/Set, 읽기 전용)

## 실행

```bash
dotnet run --project SymbolTypes
```
