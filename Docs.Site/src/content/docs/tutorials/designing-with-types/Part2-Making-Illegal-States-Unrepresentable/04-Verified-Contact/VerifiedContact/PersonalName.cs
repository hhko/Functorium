using LanguageExt;

namespace VerifiedContact;

/// <summary>
/// 개인 이름 복합 값 객체
/// </summary>
public sealed record PersonalName
{
    public required String50 FirstName { get; init; }
    public required String50 LastName { get; init; }

    private PersonalName() { }

    public static Fin<PersonalName> Create(string firstName, string lastName)
    {
        var firstResult = String50.Create(firstName);
        var lastResult = String50.Create(lastName);

        if (firstResult.IsFail)
            return firstResult.Match<Fin<PersonalName>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PersonalName>(e));
        if (lastResult.IsFail)
            return lastResult.Match<Fin<PersonalName>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PersonalName>(e));

        return new PersonalName
        {
            FirstName = firstResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            LastName = lastResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
        };
    }

    public override string ToString() => $"{FirstName} {LastName}";
}
