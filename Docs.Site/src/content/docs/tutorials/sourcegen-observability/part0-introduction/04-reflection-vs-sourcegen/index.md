---
title: "Reflection vs Source Generator"
---

리플렉션 기반 로깅과 LoggerMessage.Define(소스 생성기) 방식을 비교합니다.

## 실행

```bash
dotnet run --project ReflectionVsSourceGen
```

---

## FAQ

### Q1: 리플렉션 기반 로깅이 성능에 미치는 영향은 어느 정도인가요?
**A**: 리플렉션은 매 호출마다 타입 메타데이터를 조회하므로 약 100배 이상의 성능 차이가 발생할 수 있습니다. 특히 고빈도 호출 경로에서는 GC 압력도 증가하여 전체 애플리케이션 응답 시간에 영향을 줍니다.

### Q2: `LoggerMessage.Define`은 왜 고성능인가요?
**A**: `LoggerMessage.Define`은 로그 메시지 템플릿을 컴파일 타임에 한 번만 파싱하고, 델리게이트로 캐싱합니다. 로깅 호출 시 문자열 보간이나 박싱 없이 직접 값을 전달하므로, 할당(allocation)이 발생하지 않아 고성능을 달성합니다.

### Q3: 이 프로젝트를 직접 실행해보려면 어떤 준비가 필요한가요?
**A**: .NET SDK가 설치되어 있으면 `dotnet run --project ReflectionVsSourceGen` 명령으로 바로 실행할 수 있습니다. 벤치마크 결과를 통해 리플렉션 방식과 소스 생성기 방식의 성능 차이를 직접 확인할 수 있습니다.
