---
title: "ADR-0018: 명시적 트랜잭션 지원"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

Functorium의 파이프라인은 기본적으로 Usecase 단위로 자동 트랜잭션을 관리합니다. 그러나 다중 Aggregate를 포함하는 비즈니스 트랜잭션에서는 자동 트랜잭션의 범위가 단일 Usecase에 고정되어 있어, 여러 Aggregate 변경을 하나의 트랜잭션으로 묶거나 특정 구간만 트랜잭션으로 감싸는 것이 불가능합니다.

예를 들어 주문 생성 시 재고 차감과 결제 처리를 하나의 트랜잭션으로 묶어야 하는 경우, 자동 트랜잭션만으로는 범위를 제어할 수 없습니다.

## 검토한 옵션

1. IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction
2. 항상 자동 트랜잭션
3. 항상 명시적 트랜잭션
4. Saga 패턴

## 결정

**선택한 옵션: "IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction"**, 파이프라인의 자동 트랜잭션과 명시적 트랜잭션을 공존시켜 단순한 경우는 자동으로, 복잡한 경우는 명시적으로 트랜잭션 범위를 제어할 수 있기 때문입니다.

`IUnitOfWork`에 `BeginTransactionAsync()`를 추가하여 `IUnitOfWorkTransaction`을 반환하고, 이를 통해 `CommitAsync()` / `RollbackAsync()`를 명시적으로 호출할 수 있습니다. 파이프라인이 이미 활성 트랜잭션을 감지하면 자동 트랜잭션을 생성하지 않습니다.

### 결과

- Good, because 단순한 단일 Aggregate Usecase는 기존과 동일하게 자동 트랜잭션으로 동작합니다.
- Good, because 다중 Aggregate 시나리오에서 트랜잭션 범위를 명시적으로 제어할 수 있습니다.
- Good, because 자동과 명시적 트랜잭션이 충돌 없이 공존합니다.
- Bad, because 명시적 트랜잭션 사용 시 개발자가 Commit/Rollback을 직접 관리해야 합니다.

### 확인

- 자동 트랜잭션과 명시적 트랜잭션이 중첩되지 않는지 통합 테스트로 검증합니다.
- 명시적 트랜잭션 내에서 예외 발생 시 롤백이 정상 수행되는지 확인합니다.

## 옵션별 장단점

### IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction

- Good, because 자동 트랜잭션과 명시적 트랜잭션이 공존하여 유연합니다.
- Good, because 기존 파이프라인 동작을 변경하지 않으면서 확장됩니다.
- Good, because IUnitOfWorkTransaction이 명시적인 Commit/Rollback 시점을 제공합니다.
- Bad, because 트랜잭션 중첩 감지 로직이 추가로 필요합니다.

### 항상 자동 트랜잭션

- Good, because 개발자가 트랜잭션을 의식하지 않아도 됩니다.
- Bad, because 트랜잭션 범위가 Usecase 단위에 고정되어 다중 Aggregate 시나리오를 처리할 수 없습니다.
- Bad, because 트랜잭션 범위 축소(특정 구간만 트랜잭션)가 불가능합니다.

### 항상 명시적 트랜잭션

- Good, because 트랜잭션 범위를 완전히 제어할 수 있습니다.
- Bad, because 모든 Usecase에서 보일러플레이트 코드가 발생합니다.
- Bad, because 단순한 단일 Aggregate Usecase에도 불필요한 트랜잭션 관리 코드가 필요합니다.

### Saga 패턴

- Good, because 분산 시스템 간 트랜잭션을 관리할 수 있습니다.
- Bad, because 단일 데이터베이스 내 다중 Aggregate 시나리오에는 복잡도가 과도합니다.
- Bad, because 보상 트랜잭션 설계가 필요하여 구현 비용이 높습니다.

## 관련 정보

- 관련 커밋: `5a802766`, `71272343`
- 관련 가이드: `Docs.Site/src/content/docs/guides/application/`
