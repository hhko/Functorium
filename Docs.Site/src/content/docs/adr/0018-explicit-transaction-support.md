---
title: "ADR-0018: 명시적 트랜잭션 지원"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

Functorium의 파이프라인은 Usecase 단위로 자동 트랜잭션을 관리합니다. 단일 Aggregate를 변경하는 대부분의 Usecase에서는 이 방식이 잘 작동합니다. 그러나 주문 생성 시 `Order` Aggregate 저장과 `Inventory` Aggregate의 재고 차감을 하나의 원자적 트랜잭션으로 묶어야 하는 경우, 자동 트랜잭션의 범위가 단일 Usecase에 고정되어 있어 두 Aggregate 변경을 하나의 트랜잭션으로 감쌀 수 없습니다. 결과적으로 주문은 생성되었으나 재고 차감은 별도 트랜잭션에서 실패하여 롤백되는 데이터 불일치가 발생할 수 있습니다.

반대로 하나의 Usecase 내에서 읽기 전용 조회와 쓰기 작업이 혼재할 때, 읽기 구간까지 트랜잭션에 포함되어 불필요한 잠금이 발생하는 문제도 있습니다. 트랜잭션 범위를 확장하거나 축소할 수 있는 명시적 제어가 필요합니다.

## 검토한 옵션

1. IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction
2. 항상 자동 트랜잭션
3. 항상 명시적 트랜잭션
4. Saga 패턴

## 결정

**선택한 옵션: "IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction"**, 기존 자동 트랜잭션의 편의성을 유지하면서, 다중 Aggregate 시나리오에서만 트랜잭션 범위를 명시적으로 지정할 수 있도록 확장하기 위해서입니다.

`IUnitOfWork`에 `BeginTransactionAsync()`를 추가하여 `IUnitOfWorkTransaction`을 반환합니다. 개발자는 이 트랜잭션 객체를 통해 `CommitAsync()` / `RollbackAsync()` 시점을 직접 결정합니다. 핵심 설계 원칙은 **자동 트랜잭션과의 충돌 방지**입니다. 파이프라인이 이미 활성 상태인 명시적 트랜잭션을 감지하면 자동 트랜잭션 생성을 건너뛰어, 두 메커니즘이 중첩되지 않고 자연스럽게 공존합니다.

### 결과

- Good, because 단일 Aggregate Usecase(전체의 대다수)는 코드 변경 없이 기존 자동 트랜잭션으로 동작하여 마이그레이션 비용이 없습니다.
- Good, because `Order` + `Inventory` 같은 다중 Aggregate 변경을 하나의 원자적 트랜잭션으로 묶어 데이터 불일치를 방지할 수 있습니다.
- Good, because 파이프라인의 활성 트랜잭션 감지 로직이 자동/명시적 트랜잭션 중첩을 원천 차단합니다.
- Bad, because 명시적 트랜잭션을 사용하는 Usecase에서 `CommitAsync()` 호출 누락이나 예외 경로에서의 `RollbackAsync()` 미처리 같은 실수 가능성이 개발자에게 전가됩니다.

### 확인

- 자동 트랜잭션과 명시적 트랜잭션이 중첩되지 않는지 통합 테스트로 검증합니다.
- 명시적 트랜잭션 내에서 예외 발생 시 롤백이 정상 수행되는지 확인합니다.

## 옵션별 장단점

### IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction

- Good, because 단일 Aggregate Usecase의 기존 자동 트랜잭션 코드를 전혀 수정하지 않고 다중 Aggregate 지원을 추가할 수 있습니다.
- Good, because 명시적 트랜잭션이 필요한 소수의 Usecase에서만 `BeginTransactionAsync()`를 호출하므로 보일러플레이트가 해당 범위에 한정됩니다.
- Good, because `IUnitOfWorkTransaction`이 `IAsyncDisposable`을 구현하여 `await using` 블록으로 Commit 누락 시 자동 롤백을 보장합니다.
- Bad, because 파이프라인 내부에 "현재 활성 트랜잭션이 존재하는가"를 판별하는 감지 로직을 추가 구현해야 합니다.

### 항상 자동 트랜잭션

- Good, because 개발자가 트랜잭션 경계를 의식하지 않아도 되어 인지 부하가 최소입니다.
- Bad, because 트랜잭션 범위가 Usecase 단위에 고정되므로 `Order` 저장 후 `Inventory` 차감이 실패해도 주문을 롤백할 수 없습니다.
- Bad, because 읽기 전용 구간까지 트랜잭션에 포함되어 불필요한 DB 잠금과 성능 저하가 발생합니다.

### 항상 명시적 트랜잭션

- Good, because 모든 Usecase에서 트랜잭션 시작, 커밋, 롤백 시점이 코드에 명시되어 범위가 투명합니다.
- Bad, because 단일 Aggregate만 변경하는 단순 Usecase(전체의 대다수)에도 `BeginTransaction`/`Commit`/`Rollback` 보일러플레이트가 반복됩니다.
- Bad, because 모든 개발자가 트랜잭션 관리를 직접 해야 하므로 Commit 누락이나 예외 경로 미처리 같은 실수 확률이 전체 Usecase에 걸쳐 증가합니다.

### Saga 패턴

- Good, because 서로 다른 데이터베이스나 마이크로서비스에 걸친 분산 트랜잭션을 관리할 수 있습니다.
- Bad, because 동일 데이터베이스 내 다중 Aggregate를 하나의 DB 트랜잭션으로 묶으면 해결되는 문제에 Saga를 적용하면 보상 트랜잭션, 상태 머신, 메시지 브로커 등 불필요한 인프라 복잡도가 추가됩니다.
- Bad, because 각 단계의 보상 로직(예: 재고 복원, 결제 취소)을 별도로 설계하고 테스트해야 하여 구현 비용이 단일 DB 트랜잭션 대비 수 배입니다.

## 관련 정보

- 관련 커밋: `5a802766`, `71272343`
- 관련 가이드: `Docs.Site/src/content/docs/guides/application/`
