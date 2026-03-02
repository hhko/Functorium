# Part 2: 메서드와 프로퍼티 검증

Part 2에서는 **ClassValidator의** 메서드 검증과 프로퍼티/필드 검증 기능을 학습합니다. 메서드의 가시성, 정적 여부, 반환 타입, 파라미터뿐만 아니라 프로퍼티의 존재 여부와 불변성까지 아키텍처 테스트로 강제하는 방법을 다룹니다.

## 챕터 구성

| 챕터 | 주제 | 핵심 API |
|------|------|----------|
| [Chapter 5](01-Method-Validation/README.md) | 메서드 검증 | `RequireMethod`, `RequireStatic`, `RequireExtensionMethod` |
| [Chapter 6](02-Return-Type-Validation/README.md) | 반환 타입 검증 | `RequireReturnType`, `RequireReturnTypeOfDeclaringClass` |
| [Chapter 7](03-Parameter-Validation/README.md) | 파라미터 검증 | `RequireParameterCount`, `RequireFirstParameterTypeContaining` |
| [Chapter 8](04-Property-And-Field-Validation/README.md) | 프로퍼티와 필드 검증 | `RequireProperty`, `RequireNoPublicSetters`, `RequireNoInstanceFields` |

## 학습 흐름

```
메서드 검증 → 반환 타입 검증 → 파라미터 검증 → 프로퍼티/필드 검증
```

Part 1에서 클래스 수준의 기본 검증(가시성, 한정자, 네이밍, 상속)을 학습했다면, Part 2에서는 클래스 내부의 **멤버 수준 검증으로** 범위를 확장합니다.

---

[이전: Part 1 - ClassValidator 기초](../Part1-ClassValidator-Basics/README.md) | [다음: Part 3 - 고급 검증](../Part3-Advanced-Validation/README.md)
