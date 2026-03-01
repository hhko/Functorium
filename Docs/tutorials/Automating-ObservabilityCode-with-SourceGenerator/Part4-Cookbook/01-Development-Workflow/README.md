# 01: Development Workflow

생성기 개발의 전체 워크플로우(생성기 -> 사용 -> 테스트)를 보여줍니다.

## 프로젝트 구조

- `DevelopmentWorkflow.Generator/` -- 소스 생성기
- `DevelopmentWorkflow.Usage/` -- 사용 예제
- `DevelopmentWorkflow.Tests.Unit/` -- 단위 테스트

## 실행

```bash
dotnet run --project DevelopmentWorkflow.Usage
dotnet test --project DevelopmentWorkflow.Tests.Unit
```
