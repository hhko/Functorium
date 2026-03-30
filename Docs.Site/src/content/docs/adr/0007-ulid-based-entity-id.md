---
title: "ADR-0007: Ulid 기반 Entity ID"
status: "accepted"
date: 2026-03-16
---

## 맥락과 문제

DDD에서 Entity는 고유 식별자(ID)로 구분되며, ID 타입 선택은 데이터베이스 쓰기 성능, 분산 환경 충돌 확률, 그리고 프레임워크의 타입 시스템 통합에 직접적인 영향을 미칩니다.

상품 테이블에 Guid v4를 PK로 사용하는 상황을 봅니다. 100만 건을 삽입하면 랜덤한 값이 B-Tree 인덱스의 중간 곳곳에 끼어들어 페이지 분할(page split)이 빈번하게 발생하고, 인덱스 단편화율이 급격히 상승합니다. auto-increment long으로 전환하면 순차 삽입이 보장되지만, 주문 서비스와 상품 서비스가 각각 독립 데이터베이스를 사용하는 분산 환경에서는 중앙 시퀀스 없이 ID 충돌을 피할 수 없습니다. Guid v7은 시간순 정렬이 가능하지만, Functorium이 사용하는 `[GenerateEntityId]` 소스 제너레이터 및 LanguageExt 타입 시스템과의 통합이 검증되지 않았습니다.

## 검토한 옵션

1. Ulid
2. Guid v4
3. Guid v7
4. long auto-increment

## 결정

**선택한 옵션: "Ulid"**. 타임스탬프 48비트가 시간순 정렬을 보장하여 B-Tree 인덱스의 페이지 분할을 최소화하고, 랜덤 80비트가 분산 환경에서도 충돌 없는 고유성을 제공합니다. 결정적으로 `[GenerateEntityId]` 소스 제너레이터와 통합되어 `ProductId`, `OrderId` 같은 타입 안전한 ID와 EF Core ValueConverter를 자동 생성할 수 있어, Functorium의 타입 시스템과 자연스럽게 결합됩니다.

### 결과

- Good, because 타임스탬프 기반 순차 정렬로 B-Tree 인덱스 끝에 순서대로 삽입되어, Guid v4 대비 페이지 분할이 크게 감소하고 대량 삽입 시 쓰기 성능이 안정적입니다.
- Good, because 랜덤 80비트의 고유성으로 주문 서비스/상품 서비스 등 독립 데이터베이스 환경에서도 중앙 시퀀스 없이 ID를 안전하게 생성합니다.
- Good, because `[GenerateEntityId]`와 통합되어 `ProductId.New()`, EF Core ValueConverter, `IParsable<T>` 구현이 자동 생성되므로 ID 타입마다 반복 코드를 작성할 필요가 없습니다.
- Good, because 26자 Crockford Base32 인코딩(`01ARZ3NDEKTSV4RRFFQ69G5FAV`)으로 URL-safe하고, Guid의 36자 대시 포함 형식보다 짧아 로그와 API 응답에서 가독성이 좋습니다.
- Bad, because .NET 표준 라이브러리에 Ulid가 내장되어 있지 않아 Cysharp/Ulid NuGet 패키지에 대한 외부 의존이 추가됩니다.
- Bad, because Guid를 PK로 기대하는 외부 시스템(Azure AD, 타사 API 등)과 통합 시 `Ulid.ToGuid()` / `Guid → Ulid` 변환 코드가 필요합니다.

### 확인

- Entity ID 타입이 Ulid 기반으로 생성되는지 확인합니다 (예: `ProductId.New()` 호출 결과가 Ulid 형식).
- `[GenerateEntityId]` 어트리뷰트가 적용된 ID 타입에 ValueConverter가 자동 생성되는지 확인합니다.
- B-Tree 인덱스에서 순차 삽입 시 페이지 분할이 발생하지 않는지 성능 테스트로 검증합니다.

## 옵션별 장단점

### Ulid

- Good, because 타임스탬프 48비트(밀리초 정밀도)가 시간순 정렬을 보장하고 랜덤 80비트가 동일 밀리초 내에서도 충돌을 방지하여, 정렬 가능성과 고유성을 동시에 달성합니다.
- Good, because 새 ID가 항상 B-Tree 인덱스의 끝에 추가되므로 페이지 분할이 최소화되고, 100만 건 이상의 대량 삽입에서도 인덱스 단편화 없이 안정적인 성능을 유지합니다.
- Good, because Crockford Base32 인코딩 26자(`01ARZ3NDEKTSV4RRFFQ69G5FAV`)로 URL-safe하며 대소문자 혼동이 없습니다.
- Good, because `[GenerateEntityId]`와 통합되어 `ProductId`, `OrderId` 등의 ValueConverter, `ToString`, `Parse`, 동등성 비교 코드가 자동 생성됩니다.
- Bad, because Cysharp/Ulid 패키지가 유지보수 중단되면 대체재를 찾거나 자체 fork해야 하는 외부 의존 위험이 있습니다.

### Guid v4

- Good, because `System.Guid`가 .NET 표준 라이브러리에 내장되어 추가 NuGet 의존 없이 즉시 사용 가능합니다.
- Good, because 대부분의 외부 시스템(Azure, AWS, 타사 API)이 Guid를 PK로 기대하므로 통합 시 변환이 불필요합니다.
- Bad, because 완전 랜덤 값이 B-Tree 인덱스의 임의 위치에 삽입되어, 대량 쓰기 시 페이지 분할과 인덱스 단편화가 누적되어 성능이 저하됩니다.
- Bad, because 시간 정보가 없어 ID만으로 생성 순서를 추론할 수 없고, 디버깅 시 "어떤 엔티티가 먼저 생성되었는지" 확인하려면 별도 타임스탬프 컬럼이 필요합니다.

### Guid v7

- Good, because 타임스탬프 기반 시간순 정렬이 가능하여 Ulid와 동등한 B-Tree 인덱스 성능을 기대할 수 있습니다.
- Good, because .NET 9의 `Guid.CreateVersion7()`으로 표준 라이브러리에서 지원되어 외부 의존이 없습니다.
- Bad, because `[GenerateEntityId]` 소스 제너레이터가 Ulid 기반으로 설계되어 있어, Guid v7으로 전환하면 ValueConverter, Parse, 동등성 비교 등의 코드 생성 로직을 재작성해야 합니다.
- Bad, because Functorium의 설계 시점에 Guid v7의 LanguageExt NewType 통합과 Crockford Base32 인코딩 지원이 검증되지 않았습니다.

### long auto-increment

- Good, because 8바이트로 Guid(16바이트)이나 Ulid(16바이트) 대비 저장 공간과 인덱스 크기가 절반이며, 완전한 순차 정렬로 B-Tree 쓰기 성능이 이론상 최적입니다.
- Good, because 정수 비교가 바이트 배열 비교보다 빨라 조인 및 조회 성능에서 미세한 이점이 있습니다.
- Bad, because 주문 서비스와 상품 서비스가 각각 독립 데이터베이스를 사용하면 시퀀스가 충돌하며, 이를 해결하려면 중앙 ID 발급 서비스가 필요하여 단일 장애점(SPOF)이 됩니다.
- Bad, because ID 값이 1, 2, 3으로 예측 가능하여 IDOR(Insecure Direct Object Reference) 공격에 취약합니다.
- Bad, because `INSERT` 후 `SCOPE_IDENTITY()`로 ID를 받아와야 하므로, 도메인 레이어에서 데이터베이스 없이 엔티티를 생성하고 테스트하는 것이 불가능합니다.

## 관련 정보

- 관련 커밋: `0470af7b` refactor(domains): GenerateEntityIdAttribute를 Entities 네임스페이스로 이동
- 관련 커밋: `adfa72c8` feat: IEntityId에 IParsable<T> 제약 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/domain/`
