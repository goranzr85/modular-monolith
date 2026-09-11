using ErrorOr;

namespace Modular.Common.Errors;

public static class PriceErrors
{
    public static Error InvalidValue() =>
        Error.Validation("Price.InvalidValue", "Price cannot be negative.");
}
