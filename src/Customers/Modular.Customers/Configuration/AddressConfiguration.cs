namespace Modular.Customers.Configuration;

internal static class AddressConfiguration
{
    internal static byte StreetMaxLength => 100;
    internal static byte CityMaxLength => 50;
    internal static byte StateMaxLength => 50;
    internal static byte ZipMaxLength => 10;
}
