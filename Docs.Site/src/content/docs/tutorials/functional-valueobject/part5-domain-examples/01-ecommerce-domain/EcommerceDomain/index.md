---
title: "E-commerce Domain"
---
## Overview

Implements value objects commonly used in the e-commerce domain.

---

## Learning Objectives

- `Money` - Composite comparable value object
- `ProductCode` - Simple value object
- `Quantity` - Comparable simple value object
- `OrderStatus` - Type-safe enumeration
- `ShippingAddress` - Composite value object + Apply pattern

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/05-domain-examples/01-Ecommerce-Domain/EcommerceDomain
dotnet run
```

---

## Expected Output

```
=== E-commerce Domain value objects ===

1. Money (Amount) - ComparableValueObject
────────────────────────────────────────
   Product price: 10,000 KRW
   Discount amount: 1,000 KRW
   Final price: 9,000 KRW
   Cross-currency addition attempt: Exception raised

2. ProductCode (Product Code) - SimpleValueObject
────────────────────────────────────────
   Product code: EL-001234
   Category: EL
   Number: 001234
   Invalid format: Product code format is invalid. (e.g., EL-001234)

3. Quantity - ComparableSimpleValueObject
────────────────────────────────────────
   Quantity 1: 5
   Quantity 2: 3
   Total: 8
   Comparison: 5 > 3 = True
   Sorted: [1, 3, 5]

4. OrderStatus - SmartEnum
────────────────────────────────────────
   Current status: Pending
   Cancellable: True
   After transition: Confirmed
   Shipping: Shipped, Cancellable: False

5. ShippingAddress - ValueObject
────────────────────────────────────────
   Recipient: Hong Gildong
   Address: Teheran-ro 123, Seoul
   Postal code: 06234
   Country: KR

   Empty address validation results:
      - Recipient name is required.
      - Street address is required.
      - City is required.
      - Postal code is required.
      - Country is required.
```

---

## Implemented value objects

| value object | Framework Type | Key Features |
|---------|----------------|----------|
| Money | ComparableValueObject | Per-currency operations, exception on cross-currency operations |
| ProductCode | SimpleValueObject | Regex validation, category parsing |
| Quantity | ComparableSimpleValueObject | Sortable, operator overloading |
| OrderStatus | SmartEnum | State transition logic, behavior included |
| ShippingAddress | ValueObject | Collects all errors via Apply pattern |

## FAQ

### Q1: Why is cross-currency operation handled as an exception in `Money`?
**A**: Adding different currencies like 1,000 KRW + 100 USD is a meaningless operation without an exchange rate. This is a predictable domain rule, but since the return type of operator overloading (`+`, `-`) cannot be `Fin<Money>`, exceptions are used.

### Q2: Why does `ShippingAddress` use the `Apply` pattern?
**A**: Shipping addresses require simultaneous validation of multiple fields: recipient, street, city, postal code, and country. The `Apply` pattern collects all field errors at once to inform the user. Using `Bind` would stop at the first error, leaving remaining errors unknown.

### Q3: Why does `ProductCode` provide the category code as a separate property?
**A**: In the product code `EL-001234`, the category (`EL`) and number (`001234`) are each meaningful domain information. Instead of parsing the string every time, the value object parses it at creation time and provides it as a property, allowing callers to use it safely and conveniently.

---

## Next Steps

Learn about value objects in the finance domain.

→ [5.2 Finance Domain](../../02-Finance-Domain/FinanceDomain/)
