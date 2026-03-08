---
title: "Hello World 생성기"
---

최소한의 IIncrementalGenerator 구현 예제입니다.

## 프로젝트 구조

- `HelloWorldGenerator.Generator/` — 소스 생성기 (netstandard2.0)
- `HelloWorldGenerator.Usage/` — 생성된 코드를 사용하는 콘솔 앱

## 실행

```bash
dotnet run --project HelloWorldGenerator.Usage
```

---

## FAQ

### Q1: Hello World 생성기는 왜 `netstandard2.0`을 타겟으로 해야 하나요?
**A**: 소스 생성기는 Roslyn 컴파일러 내부에서 실행되며, 컴파일러가 `netstandard2.0` 어셈블리만 로드할 수 있기 때문입니다. `net8.0`이나 `net10.0`을 타겟으로 지정하면 컴파일러가 생성기 어셈블리를 인식하지 못합니다.

### Q2: 생성기 프로젝트와 사용 프로젝트를 왜 분리해야 하나요?
**A**: 소스 생성기는 컴파일러의 확장으로 동작하므로, 생성기 코드 자체가 컴파일 대상 프로젝트에 포함되면 순환 의존이 발생합니다. 별도 프로젝트로 분리하고 `OutputItemType="Analyzer"`로 참조해야 컴파일러가 생성기로 올바르게 인식합니다.

### Q3: 생성된 코드는 어디에서 확인할 수 있나요?
**A**: Visual Studio의 솔루션 탐색기에서 Dependencies > Analyzers > 생성기 프로젝트 이름을 확장하면 `.g.cs` 파일을 직접 열어볼 수 있습니다. 또는 프로젝트 파일에 `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`를 추가하면 디스크에 파일이 출력됩니다.
