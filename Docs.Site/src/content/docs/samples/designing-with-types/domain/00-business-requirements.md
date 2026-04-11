---
title: "Business Requirements"
---

## Background

Various contacts such as customers, partners, and suppliers need to be managed systematically. Each contact requires accurate personal information and at least one valid contact method. Contacts have a lifecycle of creation, modification, and deletion, and email uniqueness must be guaranteed. To solve the problems that the naive code in the [overview](../) allows, business rules must first be clearly defined.

## Domain Terminology

| Term | English | Definition |
|------|---------|------------|
| Contact | Contact | A unit of contact information to be managed |
| Personal Name | PersonalName | Composed of first name (required), last name (required), and middle initial (optional) |
| Email Address | EmailAddress | An address in standard email format |
| Postal Address | PostalAddress | Composed of street address, city, state code (2-letter uppercase), and zip code (5-digit numeric) |
| Contact Method | ContactInfo | A combination of email only, postal address only, or both |
| Email Verification | EmailVerification | The unverified/verified status of an email and the verification timestamp |
| Note | ContactNote | Free-form text about a contact (500 characters max) |

## Business Rules

### 1. Data Validity

- First name and last name must be 50 characters or fewer
- Email must be in standard email format
- State code must be a 2-letter uppercase alphabetic string
- Zip code must be a 5-digit numeric string
- Note content must be 500 characters or fewer

### 2. Contact Methods

- A contact must have at least one contact method
- Possible combinations: email only, postal address only, or both email and postal address
- A contact without any contact method cannot exist

### 3. Email Verification

- A newly registered email starts in an unverified state
- An unverified email can be verified, and the verification timestamp is recorded
- Verification is one-way — a verified email cannot be reverted to unverified
- An already-verified email cannot be verified again

### 4. Contact Lifecycle Management

- A contact's name can be changed
- A contact can be soft-deleted, recording the deleter and deletion timestamp
- A deleted contact can be restored
- Name changes, email verification, and note addition/removal are not allowed on deleted contacts
- Deletion and restoration are idempotent — deleting an already-deleted contact has no side effects

### 5. Note Management

- Notes can be added to a contact
- Notes can be removed from a contact
- Note content must be 500 characters or fewer
- Notes cannot be added to or removed from a deleted contact
- Removing a non-existent note has no side effects (idempotent)

### 6. Email Uniqueness

- Two or more contacts with the same email cannot exist
- When updating a contact, the email uniqueness check excludes the contact itself

## Scenarios

### Normal Scenarios

1. **Register with email only, then verify** — Create a contact with first name, last name, and email. The email starts in an unverified state. After verification, the verification timestamp is recorded.
2. **Register with postal address only** — Create a contact with first name, last name, street address, city, state code, and zip code.
3. **Register with both email and postal address** — Create a contact with first name, last name, email, street address, city, state code, and zip code.
4. **Change name** — Change the name of a created contact. The change timestamp is recorded.
5. **Add/remove notes** — Add a note to a contact, then remove that note.
6. **Soft delete and restore** — Delete a contact. The deleter and timestamp are recorded. After restoration, the deletion information is cleared.

### Rejection Scenarios

7. **Register without any contact method** — If only first and last name are provided with no email or postal address, registration is rejected.
8. **Re-verify a verified email** — Attempting to verify an already-verified email is rejected.
9. **Modify a deleted contact** — Attempting name changes, email verification, or note additions on a deleted contact is rejected.
10. **Register with duplicate email** — Creating a new contact with an email already in use by another contact is rejected.

## States That Must Not Exist

- Verified state without an email
- A contact with no contact methods at all
- A verified email that has reverted to unverified
- Actions performed on a deleted contact
- Two or more contacts with the same email

In the next step, these rules are classified as invariants and [type strategies](./01-type-design-decisions/) are derived for each category.
