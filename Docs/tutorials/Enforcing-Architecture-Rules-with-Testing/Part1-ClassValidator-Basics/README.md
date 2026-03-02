# Part 1: ClassValidator 기초
> **테스트로 아키텍처 규칙 강제하기** | [← 목차로](../README.md) | [다음: Part 2 →](../Part2-Method-And-Property-Validation/README.md)

---

## 개요

이 파트에서는 **ClassValidator의** 기본 검증 메서드를 학습합니다. 클래스의 가시성, 한정자, 네이밍, 상속/인터페이스 구현을 아키텍처 테스트로 강제하는 방법을 단계별로 익힙니다.

## 목차

| 장 | 제목 | 핵심 내용 |
|----|------|----------|
| [1장](01-First-Architecture-Test/README.md) | 첫 번째 아키텍처 테스트 | ArchLoader, ValidateAllClasses, RequirePublic, RequireSealed |
| [2장](02-Visibility-And-Modifiers/README.md) | 가시성과 한정자 | RequireInternal, RequireAbstract, RequireStatic, RequireRecord |
| [3장](03-Naming-Rules/README.md) | 네이밍 규칙 | RequireNameEndsWith, RequireNameStartsWith, RequireNameMatching |
| [4장](04-Inheritance-And-Interface/README.md) | 상속과 인터페이스 | RequireInherits, RequireImplements, RequireImplementsGenericInterface |

## 학습 후 할 수 있는 것

- 어셈블리를 로드하여 아키텍처 테스트 환경 구성
- 도메인 클래스의 가시성과 한정자 규칙 강제
- 일관된 네이밍 규칙을 테스트로 검증
- 상속 계층과 인터페이스 구현 규칙 검증

---

> [← 목차로](../README.md) | [다음: Part 2 →](../Part2-Method-And-Property-Validation/README.md)
