namespace DDDContact;

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
        return from first in String50.Create(firstName)
               from last in String50.Create(lastName)
               select new PersonalName { FirstName = first, LastName = last, MiddleInitial = middleInitial };
    }

    public static PersonalName CreateFromValidated(
        String50 firstName, String50 lastName, string? middleInitial = null) =>
        new() { FirstName = firstName, LastName = lastName, MiddleInitial = middleInitial };

    public override string ToString() =>
        MiddleInitial is not null
            ? $"{FirstName} {MiddleInitial}. {LastName}"
            : $"{FirstName} {LastName}";
}
