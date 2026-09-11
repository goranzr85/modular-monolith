using ErrorOr;
using Modular.Common.Errors;

namespace Modular.Common.User;

public sealed class FullName
{
    public string FirstName { get; private set; }
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; }

    private FullName(string firstName, string? middleName, string lastName)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
    }

    public static ErrorOr<FullName> Create(string firstName, string? middleName, string lastName)
    {
        if (string.IsNullOrEmpty(firstName))
        {
            return FullNameErrors.InvalidFirstName();
        }

        if (string.IsNullOrEmpty(lastName))
        {
            return FullNameErrors.InvalidLastName();
        }

        return new FullName(firstName, middleName, lastName);
    }

}
