using ParameterValidation.Domains;

namespace ParameterValidation.Extensions;

public static class AddressExtensions
{
    public static string FormatFull(this Address address)
        => $"{address.ZipCode} {address.City} {address.Street}";
}
