using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Modular.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        List<Error> errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToList();

        if (errors.Count > 0 && typeof(TResponse).IsAssignableTo(typeof(IErrorOr)))
        {
            return (dynamic)errors;
        }

        return await next();
    }
}
