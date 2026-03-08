namespace NaiveContact;

/// <summary>
/// 나이브한 Contact — 모든 필드가 string
/// 타입 혼동 버그가 컴파일 타임에 감지되지 않음을 시연
/// </summary>
public class Contact
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";

    /// <summary>
    /// firstName과 lastName을 받아 Contact를 생성하는 팩토리 메서드
    /// 매개변수 순서를 실수해도 컴파일러가 잡아주지 않음
    /// </summary>
    public static Contact Create(string firstName, string lastName, string email) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = email
        };
}
