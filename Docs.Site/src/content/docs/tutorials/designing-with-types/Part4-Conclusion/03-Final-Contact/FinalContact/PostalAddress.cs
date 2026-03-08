using LanguageExt;

namespace FinalContact;

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
        var address1Result = String50.Create(address1);
        var cityResult = String50.Create(city);
        var stateResult = StateCode.Create(state);
        var zipResult = ZipCode.Create(zip);

        if (address1Result.IsFail)
            return address1Result.Match<Fin<PostalAddress>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PostalAddress>(e));
        if (cityResult.IsFail)
            return cityResult.Match<Fin<PostalAddress>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PostalAddress>(e));
        if (stateResult.IsFail)
            return stateResult.Match<Fin<PostalAddress>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PostalAddress>(e));
        if (zipResult.IsFail)
            return zipResult.Match<Fin<PostalAddress>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: e => Fin.Fail<PostalAddress>(e));

        return new PostalAddress
        {
            Address1 = address1Result.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            City = cityResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            State = stateResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
            Zip = zipResult.Match(Succ: v => v, Fail: _ => throw new InvalidOperationException()),
        };
    }

    public override string ToString() =>
        $"{Address1}, {City}, {State} {Zip}";
}
