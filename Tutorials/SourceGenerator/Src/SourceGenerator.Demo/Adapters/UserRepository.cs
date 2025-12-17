using Functorium.Adapters.SourceGenerator;

using LanguageExt;

using SourceGenerator.Demo.Contracts;

namespace SourceGenerator.Demo.Adapters;

/// <summary>
/// 사용자 Repository 구현체.
///
/// [GeneratePipeline] 어트리뷰트가 적용되어 있으므로
/// Source Generator가 자동으로 UserRepositoryPipeline 클래스를 생성합니다.
///
/// 생성된 Pipeline 클래스는:
/// - 이 클래스를 상속
/// - 각 메서드를 override하여 Observability(로깅, 트레이싱, 메트릭) 추가
/// - ActivityContext 전파 처리
/// </summary>
[GeneratePipeline]
public class UserRepository : IUserRepository
{
    public string RequestCategory => "repository";

    // public virtual FinT<IO, User> GetUserById(int id) =>
    //     FinT.lift<IO, User>(IO.pure(Fin.Succ(new User(id, $"User{id}", $"user{id}@example.com"))));

    // public virtual FinT<IO, IReadOnlyList<User>> GetAllUsers() =>
    //     FinT.lift<IO, IReadOnlyList<User>>(IO.pure(Fin.Succ<IReadOnlyList<User>>(new List<User>
    //     {
    //         new(1, "Alice", "alice@example.com"),
    //         new(2, "Bob", "bob@example.com")
    //     })));

    public virtual FinT<IO, User> GetUserById(int id) =>
        IO.lift<User>(new User(id, $"User{id}", $"user{id}@example.com"));

    public virtual FinT<IO, IReadOnlyList<User>> GetAllUsers() =>
        IO.lift<IReadOnlyList<User>>(new List<User>
        {
            new(1, "Alice", "alice@example.com"),
            new(2, "Bob", "bob@example.com")
        });
}
