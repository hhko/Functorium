using LanguageExt;
using LanguageExt.Common;

namespace CompositeTypes.ValueObjects;

/// <summary>
/// 개인 이름 복합 값 객체
/// FirstName, LastName은 필수, MiddleInitial은 선택
/// </summary>
public sealed record PersonalName
{
    public required String50 FirstName { get; init; }
    public required String50 LastName { get; init; }
    public string? MiddleInitial { get; init; }

    private PersonalName() { }

    public static Fin<PersonalName> Create(string firstName, string lastName, string? middleInitial = null)
    {
        var firstNameResult = String50.Create(firstName);
        var lastNameResult = String50.Create(lastName);

        if (firstNameResult.IsFail)
            return firstNameResult.Match<Fin<PersonalName>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PersonalName>(e));

        if (lastNameResult.IsFail)
            return lastNameResult.Match<Fin<PersonalName>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PersonalName>(e));

        return new PersonalName
        {
            FirstName = firstNameResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            LastName = lastNameResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            MiddleInitial = middleInitial
        };
    }

    public override string ToString() =>
        MiddleInitial is not null
            ? $"{FirstName} {MiddleInitial}. {LastName}"
            : $"{FirstName} {LastName}";
}
