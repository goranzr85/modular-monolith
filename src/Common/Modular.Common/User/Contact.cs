namespace Modular.Common.User;

public sealed class Contact : IEquatable<Contact>
{
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public PrimaryContactType PrimaryContactType { get; private set; }

    public Contact(string? email, string? phone, PrimaryContactType primaryContactType)
    {
        Email = email;
        Phone = phone;
        PrimaryContactType = primaryContactType;
    }

    public bool Equals(Contact? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Email == other.Email && Phone == other.Phone && PrimaryContactType == other.PrimaryContactType;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Contact);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Email, Phone, PrimaryContactType);
    }

    public static bool operator ==(Contact? left, Contact? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Contact? left, Contact? right)
    {
        return !Equals(left, right);
    }

}

public enum PrimaryContactType
{
    Email,
    Phone
}



