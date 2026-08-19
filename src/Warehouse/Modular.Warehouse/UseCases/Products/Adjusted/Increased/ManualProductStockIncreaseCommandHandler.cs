using ErrorOr;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Warehouse.SourceModels;
using Modular.Warehouse.UseCases.Products.Models;

namespace Modular.Warehouse.UseCases.Products.Adjusted.Increased;

internal sealed class ManualProductStockIncreaseCommandValidator : AbstractValidator<ManualProductStockIncreaseCommand>
{
    public ManualProductStockIncreaseCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0u);
        RuleFor(x => x.Reason).NotEmpty();
    }
}

internal sealed record ManualProductStockIncreaseCommand(string Sku, uint Quantity, string Reason);

internal sealed class ManualProductStockIncreaseCommandHandler
{
    private readonly IDocumentStore _documentStore;
    private readonly IProductStreamStore _productStreamStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ManualProductStockIncreaseCommandHandler> _logger;
    private readonly IValidator<ManualProductStockIncreaseCommand> _validator;

    public ManualProductStockIncreaseCommandHandler(IDocumentStore documentStore, IProductStreamStore productStreamStore, ILogger<ManualProductStockIncreaseCommandHandler> logger,
        TimeProvider dateTimeProvider, IValidator<ManualProductStockIncreaseCommand> validator)
    {
        _documentStore = documentStore;
        _productStreamStore = productStreamStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<ErrorOr<Unit>> Handle(ManualProductStockIncreaseCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

        _logger.LogInformation("Increasing product {Sku} with quantity {Quantity}. Reason: {Reason}.", request.Sku, request.Quantity, request.Reason);

        await using var session = _documentStore.LightweightSession();
        ErrorOr<Product> productResult = await _productStreamStore.LoadAsync(session, request.Sku, cancellationToken);
        if (productResult.IsError)
        {
            return productResult.Errors;
        }

        IncreasedProductQuantity productIncreased = new(request.Sku, request.Quantity, request.Reason, _dateTimeProvider.GetUtcNow());
        session.Events.Append(productIncreased.Sku, productIncreased);

        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Increasing product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}