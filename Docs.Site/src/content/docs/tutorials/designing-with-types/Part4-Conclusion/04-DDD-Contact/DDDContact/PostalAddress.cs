namespace DDDContact;

/// <summary>
/// 우편 주소 복합 값 객체
/// </summary>
public sealed record PostalAddress
{
    public required String50 Address1 { get; init; }
    public required String50 City { get; init; }
    public required StateCode State { get; init; }
    public required ZipCode Zip { get; init; }

    private PostalAddress() { }

    public static Fin<PostalAddress> Create(string address1, string city, string state, string zip)
    {
        return from addr in String50.Create(address1)
               from c in String50.Create(city)
               from s in StateCode.Create(state)
               from z in ZipCode.Create(zip)
               select new PostalAddress { Address1 = addr, City = c, State = s, Zip = z };
    }

    public static PostalAddress CreateFromValidated(
        String50 address1, String50 city, StateCode state, ZipCode zip) =>
        new() { Address1 = address1, City = city, State = state, Zip = zip };

    public override string ToString() =>
        $"{Address1}, {City}, {State} {Zip}";
}
