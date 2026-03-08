using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace CompositeTypes.ValueObjects;

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

/// <summary>
/// 미국 주 코드 값 객체 (복합 타입에서 재사용)
/// </summary>
public sealed partial class StateCode : SimpleValueObject<string>
{
    private StateCode(string value) : base(value) { }

    public static Fin<StateCode> Create(string value) =>
        CreateFromValidation(Validate(value), v => new StateCode(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<StateCode>.NotEmpty(value)
            .ThenMatches(StatePattern());

    [GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial Regex StatePattern();
}

/// <summary>
/// 우편번호 값 객체 (복합 타입에서 재사용)
/// </summary>
public sealed partial class ZipCode : SimpleValueObject<string>
{
    private ZipCode(string value) : base(value) { }

    public static Fin<ZipCode> Create(string value) =>
        CreateFromValidation(Validate(value), v => new ZipCode(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<ZipCode>.NotEmpty(value)
            .ThenMatches(ZipPattern());

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex ZipPattern();
}
