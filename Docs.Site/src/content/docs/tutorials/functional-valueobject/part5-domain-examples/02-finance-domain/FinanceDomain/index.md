---
title: "Finance Domain"
---
## Overview

금융 도메인에서 자주 사용되는 value object를 implements.

---

## Learning Objectives

- `AccountNumber` - 계좌번호 (마스킹 포함)
- `InterestRate` - 이자율 (단리/복리 계산)
- `ExchangeRate` - 환율 (통화 변환)
- `TransactionType` - 거래 유형 (SmartEnum)

---

## 실행 방법

```bash
cd Docs/tutorials/Functional-ValueObject/05-domain-examples/02-Finance-Domain/FinanceDomain
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

## FAQ

### Q1: `AccountNumber`에서 마스킹 기능이 필요한 이유는 무엇인가요?
**A**: 계좌번호는 민감한 금융 정보입니다. 로그 출력이나 UI 표시 시 전체 번호를 노출하면 보안 위험이 생깁니다. value object가 `Masked` 속성(`110-****7890`)을 제공하면, 개발자가 실수로 원본 값을 노출하는 것을 방지할 수 있습니다.

### Q2: `InterestRate`에서 단리와 복리 계산을 value object에 포함하는 것이 적절한가요?
**A**: 네. 이자율과 관련된 계산은 `InterestRate`라는 도메인 개념에 밀접하게 연결됩니다. 계산 로직을 외부에 두면 이자율 값과 계산이 분리되어 일관성 유지가 어려워집니다. value object에 캡슐화하면 항상 올바른 이자율로 계산이 수행됩니다.

### Q3: `ExchangeRate`에서 역환율을 제공하는 이유는 무엇인가요?
**A**: USD/KRW 환율이 1,350이면 KRW/USD는 1/1,350입니다. 역환율 계산을 호출 측에 맡기면 계산 오류가 발생할 수 있습니다. value object가 `Inverse()` 메서드를 제공하면 정확한 역환율을 보장할 수 있습니다.

---

## Next Steps

사용자 관리 도메인의 value object를 학습합니다.

→ [5.3 사용자 관리 도메인](../../03-User-Management-Domain/UserManagementDomain/)
