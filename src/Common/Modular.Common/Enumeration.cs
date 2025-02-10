namespace Modular.Common;
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Dictionary<int, TEnum> EnumerationsByValue = CreateEnumerationsByValue();
    private static readonly Dictionary<string, TEnum> EnumerationsByName = CreateEnumerationsByName();

    public int Value { get; protected init; }
    public string Name { get; protected init; }

    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetType().Equals(other.GetType()) && Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is Enumeration<TEnum> other && Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public static TEnum? FromValue(int value)
    {
        return EnumerationsByValue.TryGetValue(value, out var enumValue) ? enumValue : null;
    }

    public static TEnum? FromName(string name)
    {
        return EnumerationsByName.TryGetValue(name, out var enumValue) ? enumValue : null;
    }

    private static Dictionary<int, TEnum> CreateEnumerationsByValue()
    {
        return typeof(TEnum)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToDictionary(e => e.Value);
    }

    private static Dictionary<string, TEnum> CreateEnumerationsByName()
    {
        return typeof(TEnum)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToDictionary(e => e.Name);
    }
}
