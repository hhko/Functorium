namespace DDDContactExt;

/// <summary>
/// 개인 이름 복합 값 객체 (향상: ValueObject 상속, string? 입력)
/// FirstName, LastName은 필수, MiddleInitial은 선택
/// </summary>
public sealed class PersonalName : ValueObject
{
    public String50 FirstName { get; }
    public String50 LastName { get; }
    public string? MiddleInitial { get; }

    private PersonalName(String50 firstName, String50 lastName, string? middleInitial)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleInitial = middleInitial;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        if (MiddleInitial is not null)
            yield return MiddleInitial;
    }

    public static Fin<PersonalName> Create(
        string? firstName, string? lastName, string? middleInitial = null)
    {
        return from first in String50.Create(firstName)
               from last in String50.Create(lastName)
               select new PersonalName(first, last, middleInitial);
    }

    public static PersonalName CreateFromValidated(
        String50 firstName, String50 lastName, string? middleInitial = null) =>
        new(firstName, lastName, middleInitial);

    public override string ToString() =>
        MiddleInitial is not null
            ? $"{FirstName} {MiddleInitial}. {LastName}"
            : $"{FirstName} {LastName}";
}
