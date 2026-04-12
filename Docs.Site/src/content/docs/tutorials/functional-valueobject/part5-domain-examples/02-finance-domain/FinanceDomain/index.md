---
title: "Finance Domain"
---
## Overview

Implements value objects commonly used in the finance domain.

---

## Learning Objectives

- `AccountNumber` - Account number (with masking)
- `InterestRate` - Interest rate (simple/compound interest calculation)
- `ExchangeRate` - Exchange rate (currency conversion)
- `TransactionType` - Transaction type (SmartEnum)

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/05-domain-examples/02-Finance-Domain/FinanceDomain
dotnet run
```

---

## Expected Output

```
=== Finance Domain Value Objects ===

1. AccountNumber
────────────────────────────────────────
   Account number: 110-1234567890
   Bank code: 110
   Masked: 110-****7890

2. InterestRate
────────────────────────────────────────
   Annual rate: 5.50%
   Principal: 1,000,000 KRW
   Period: 3 years
   Simple interest: 165,000 KRW
   Compound interest: 174,618 KRW

3. ExchangeRate
────────────────────────────────────────
   Rate: USD/KRW = 1350.5000
   100 USD = 135,050 KRW
   Inverse rate: KRW/USD = 0.0007

4. TransactionType
────────────────────────────────────────
   All transaction types:
      - DEPOSIT: Deposit (credit)
      - WITHDRAWAL: Withdrawal (debit)
      - TRANSFER: Transfer (debit)
      - INTEREST: Interest (credit)
      - FEE: Fee (debit)
```

## FAQ

### Q1: Why is the masking feature needed in `AccountNumber`?
**A**: Account numbers are sensitive financial information. Exposing the full number in log output or UI display creates security risks. When the value object provides a `Masked` property (`110-****7890`), it prevents developers from accidentally exposing the original value.

### Q2: Is it appropriate to include simple and compound interest calculations in the `InterestRate` value object?
**A**: Yes. Calculations related to interest rates are closely tied to the domain concept of `InterestRate`. Placing calculation logic externally separates the interest rate value from its calculations, making consistency difficult to maintain. Encapsulating it in the value object ensures calculations are always performed with the correct interest rate.

### Q3: Why does `ExchangeRate` provide an inverse rate?
**A**: If the USD/KRW exchange rate is 1,350, then KRW/USD is 1/1,350. Leaving the inverse rate calculation to the caller can lead to calculation errors. When the value object provides an `Inverse()` method, it guarantees an accurate inverse rate.

---

## Next Steps

Learn about value objects in the user management domain.

-> [5.3 User Management Domain](../../03-User-Management-Domain/UserManagementDomain/)
