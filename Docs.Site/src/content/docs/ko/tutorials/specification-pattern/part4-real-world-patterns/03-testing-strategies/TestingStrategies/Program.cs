Console.WriteLine("=== Specification Testing Strategies ===\n");

Console.WriteLine("Specification 테스트는 3가지 레벨로 구성됩니다:\n");

Console.WriteLine("Level 1: Spec 자체 테스트 (Self Tests)");
Console.WriteLine("  - 개별 Specification의 IsSatisfiedBy() 경계값 테스트");
Console.WriteLine("  - 만족/불만족 경계를 Theory + InlineData로 검증\n");

Console.WriteLine("Level 2: 조합 테스트 (Composition Tests)");
Console.WriteLine("  - And, Or, Not 조합의 정확한 동작 검증");
Console.WriteLine("  - 실제 데이터로 복합 조건 테스트\n");

Console.WriteLine("Level 3: Usecase 테스트 (Usecase Tests)");
Console.WriteLine("  - Mock Repository를 통한 Usecase 통합 테스트");
Console.WriteLine("  - Specification이 Repository에 올바르게 전달되는지 검증");
