---
title: "ADR-0008: Ulid 기반 Entity ID"
status: "accepted"
date: 2026-03-16
---

## 맥락과 문제

DDD에서 Entity는 고유 식별자(ID)로 구분됩니다. ID 타입 선택은 정렬 가능성, 충돌 확률, 데이터베이스 인덱스 성능, 분산 환경 호환성, 그리고 프레임워크의 타입 시스템 통합에 영향을 미칩니다.

Guid v4는 비순차적이어서 B-Tree 인덱스에서 페이지 분할(page split)과 단편화를 유발합니다. auto-increment long은 순차적이지만 분산 환경에서 중앙 조정 없이 충돌을 피할 수 없습니다. Guid v7은 시간순 정렬이 가능하지만 LanguageExt의 타입 시스템과의 통합이 제한적입니다.

## 검토한 옵션

1. Ulid
2. Guid v4
3. Guid v7
4. long auto-increment

## 결정

**선택한 옵션: "Ulid"**, 시간순 정렬(타임스탬프 48비트)로 B-Tree 인덱스에 친화적이고, 128비트 고유성으로 분산 환경에서 충돌 위험이 없으며, `[GenerateEntityId]` 소스 제너레이터를 통해 EF Core ValueConverter와 타입 안전한 ID를 자동 생성할 수 있기 때문입니다.

### 결과

- Good, because 시간순 정렬로 B-Tree 인덱스 페이지 분할이 최소화되어 쓰기 성능이 향상됩니다.
- Good, because 128비트 랜덤성으로 분산 환경에서도 중앙 조정 없이 ID를 생성할 수 있습니다.
- Good, because `[GenerateEntityId]`로 ProductId, OrderId 등 타입 안전한 ID와 EF Core ValueConverter가 자동 생성됩니다.
- Good, because 26자 Crockford Base32 인코딩으로 URL-safe하며 가독성이 좋습니다.
- Bad, because .NET 표준 라이브러리에 포함되지 않아 별도 NuGet 패키지(Cysharp/Ulid)에 의존합니다.
- Bad, because Guid를 기대하는 외부 시스템과 통합 시 변환이 필요합니다.

### 확인

- Entity ID 타입이 Ulid 기반으로 생성되는지 확인합니다 (예: `ProductId.New()` 호출 결과가 Ulid 형식).
- `[GenerateEntityId]` 어트리뷰트가 적용된 ID 타입에 ValueConverter가 자동 생성되는지 확인합니다.
- B-Tree 인덱스에서 순차 삽입 시 페이지 분할이 발생하지 않는지 성능 테스트로 검증합니다.

## 옵션별 장단점

### Ulid

- Good, because 타임스탬프 48비트 + 랜덤 80비트로 시간순 정렬과 고유성을 동시에 보장합니다.
- Good, because B-Tree 인덱스에 친화적이어서 대량 삽입 시 성능이 안정적입니다.
- Good, because Crockford Base32 인코딩으로 26자 문자열 표현이 URL-safe합니다.
- Good, because `[GenerateEntityId]`와 통합되어 ValueConverter 보일러플레이트가 제거됩니다.
- Bad, because 외부 패키지(Cysharp/Ulid) 의존이 필요합니다.

### Guid v4

- Good, because .NET 표준 라이브러리에 내장되어 별도 의존이 없습니다.
- Good, because 널리 사용되어 외부 시스템과의 호환성이 높습니다.
- Bad, because 비순차적이어서 B-Tree 인덱스에서 페이지 분할과 단편화가 발생합니다.
- Bad, because 시간 정보가 없어 생성 순서를 추론할 수 없습니다.

### Guid v7

- Good, because 시간순 정렬이 가능하여 B-Tree 친화적입니다.
- Good, because .NET 9부터 표준 라이브러리에서 지원됩니다.
- Bad, because LanguageExt의 NewType/타입 시스템과의 통합이 검증되지 않았습니다.
- Bad, because 도입 시점에 .NET 생태계에서의 지원이 제한적이었습니다.

### long auto-increment

- Good, because 8바이트로 가장 작은 저장 공간을 차지합니다.
- Good, because 완전한 순차 정렬로 B-Tree 성능이 최적입니다.
- Bad, because 분산 환경에서 중앙 시퀀스 조정 없이 충돌을 피할 수 없습니다.
- Bad, because ID 값이 예측 가능하여 보안 관점에서 취약합니다.
- Bad, because 엔티티 생성 전에 데이터베이스 접근이 필요합니다.

## 관련 정보

- 관련 커밋: `0470af7b` refactor(domains): GenerateEntityIdAttribute를 Entities 네임스페이스로 이동
- 관련 커밋: `adfa72c8` feat: IEntityId에 IParsable<T> 제약 추가
- 관련 문서: `Docs.Site/src/content/docs/guides/domain/`
