---
title: "ADR-0013: Application - Validation Order: Normalize-Then-MaxLength"
status: "accepted"
date: 2026-03-26
---

## Context and Problem

A user entered `"  Hello  "` in the product name input field. The meaningful data is `"Hello"` (5 characters), but MaxLength(5) validation executed before Trim, judging it as 9 characters including leading and trailing spaces, and returned an error message "exceeds 5 characters" to the user. From the user's perspective, they clearly entered 5 characters but were rejected. Conversely, storing the original without normalization to the DB causes `"Hello"` and `"  Hello  "` to be treated as different values, accumulating duplicate data without Unique constraint violations, and exact-match searches fail to find results.

Since the execution order of validation and normalization determines the outcome, the correct order must be enforced at the pipeline level.

## Considered Options

- **Option 1**: MaxLength check then Normalize (current order)
- **Option 2**: Normalize then MaxLength check
- **Option 3**: No normalization (store raw input)
- **Option 4**: Single method performing both normalization and validation simultaneously

## Decision

**Option 2: Fix the order to `ThenNormalize(Trim)` followed by `ThenMaxLength`.**

The principle is "clean first, then apply rules." The validation pipeline enforces the following order at the API level:
1. `ThenNormalize(Trim)` -- Normalize the input first (whitespace removal, etc.)
2. `ThenMaxLength(n)` -- Validate length against the actual normalized data

The `CreateFromValidated` factory method only accepts data that has completed normalization. Value Objects cannot be created with raw strings that have not gone through normalization, structurally blocking pipeline bypass.

### Consequences

- **Positive**: A user entering `"  Hello  "` passes MaxLength(5) validation normally and is stored as the trimmed `"Hello"`. No whitespace-contaminated data accumulates in the DB, so search, comparison, and Unique constraints all work as intended. The validation pipeline's order is fixed by the API, preventing developers from accidentally reversing the order.
- **Negative**: All Value Objects with existing MaxLength-first validation pipelines must be updated. The normalization stage adds one more step to the pipeline.

### Confirmation

- Verify that `"  Hello  "` (with spaces) passes MaxLength(5) and is stored as the trimmed `"Hello"`.
- Verify that `"  Hello World  "` (11 characters after trimming) is correctly rejected by MaxLength(5).
- Verify that passing an untrimmed string to `CreateFromValidated` is rejected.

## Pros and Cons of the Options

### Option 1: MaxLength Then Normalize

- **Pros**: No existing code changes needed. Validating the raw input length first can block extreme inputs like 1 million spaces early.
- **Cons**: `"  Hello  "` (9 characters) is rejected by MaxLength(5), causing a false rejection where an actually valid 5-character input returns a "length exceeded" error to the user. Data that would pass after normalization is dropped at the validation stage.

### Option 2: Normalize Then MaxLength

- **Pros**: After trimming, `"Hello"` (5 characters) is validated against the actual normalized data, eliminating false rejections. The validated value and stored value are exactly aligned. Users do not see unnecessary error messages, improving UX.
- **Cons**: Extremely long inputs (e.g., 1 million spaces) could proceed through the normalization stage. This can be addressed with a separate raw length upper bound check (e.g., raw length 10,000 character limit).

### Option 3: No Normalization

- **Pros**: No normalization logic is needed, making the pipeline the simplest.
- **Cons**: `"Hello"` and `"  Hello  "` are stored as separate values in the DB, accumulating duplicate data without Unique constraint violations. `WHERE name = 'Hello'` searches miss whitespace-padded data. Comparison, sorting, and index behavior are all distorted by whitespace.

### Option 4: Single Method Performing Both Normalization and Validation

- **Pros**: Order mistakes are inherently impossible. `ValidateAndNormalize(input, maxLength: 5)` completes in one call.
- **Cons**: Normalization strategies cannot be flexibly combined -- whether to only Trim, Trim + ToLower, or also remove special characters. Validation rules and normalization logic are coupled in a single method, violating the Single Responsibility Principle. Pipeline compositions like `ThenNormalize().ThenMaxLength().ThenMatches()` become impossible.

## Related Information

- Commits: cab7819a, 991500d5
