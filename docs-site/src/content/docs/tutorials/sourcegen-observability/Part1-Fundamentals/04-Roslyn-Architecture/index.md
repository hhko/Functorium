---
title: "Roslyn 아키텍처"
---

Roslyn의 SyntaxTree, CompilationUnit 구조를 탐색합니다.

## 핵심 개념

- `SyntaxTree.ParseText()` — 소스 코드 파싱
- `CompilationUnitSyntax` — 루트 노드
- `DescendantNodes()` — 트리 순회

## 실행

```bash
dotnet run --project RoslynArchitecture
```
