namespace Modular.Common;
public record Price(decimal Value)
{
    public static Price Create(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(value));

        return new Price(value);
    }

    public static implicit operator decimal(Price price) => price.Value;
}

