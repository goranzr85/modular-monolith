namespace Modular.Common;
public class Price
{
    private decimal _value;

    private Price()
    {
    }

    public static Price Create(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Price cannot be less than or equal to zero.", nameof(value));
        }

        return new Price { _value = value };
    }

    public static implicit operator decimal(Price price) => price._value;

}
