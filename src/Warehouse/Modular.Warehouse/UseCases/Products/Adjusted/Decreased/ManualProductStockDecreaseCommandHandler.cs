using ErrorOr;
using FluentValidation;
using JasperFx;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Warehouse.Errors;
using Modular.Warehouse.SourceModels;
using Modular.Warehouse.UseCases.Products.Models;

namespace Modular.Warehouse.UseCases.Products.Adjusted.Decreased;

internal sealed class ManualProductStockDecreaseCommandValidator : AbstractValidator<ManualProductStockDecreaseCommand>
{
    public ManualProductStockDecreaseCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0u);
        RuleFor(x => x.Reason).NotEmpty();
    }
}

internal sealed record ManualProductStockDecreaseCommand(string Sku, uint Quantity, string Reason);

internal sealed class ManualProductStockDecreaseCommandHandler
{
    private readonly IDocumentStore _documentStore;
    private readonly IProductStreamStore _productStreamStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ManualProductStockDecreaseCommandHandler> _logger;
    private readonly IValidator<ManualProductStockDecreaseCommand> _validator;

    public ManualProductStockDecreaseCommandHandler(IDocumentStore documentStore, IProductStreamStore productStreamStore, ILogger<ManualProductStockDecreaseCommandHandler> logger,
        TimeProvider dateTimeProvider, IValidator<ManualProductStockDecreaseCommand> validator)
    {
        _documentStore = documentStore;
        _productStreamStore = productStreamStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<ErrorOr<Unit>> Handle(ManualProductStockDecreaseCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

        _logger.LogInformation("Decreasing product {Sku} for quantity {Quantity}. Reason: {Reason}.", request.Sku, request.Quantity, request.Reason);

        await using var session = _documentStore.LightweightSession();
        ErrorOr<ProductWriteContext> writeContextResult = await _productStreamStore.LoadForWritingAsync(session, request.Sku, cancellationToken);
        if (writeContextResult.IsError)
        {
            return writeContextResult.Errors;
        }

        (Product product, IEventStream<Product> stream) = writeContextResult.Value;

        ErrorOr<Unit> decreaseResult = product.DecreaseQuantity(request.Quantity);
        if (decreaseResult.IsError)
        {
            _logger.LogWarning("Product with SKU: {Sku} does not have enough quantity ({CurrentQuantity}) to decrease by {Quantity}.", request.Sku, product.Quantity, request.Quantity);
            return decreaseResult;
        }

        DecreasedProductQuantity productDecreased = new(request.Sku, request.Quantity, request.Reason, _dateTimeProvider.GetUtcNow());
        stream.AppendOne(productDecreased);

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Product with SKU: {Sku} was modified concurrently while decreasing quantity by {Quantity}.", request.Sku, request.Quantity);
            return ProductErrors.ConcurrentModification(request.Sku);
        }

        _logger.LogDebug("Decreasing product {Sku} for quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}