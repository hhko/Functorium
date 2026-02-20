# 도메인 용어 사전 (Domain Glossary)

## 1. 도메인 개요

이 도메인은 **단일 Bounded Context(전자상거래 주문 처리)**로 모델링한다.
**고객(Customer)**, **상품(Product)**, **주문(Order)**, **재고(Inventory)**, **태그(Tag)** 5개 핵심 영역으로 구성된다.

> **Bounded Context 경계**: 현재 모든 Aggregate는 동일한 Bounded Context에 속하며, 단일 배포 단위로 관리된다. 향후 도메인이 확장되면 주문/재고를 별도 Context로 분리할 수 있다.

## 2. 도메인 용어 사전 (Ubiquitous Language)

| 용어 | 영문 | 유형 | 정의 |
|------|------|------|------|
| 고객 | Customer | 애그리거트 루트 | 주문을 생성할 수 있는 주체. 이름, 이메일, 신용한도를 가진다 |
| 고객명 | CustomerName | 값 객체 | 고객의 이름 (최대 100자, 공백 불가) |
| 이메일 | Email | 값 객체 | 고객의 이메일 주소. 형식 검증 포함 (최대 320자) |
| 신용한도 | CreditLimit (Money) | 값 객체 | 고객이 주문할 수 있는 최대 총 금액 |
| 상품 | Product | 애그리거트 루트 | 판매 대상. 이름, 설명, 가격, 태그ID 목록을 가진다 |
| 상품명 | ProductName | 값 객체 | 상품의 이름 (최대 100자, 공백 불가) |
| 상품설명 | ProductDescription | 값 객체 | 상품의 상세 설명 (최대 1000자, 빈 값 허용) |
| 태그 | Tag | 애그리거트 루트 | 상품에 부여할 수 있는 분류 라벨. 독립 생명주기를 가지며 ITagRepository로 관리된다. 이름(TagName)을 가진다 |
| 태그명 | TagName | 값 객체 | 태그의 이름 (최대 50자, 공백 불가) |
| 주문 | Order | 애그리거트 루트 | 고객이 상품을 구매하는 행위의 기록. 다중 주문라인을 포함한다 |
| 주문라인 | OrderLine | 엔티티 | 주문 내 개별 상품의 수량·단가·소계. Order의 Child Entity |
| 주문상태 | OrderStatus | 값 객체 | 주문의 처리 상태 (Pending/Confirmed/Shipped/Delivered/Cancelled). Smart Enum 패턴 |
| 배송주소 | ShippingAddress | 값 객체 | 주문의 배송 목적지 (최대 500자, 공백 불가) |
| 재고 | Inventory | 애그리거트 루트 | 특정 상품의 현재 보유 수량. 동시성 제어 포함 |
| 금액 | Money | 공유 값 객체 | 양수인 화폐 금액. `Zero` 항등원, `Add`/`Subtract`(`Fin<Money>` 반환)/`Multiply(decimal)`/`Sum(IEnumerable<Money>)` 연산 가능 |
| 수량 | Quantity | 공유 값 객체 | 0 이상의 정수. 덧셈/뺄셈 연산 가능 (뺄셈 시 0 클램핑 — 예외 대신 `Math.Max(0, Value - amount)`) |

## 3. 애그리거트별 상세

### 고객(Customer)

- **속성**: 고객명, 이메일, 신용한도
- **기능**: 고객 생성, 신용한도 변경(`UpdateCreditLimit` → `Customer` 반환, fluent + `UpdatedAt` 갱신), 이메일 변경(`ChangeEmail` → `Customer` 반환, fluent + `UpdatedAt` 갱신)
- **불변 조건**:
  - 고객명은 비어있을 수 없으며 최대 100자
  - 이메일은 유효한 형식이어야 하며 최대 320자, 소문자로 정규화
  - 신용한도는 반드시 양수
- **조회 규칙**: 이메일로 고객 존재 여부 확인 가능 (CustomerEmailSpec)

### 상품(Product)

- **속성**: 상품명, 상품설명, 가격(Money), 태그ID 목록(TagId 참조)
- **기능**: 상품 생성, 상품 수정(이름/설명/가격) → `Product` 반환(fluent) + `UpdatedAt` 갱신, 태그 할당(`AssignTag`), 태그 해제(`UnassignTag` — 존재하지 않는 태그 해제 시 no-op, 멱등)
- **불변 조건**:
  - 상품명은 비어있을 수 없으며 최대 100자
  - 상품설명은 최대 1000자 (빈 값 허용)
  - 가격은 반드시 양수
  - 동일 태그 중복 부여 불가 (태그 ID 기준 멱등성)
- **조회 규칙**:
  - 상품명 정확 일치 검색 (ProductNameSpec)
  - 상품명 고유성 검증 — 수정 시 자기 자신 제외 (ProductNameUniqueSpec)
  - 가격 범위 검색 (ProductPriceRangeSpec) — `minPrice` ≤ 가격 ≤ `maxPrice`

### 주문(Order)

- **속성**: 고객ID(CustomerId), 주문라인 목록(OrderLines), 총액(TotalAmount), 배송주소, 주문상태(OrderStatus)
- **기능**: 주문 생성(`Create` → `Fin<Order>`), 주문 확인(`Confirm` → `Fin<Unit>`), 주문 배송(`Ship` → `Fin<Unit>`), 주문 배달완료(`Deliver` → `Fin<Unit>`), 주문 취소(`Cancel` → `Fin<Unit>`)
- **자식 엔티티 — 주문라인(OrderLine)**:
  - **속성**: 상품ID(ProductId), 수량(Quantity), 단가(UnitPrice), 소계(LineTotal)
  - **불변 조건**: 주문라인 수량은 반드시 0보다 커야 한다
  - **소계 자동 계산**: LineTotal = UnitPrice × Quantity
- **불변 조건**:
  - 주문라인은 최소 1개 이상 필요
  - **총액 = 모든 주문라인의 LineTotal 합계** (자동 계산, 직접 지정 불가)
  - 배송주소는 비어있을 수 없으며 최대 500자
  - **상태 전이 규칙**: Pending → Confirmed → Shipped → Delivered, Pending/Confirmed → Cancelled (허용되지 않는 전이 시 "InvalidOrderStatusTransition" 오류 — `Fin<Unit>`로 반환)
- **교차 규칙**: 주문은 고객(CustomerId)을 ID로만 참조. 주문라인은 상품(ProductId)을 ID로만 참조 (다른 애그리거트를 직접 포함하지 않음)
- **설계 결정**: 주문 생성 후 주문라인 변경 불가 — 주문라인은 생성 시점에 확정되며, 이후 추가/수정/삭제를 허용하지 않는다

### 재고(Inventory)

- **속성**: 상품ID, 보유수량(StockQuantity), 버전(RowVersion)
- **기능**: 재고 생성, 재고 차감(`DeductStock` → `Fin<Unit>`), 재고 추가(`AddStock` → `Inventory` 반환, fluent)
- **불변 조건**:
  - 보유수량은 0 이상의 정수
  - **재고 차감 시 보유수량보다 많은 수량을 차감할 수 없음** (부족 시 "InsufficientStock" 오류 — `Fin<Unit>`로 반환)
  - 모든 수량 변경 시 수정 시각 갱신
- **동시성**: 낙관적 잠금(RowVersion)으로 동시 재고 변경 충돌 감지
- **조회 규칙**: 임계값 미만 재고 탐지 (InventoryLowStockSpec) — `StockQuantity < threshold` (미만, 이하 아님)

### 태그(Tag)

- **속성**: 태그명(TagName), CreatedAt, UpdatedAt (IAuditable)
- **기능**: 태그 생성, 태그 이름 변경(`Rename` → `Tag` 반환, fluent + `UpdatedAt` 갱신)
- **생명주기 관리**: 독립 Aggregate Root로서 ITagRepository를 통해 생성/조회/수정/삭제 가능. Product는 TagId만 참조하며 Tag 객체를 직접 포함하지 않는다.
- **설계 결정 근거**: Tag는 독립 생명주기를 가지므로 Product의 하위 Entity가 아닌 독립 Aggregate Root로 모델링했다. Product는 TagId만 참조한다.

## 4. 애그리거트 간 관계

```
범례: 이름 = 애그리거트 루트 | [이름] = 자식 엔티티
범례: ──▶ = ID 참조/포함 | ┄┄▷ = 도메인 서비스 검증

Customer ──(고객ID 참조)──▶ Order ──(포함)──▶ [OrderLine] ◀──(상품ID 참조)── Product ──(태그ID 참조)──▶ Tag
                              ┆
                              ┆ (신용한도 검증)
                              ▽
                              Inventory ◀──(상품ID 참조)── Product
```

- **고객 → 주문**: 주문은 고객ID를 참조. 고객의 신용한도 내에서만 주문 가능 (도메인 서비스로 검증)
- **주문 → 주문라인**: Order가 OrderLine 컬렉션을 포함 (Aggregate 경계 내 Child Entity)
- **주문라인 → 상품**: 주문라인은 상품 ID만 참조 (느슨한 결합)
- **재고 → 상품**: 재고는 상품 ID만 참조. 상품과 분리된 이유는 변경 빈도가 높기 때문
- **상품 → 태그**: 상품은 TagId만 참조. Tag는 독립 Aggregate Root로서 별도 Repository로 관리

## 5. 도메인 서비스 — 교차 애그리거트 비즈니스 규칙

### 주문 신용한도 검증 (OrderCreditCheckService)

- **목적**: 고객의 주문 총액이 신용한도를 초과하지 않도록 보장
- **규칙 1**: 단일 주문 금액이 고객 신용한도를 초과하면 거부 ("CreditLimitExceeded")
- **규칙 2**: 기존 주문 총액 + 신규 주문 금액이 고객 신용한도를 초과하면 거부 — 기존 주문은 `Seq<Order>`(LanguageExt 불변 시퀀스)로 전달, `Money.Sum`으로 합산
- 이 규칙은 Customer와 Order 두 애그리거트에 걸쳐 있으므로 도메인 서비스로 구현
- **반환 타입**: `Fin<Unit>` (성공 시 `Unit`, 실패 시 `Error`)

### Invariant 검증 위치 설계 결정

| 검증 유형 | 위치 | 근거 |
|-----------|------|------|
| 단일 Aggregate 규칙 (DeductStock) | Inventory Aggregate 내부 | 재고 차감은 Inventory 자체 불변 조건이므로 Aggregate 내부에서 검증 |
| 단일 Aggregate 규칙 (Order 상태 전이) | Order Aggregate 내부 | 상태 전이 가드는 Order 자체 불변 조건이므로 Aggregate 내부에서 검증 (DeductStock과 동일 패턴) |
| 교차 Aggregate 규칙 (CreditCheck) | 도메인 서비스 (OrderCreditCheckService) | Customer의 신용한도와 Order의 금액을 함께 검증해야 하므로, 어느 한쪽 Aggregate에 넣으면 다른 Aggregate에 대한 의존성이 생김. 도메인 서비스로 분리하여 양쪽 Aggregate를 순수 함수적으로 검증 |

## 6. 도메인 이벤트 (상태 변경 시 발행)

| 이벤트 | 발생 시점 | 포함 정보 |
|--------|-----------|-----------|
| Customer.CreatedEvent | 고객 생성 | 고객ID, 고객명, 이메일 |
| Customer.CreditLimitUpdatedEvent | 신용한도 변경 | 고객ID, 이전 신용한도, 새 신용한도 |
| Customer.EmailChangedEvent | 이메일 변경 | 고객ID, 이전 이메일, 새 이메일 |
| Product.CreatedEvent | 상품 생성 | 상품ID, 상품명, 가격 |
| Product.UpdatedEvent | 상품 수정 | 상품ID, 상품명, 변경 전 가격, 변경 후 가격 |
| Product.TagAssignedEvent | 태그 할당 | 상품ID, 태그ID |
| Product.TagUnassignedEvent | 태그 해제 | 상품ID, 태그ID |
| Tag.CreatedEvent | 태그 생성 | 태그ID, 태그명 |
| Tag.RenamedEvent | 태그 이름 변경 | 태그ID, 이전 태그명, 새 태그명 |
| Order.CreatedEvent | 주문 생성 | 주문ID, 고객ID, 주문라인 목록(Seq&lt;OrderLineInfo&gt;), 총액 |
| Order.ConfirmedEvent | 주문 확인 | 주문ID |
| Order.ShippedEvent | 주문 배송 | 주문ID |
| Order.DeliveredEvent | 주문 배달 완료 | 주문ID |
| Order.CancelledEvent | 주문 취소 | 주문ID |
| Inventory.CreatedEvent | 재고 생성 | 재고ID, 상품ID, 수량 |
| Inventory.StockDeductedEvent | 재고 차감 | 재고ID, 상품ID, 차감 수량 |
| Inventory.StockAddedEvent | 재고 추가 | 재고ID, 상품ID, 추가 수량 |

## 7. 핵심 제약조건(불변 조건) 요약

| 분류 | 제약조건 | 오류 |
|------|----------|------|
| 금액 | 반드시 양수. `Subtract` 결과가 음수이면 `Fin<Money>` Fail 반환 | 검증 실패 |
| 수량 | 0 이상 정수, 뺄셈 시 0 클램핑 | 검증 실패 |
| 주문라인 수량 | 주문라인의 수량은 반드시 0보다 커야 함 | InvalidQuantity |
| 주문라인 최소 개수 | 주문에는 최소 1개의 주문라인 필요 | EmptyOrderLines |
| 이메일 | `^[^@\s]+@[^@\s]+\.[^@\s]+$` 정규식 검증, 소문자 정규화 | 검증 실패 |
| 주문 총액 | 모든 주문라인 LineTotal 합계로 자동 계산 | - |
| 신용한도 초과 | 고객의 기존 주문 총액 + 신규 주문 ≤ 신용한도 | CreditLimitExceeded |
| 재고 부족 | 차감 수량 ≤ 현재 보유수량 | InsufficientStock |
| 주문 상태 전이 | Pending→Confirmed→Shipped→Delivered, Pending/Confirmed→Cancelled만 허용 | InvalidOrderStatusTransition |
| 상품명 고유성 | 동일 상품명 중복 불가 | 스펙으로 검증 |
| 이메일 고유성 | 동일 이메일 중복 불가 | 스펙으로 검증 |
| 태그 중복 | 동일 상품에 같은 태그 중복 부여 불가 (멱등) | 무시(멱등 처리) |
| 재고 동시성 | 낙관적 잠금(RowVersion)으로 충돌 감지 | 동시성 충돌 오류 |

---

> 구현 패턴(값 객체, 애그리거트, 리포지토리, Specification, 함수형 타입, 도메인 이벤트 등)은
> [DOMAIN-IMPLEMENTATION-PATTERNS.md](../../../../../../Src/Functorium/Domains/DOMAIN-IMPLEMENTATION-PATTERNS.md)를 참조한다.
