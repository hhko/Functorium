---
title: "엔티티 ID 생성기"
---

강타입 EntityId를 자동 생성하는 소스 생성기입니다.

## 프로젝트 구조

- `EntityIdGenerator.Generator/` -- EntityId 생성기
- `EntityIdGenerator.Usage/` -- 사용 예제
- `EntityIdGenerator.Tests.Unit/` -- 단위 테스트

## 실행

```bash
dotnet run --project EntityIdGenerator.Usage
dotnet test --project EntityIdGenerator.Tests.Unit
```
