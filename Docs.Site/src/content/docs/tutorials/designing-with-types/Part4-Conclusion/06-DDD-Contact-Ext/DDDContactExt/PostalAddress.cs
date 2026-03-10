namespace DDDContactExt;

/// <summary>
/// 우편 주소 복합 값 객체 (향상: ValueObject 상속, string? 입력)
/// </summary>
public sealed class PostalAddress : ValueObject
{
    public String50 Address1 { get; }
    public String50 City { get; }
    public StateCode State { get; }
    public ZipCode Zip { get; }

    private PostalAddress(String50 address1, String50 city, StateCode state, ZipCode zip)
    {
        Address1 = address1;
        City = city;
        State = state;
        Zip = zip;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address1;
        yield return City;
        yield return State;
        yield return Zip;
    }

    public static Fin<PostalAddress> Create(
        string? address1, string? city, string? state, string? zip)
    {
        return from addr in String50.Create(address1)
               from c in String50.Create(city)
               from s in StateCode.Create(state)
               from z in ZipCode.Create(zip)
               select new PostalAddress(addr, c, s, z);
    }

    public static PostalAddress CreateFromValidated(
        String50 address1, String50 city, StateCode state, ZipCode zip) =>
        new(address1, city, state, zip);

    public override string ToString() =>
        $"{Address1}, {City}, {State} {Zip}";
}
