---
title: "메서드와 프로퍼티 검증"
---

## 개요

Part 1에서 클래스의 가시성, 한정자, 네이밍 규칙은 모두 통과했습니다. 그런데 `public` 메서드가 `void`를 반환해야 할 곳에서 `Task`를 반환하고 있고, 불변이어야 할 프로퍼티에 `public set`이 노출되어 있다면? 클래스 수준 검증만으로는 이런 내부 설계 위반을 잡을 수 없습니다.

> **클래스의 껍데기가 올바르다고 내부까지 올바른 것은 아닙니다. 멤버 수준까지 검증해야 설계 의도가 완전히 보호됩니다.**

이 파트에서는 **ClassValidator의** 메서드 검증과 프로퍼티/필드 검증 기능을 학습합니다. 메서드의 가시성, 정적 여부, 반환 타입, 파라미터뿐만 아니라 프로퍼티의 존재 여부와 불변성까지 아키텍처 테스트로 강제하는 방법을 다룹니다.

## 학습 목표

### 핵심 학습 목표
1. **메서드 시그니처 검증**
   - `RequireMethod`, `RequireAllMethods`로 메서드 존재 및 가시성 확인
   - `RequireExtensionMethod`로 확장 메서드 패턴 강제
2. **반환 타입 규칙 강제**
   - `RequireReturnType`, `RequireReturnTypeOfDeclaringClass`로 팩토리 메서드 패턴 검증
   - `RequireReturnTypeContaining`으로 유연한 반환 타입 매칭
3. **파라미터 규칙 검증**
   - `RequireParameterCount`, `RequireFirstParameterTypeContaining`으로 메서드 시그니처 제어
4. **프로퍼티와 필드 불변성 보호**
   - `RequireNoPublicSetters`로 불변 설계 강제
   - `RequireNoInstanceFields`로 필드 접근 규칙 검증

### 실습을 통해 확인할 내용
- Usecase 클래스의 `Execute` 메서드가 올바른 시그니처를 갖는지 검증
- 팩토리 메서드가 선언 클래스 타입을 반환하는지 확인
- Value Object에 `public set` 프로퍼티가 없는지 자동 검증

## 챕터 구성

| 챕터 | 주제 | 핵심 API |
|------|------|----------|
| [Chapter 1](01-Method-Validation/) | 메서드 검증 | `RequireMethod`, `RequireAllMethods`, `RequireVisibility`, `RequireExtensionMethod` |
| [Chapter 2](02-Return-Type-Validation/) | 반환 타입 검증 | `RequireReturnType`, `RequireReturnTypeOfDeclaringClass`, `RequireReturnTypeContaining` |
| [Chapter 3](03-Parameter-Validation/) | 파라미터 검증 | `RequireParameterCount`, `RequireFirstParameterTypeContaining` |
| [Chapter 4](04-Property-And-Field-Validation/) | 프로퍼티와 필드 검증 | `RequireProperty`, `RequireNoPublicSetters`, `RequireNoInstanceFields` |

## 학습 흐름

```
메서드 검증 → 반환 타입 검증 → 파라미터 검증 → 프로퍼티/필드 검증
```

---

멤버 수준의 검증을 마치면, Part 3에서 **불변성 규칙, 중첩 클래스, 커스텀 규칙 합성** 같은 고급 검증 기법으로 진입합니다.

[이전: Part 1 - ClassValidator 기초](../Part1-ClassValidator-Basics/) | [다음: Part 3 - 고급 검증](../Part3-Advanced-Validation/)
