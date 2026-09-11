using ErrorOr;
using Modular.Common.Errors;

namespace Modular.Common;
public record Price(decimal Value)
{
    public static ErrorOr<Price> Create(decimal value)
    {
        if (value < 0)
            return PriceErrors.InvalidValue();

        return new Price(value);
    }

    public static Price FromPersistedValue(decimal value) => new(value);

    public static implicit operator decimal(Price price) => price.Value;
}
