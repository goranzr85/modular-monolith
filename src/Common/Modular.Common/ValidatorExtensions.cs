using ErrorOr;
using FluentValidation;
using FluentValidation.Results;

namespace Modular.Common;

public static class ValidatorExtensions
{
    public static async Task<List<Error>> GetValidationErrorsAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        ValidationResult result = await validator.ValidateAsync(instance, cancellationToken);

        return result.Errors
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToList();
    }
}
