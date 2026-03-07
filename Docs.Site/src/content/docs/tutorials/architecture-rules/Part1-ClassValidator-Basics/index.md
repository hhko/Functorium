---
title: "ClassValidator 기초"
---
## 개요

새로 합류한 팀원이 도메인 Value Object를 `internal`로 선언하고, `sealed` 없이 커밋했습니다. 컴파일은 문제없이 통과하지만, 이 클래스는 다른 레이어에서 접근할 수 없고 의도치 않은 상속에 노출됩니다. 코드 리뷰에서 매번 같은 코멘트를 달아야 할까요?

> **클래스의 가시성과 한정자는 설계 의도의 표현입니다. 사람 대신 테스트가 이 의도를 보호하게 하세요.**

이 파트에서는 **ClassValidator의** 기본 검증 메서드를 학습합니다. 클래스의 가시성, 한정자, 네이밍, 상속/인터페이스 구현을 아키텍처 테스트로 강제하는 방법을 단계별로 익힙니다.

## 학습 목표

### 핵심 학습 목표
1. **아키텍처 테스트 환경 구성**
   - 어셈블리를 로드하여 검증 대상 타입을 자동으로 수집
   - `ArchRuleDefinition`과 `ValidateAllClasses`의 역할 이해
2. **가시성과 한정자 규칙 강제**
   - `RequirePublic`, `RequireSealed`, `RequireAbstract` 등으로 설계 의도 보호
   - `RequireRecord`, `RequireStatic` 등 C# 고유 한정자 검증
3. **네이밍 규칙의 자동화**
   - 접미사/접두사/정규식 기반 이름 규칙을 테스트로 검증
4. **상속 계층과 인터페이스 구현 검증**
   - `RequireInherits`, `RequireImplements`로 타입 관계 강제

### 실습을 통해 확인할 내용
- 도메인 클래스가 `public sealed`인지 자동 검증
- `Spec` 접미사 네이밍 규칙 강제
- 특정 추상 클래스를 상속하는지, 특정 인터페이스를 구현하는지 확인

## 챕터 구성

| 장 | 제목 | 핵심 내용 |
|----|------|----------|
| [1장](01-First-Architecture-Test/) | 첫 번째 아키텍처 테스트 | ArchLoader, ValidateAllClasses, RequirePublic, RequireSealed |
| [2장](02-Visibility-And-Modifiers/) | 가시성과 한정자 | RequireInternal, RequireAbstract, RequireStatic, RequireRecord |
| [3장](03-Naming-Rules/) | 네이밍 규칙 | RequireNameEndsWith, RequireNameStartsWith, RequireNameMatching |
| [4장](04-Inheritance-And-Interface/) | 상속과 인터페이스 | RequireInherits, RequireImplements, RequireImplementsGenericInterface |

---

클래스 수준의 규칙을 익힌 후에는, Part 2에서 메서드와 프로퍼티 같은 **멤버 수준의 검증으로** 범위를 확장합니다.

[다음: Part 2 - 메서드와 프로퍼티 검증](../Part2-Method-And-Property-Validation/)
