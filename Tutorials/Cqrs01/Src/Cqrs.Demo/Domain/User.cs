namespace Cqrs10.Demo;

/// <summary>
/// 사용자 도메인 모델
/// </summary>
public sealed record class User(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt);
