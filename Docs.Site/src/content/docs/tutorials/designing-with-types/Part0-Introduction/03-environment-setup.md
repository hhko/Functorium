---
title: "환경 설정"
---

## 필수 요구사항

- **.NET 10 SDK** 이상
- 선호하는 IDE (Visual Studio, Rider, VS Code)

## 프로젝트 구조

```
designing-with-types/
├── designing-with-types.slnx          # 솔루션 파일
├── Directory.Build.props              # 공통 빌드 설정
├── Directory.Build.targets            # 루트 상속 차단
├── Part1-Semantic-Types/              # 시맨틱 타입 (4장)
├── Part2-Making-Illegal-States-.../   # 불가능한 상태 표현 불가 (4장)
└── Part3-State-Machines/              # 상태 기계 (4장)
```

## 빌드 및 테스트

```bash
# 전체 빌드
dotnet build designing-with-types.slnx

# 전체 테스트
dotnet test --solution designing-with-types.slnx
```

## 사전 지식

다음 개념에 익숙하면 튜토리얼을 원활하게 진행할 수 있습니다.

- **C# record, sealed record:** 불변 데이터 타입과 값 동등성
- **패턴 매칭 (switch 식):** 타입별 분기 처리
- **`Fin<T>`:** 성공 또는 실패를 표현하는 함수형 결과 타입
- **`Validation<Error, T>`:** 검증 규칙 합성

이 개념들이 익숙하지 않다면, [함수형 값 객체 구현](../../functional-valueobject/) 튜토리얼의 Part 1을 먼저 참고하세요.
