using ErrorOr;

namespace Modular.Common.Errors;

public static class FullNameErrors
{
    public static Error InvalidFirstName() =>
        Error.Validation("FullName.InvalidFirstName", "FirstName is not valid.");

    public static Error InvalidLastName() =>
        Error.Validation("FullName.InvalidLastName", "LastName is not valid.");
}
