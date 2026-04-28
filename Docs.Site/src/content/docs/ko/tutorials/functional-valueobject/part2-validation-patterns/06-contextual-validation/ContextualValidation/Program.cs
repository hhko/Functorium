using ContextualValidation.Examples;

// 성공 케이스
var validPhone = PhoneNumberValidation.Validate("010-1234-5678");
Console.WriteLine($"Phone: {validPhone}");

// 실패 케이스 — 에러에 필드 이름 포함
var invalidPhone = PhoneNumberValidation.Validate("");
Console.WriteLine($"Invalid Phone: {invalidPhone}");

// Address — 다중 필드 병렬 검증
var validAddress = AddressValidation.Validate("서울", "강남대로 123", "06000");
Console.WriteLine($"Address: {validAddress}");

// 다중 필드 실패 — 모든 오류 수집
var invalidAddress = AddressValidation.Validate(null, "", "1");
Console.WriteLine($"Invalid Address: {invalidAddress}");
