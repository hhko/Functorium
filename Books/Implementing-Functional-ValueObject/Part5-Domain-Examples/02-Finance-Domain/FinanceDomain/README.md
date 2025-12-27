# 5.2 금융 도메인 🔴

> **Part 5: 도메인별 실전 예제** | [← 이전: 5.1 이커머스 도메인](../../01-Ecommerce-Domain/EcommerceDomain/README.md) | [목차](../../../README.md) | [다음: 5.3 사용자 관리 도메인 →](../../03-User-Management-Domain/UserManagementDomain/README.md)

---

## 개요

금융 도메인에서 자주 사용되는 값 객체를 구현합니다.

---

## 학습 목표

- `AccountNumber` - 계좌번호 (마스킹 포함)
- `InterestRate` - 이자율 (단리/복리 계산)
- `ExchangeRate` - 환율 (통화 변환)
- `TransactionType` - 거래 유형 (SmartEnum)

---

## 실행 방법

```bash
cd Books/Functional-ValueObject/05-domain-examples/02-Finance-Domain/FinanceDomain
dotnet run
```

---

## 예상 출력

```
=== 금융 도메인 값 객체 ===

1. AccountNumber (계좌번호)
────────────────────────────────────────
   계좌번호: 110-1234567890
   은행 코드: 110
   마스킹: 110-****7890

2. InterestRate (이자율)
────────────────────────────────────────
   연이율: 5.50%
   원금: 1,000,000원
   기간: 3년
   단리 이자: 165,000원
   복리 이자: 174,618원

3. ExchangeRate (환율)
────────────────────────────────────────
   환율: USD/KRW = 1350.5000
   100 USD = 135,050 KRW
   역환율: KRW/USD = 0.0007

4. TransactionType (거래 유형)
────────────────────────────────────────
   모든 거래 유형:
      - DEPOSIT: 입금 (입금)
      - WITHDRAWAL: 출금 (출금)
      - TRANSFER: 이체 (출금)
      - INTEREST: 이자 (입금)
      - FEE: 수수료 (출금)
```

---

## 다음 단계

사용자 관리 도메인의 값 객체를 학습합니다.

→ [5.3 사용자 관리 도메인](../../03-User-Management-Domain/UserManagementDomain/README.md)
