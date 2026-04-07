using Functorium.Abstractions.Observabilities;

using LanguageExt;

using SourceGenerator.Demo.Adapters;

namespace SourceGenerator.Demo.Contracts;

/// <summary>
/// 사용자 데이터 접근 인터페이스.
/// IObservablePort를 상속하므로 [GenerateObservablePort]이 적용된 구현체에 대해
/// Source Generator가 Observable 래퍼를 생성합니다.
/// </summary>
public interface IUserRepository : IObservablePort
{
    /// <summary>
    /// ID로 사용자를 조회합니다.
    /// </summary>
    FinT<IO, User> GetUserById(int id);

    /// <summary>
    /// 모든 사용자를 조회합니다.
    /// </summary>
    FinT<IO, IReadOnlyList<User>> GetAllUsers();
}
