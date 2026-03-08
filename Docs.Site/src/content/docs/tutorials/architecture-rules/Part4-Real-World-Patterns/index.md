---
title: "실전 패턴"
---

## 개요

팀이 DDD 아키텍처를 도입했습니다. Entity는 반드시 `sealed`이어야 하고, Command 핸들러는 특정 인터페이스를 구현해야 하며, 도메인 레이어는 어댑터 레이어를 참조해서는 안 됩니다. 문서에 적어두었지만, 프로젝트가 커질수록 규칙은 조금씩 무너지기 시작합니다.

> **아키텍처 문서는 읽지 않을 수 있지만, 실패하는 테스트는 무시할 수 없습니다. Part 1~3에서 익힌 검증 기법을 실전 레이어 아키텍처에 적용해봅시다.**

이 파트에서는 실제 프로젝트에서 사용하는 아키텍처 패턴을 테스트로 강제하는 방법을 학습합니다. DDD 전술 패턴, Command/Query 패턴, 포트 & 어댑터 패턴, 그리고 레이어 의존성 규칙을 다룹니다.

## 학습 목표

### 핵심 학습 목표
1. **도메인 레이어 규칙 종합 적용**
   - Entity, Value Object, Domain Event, Domain Service 각각의 구조적 규칙 검증
   - 불변성, 상속, 네이밍 규칙을 하나의 테스트 스위트로 통합
2. **애플리케이션 레이어 패턴 강제**
   - Command/Query 분리 규칙과 Usecase 핸들러 시그니처 검증
   - DTO와 중첩 클래스 패턴의 구조적 일관성 보장
3. **포트 & 어댑터 규칙 검증**
   - 포트 인터페이스와 어댑터 구현체의 관계 강제
   - 어댑터가 올바른 포트만 구현하는지 자동 확인
4. **레이어 의존성 방향 자동 검증**
   - ArchUnitNET 네이티브 API와 Functorium API를 결합
   - 도메인 → 애플리케이션 → 어댑터 방향의 의존성 규칙 테스트

### 실습을 통해 확인할 내용
- Entity 클래스가 `public sealed`이고 올바른 기반 클래스를 상속하는지 검증
- Command 핸들러가 정확히 하나의 `Execute` 메서드를 갖는지 확인
- 도메인 레이어가 어댑터 레이어를 참조하지 않는지 자동 검증

## 챕터 구성

| 챕터 | 제목 | 주요 내용 |
|-------|------|-----------|
| [Chapter 1](01-Domain-Layer-Rules/) | 도메인 레이어 규칙 | Entity, Value Object, Domain Event, Domain Service |
| [Chapter 2](02-Application-Layer-Rules/) | 애플리케이션 레이어 규칙 | Command/Query 패턴, 중첩 클래스, DTO |
| [Chapter 3](03-Adapter-Layer-Rules/) | 어댑터 레이어 규칙 | 포트 인터페이스, 어댑터 구현체, 의존성 검증 |
| [Chapter 4](04-Layer-Dependency-Rules/) | 레이어 의존성 규칙 | 다중 레이어 의존성, ArchUnitNET + Functorium 결합 |

---

Part 4를 마치면, Part 5에서 지금까지 학습한 내용을 **베스트 프랙티스로 정리**하고 다음 단계를 안내합니다.

[이전: Part 3 - 고급 검증](../Part3-Advanced-Validation/) | [다음: Part 5 - 결론](../Part5-Conclusion/)
