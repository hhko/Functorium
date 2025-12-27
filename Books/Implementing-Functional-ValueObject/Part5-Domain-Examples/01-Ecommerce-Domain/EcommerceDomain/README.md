# 5.1 이커머스 도메인 🔴

> **Part 5: 도메인별 실전 예제** | [← 이전: 4.4 테스트 전략](../../../04-practical-guide/04-Testing-Strategies/TestingStrategies/README.md) | [목차](../../../README.md) | [다음: 5.2 금융 도메인 →](../../02-Finance-Domain/FinanceDomain/README.md)

---

## 개요

이커머스 도메인에서 자주 사용되는 값 객체를 구현합니다.

---

## 학습 목표

- `Money` - 복합 비교 가능 값 객체
- `ProductCode` - 단순 값 객체
- `Quantity` - 비교 가능 단순 값 객체
- `OrderStatus` - 타입 안전 열거형
- `ShippingAddress` - 복합 값 객체 + Apply 패턴

---

## 실행 방법

```bash
cd Books/Functional-ValueObject/05-domain-examples/01-Ecommerce-Domain/EcommerceDomain
dotnet run
```

---

## 예상 출력

```
=== 이커머스 도메인 값 객체 ===

1. Money (금액) - ComparableValueObject
────────────────────────────────────────
   상품 가격: 10,000 KRW
   할인 금액: 1,000 KRW
   최종 가격: 9,000 KRW
   다른 통화 합산 시도: 예외 발생

2. ProductCode (상품 코드) - SimpleValueObject
────────────────────────────────────────
   상품 코드: EL-001234
   카테고리: EL
   번호: 001234
   잘못된 형식: 상품 코드 형식이 올바르지 않습니다. (예: EL-001234)

3. Quantity (수량) - ComparableSimpleValueObject
────────────────────────────────────────
   수량 1: 5
   수량 2: 3
   합계: 8
   비교: 5 > 3 = True
   정렬: [1, 3, 5]

4. OrderStatus (주문 상태) - SmartEnum
────────────────────────────────────────
   현재 상태: 대기중
   취소 가능: True
   전이 후: 확인됨
   배송 중: 배송중, 취소 가능: False

5. ShippingAddress (배송 주소) - ValueObject
────────────────────────────────────────
   수령인: 홍길동
   주소: 테헤란로 123, 서울
   우편번호: 06234
   국가: KR

   빈 주소 검증 결과:
      - 수령인 이름은(는) 필수입니다.
      - 도로명 주소은(는) 필수입니다.
      - 도시은(는) 필수입니다.
      - 우편번호는 필수입니다.
      - 국가는 필수입니다.
```

---

## 구현된 값 객체

| 값 객체 | 프레임워크 타입 | 주요 특징 |
|---------|----------------|----------|
| Money | ComparableValueObject | 통화별 연산, 다른 통화 연산 시 예외 |
| ProductCode | SimpleValueObject | 정규식 검증, 카테고리 파싱 |
| Quantity | ComparableSimpleValueObject | 정렬 가능, 연산자 오버로딩 |
| OrderStatus | SmartEnum | 상태 전이 로직, 행위 포함 |
| ShippingAddress | ValueObject | Apply 패턴으로 모든 오류 수집 |

---

## 다음 단계

금융 도메인의 값 객체를 학습합니다.

→ [5.2 금융 도메인](../../02-Finance-Domain/FinanceDomain/README.md)
