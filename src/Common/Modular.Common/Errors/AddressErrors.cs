using ErrorOr;

namespace Modular.Common.Errors;

public static class AddressErrors
{
    public static Error InvalidStreet() =>
        Error.Validation("Address.InvalidStreet", "Street cannot be null or empty.");

    public static Error InvalidCity() =>
        Error.Validation("Address.InvalidCity", "City cannot be null or empty.");

    public static Error InvalidState() =>
        Error.Validation("Address.InvalidState", "State cannot be null or empty.");

    public static Error InvalidZip() =>
        Error.Validation("Address.InvalidZip", "Zip cannot be null or empty.");
}
