namespace DesigningWithTypes.Contacts.ValueObjects;

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

    public static Validation<Error, (string address1, string city, string state, string zip)> Validate(
        string? address1, string? city, string? state, string? zip) =>
        (String50.Validate(address1), String50.Validate(city), StateCode.Validate(state), ZipCode.Validate(zip))
            .Apply((addr, c, s, z) => (addr, c, s, z));

    public static Fin<PostalAddress> Create(
        string? address1, string? city, string? state, string? zip) =>
        CreateFromValidation<PostalAddress, (string address1, string city, string state, string zip)>(
            Validate(address1, city, state, zip),
            v => new PostalAddress(
                String50.CreateFromValidated(v.address1),
                String50.CreateFromValidated(v.city),
                StateCode.CreateFromValidated(v.state),
                ZipCode.CreateFromValidated(v.zip)));

    public static PostalAddress CreateFromValidated(
        String50 address1, String50 city, StateCode state, ZipCode zip) =>
        new(address1, city, state, zip);

    public override string ToString() =>
        $"{Address1}, {City}, {State} {Zip}";
}
