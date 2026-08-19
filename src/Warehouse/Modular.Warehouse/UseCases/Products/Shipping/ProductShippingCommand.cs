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

namespace Modular.Warehouse.UseCases.Products.Shipping;

internal sealed class ProductShippingCommandValidator : AbstractValidator<ProductShippingCommand>
{
    public ProductShippingCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0u);
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

internal sealed record ProductShippingCommand(string Sku, uint Quantity, Guid OrderId);

internal sealed class ProductShippingCommandHandler
{
    private readonly IDocumentStore _documentStore;
    private readonly IProductStreamStore _productStreamStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ProductShippingCommandHandler> _logger;
    private readonly IValidator<ProductShippingCommand> _validator;

    public ProductShippingCommandHandler(IDocumentStore documentStore,
        IProductStreamStore productStreamStore,
        ILogger<ProductShippingCommandHandler> logger,
        TimeProvider dateTimeProvider,
        IValidator<ProductShippingCommand> validator)
    {
        _documentStore = documentStore;
        _productStreamStore = productStreamStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<ErrorOr<Unit>> Handle(ProductShippingCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

        _logger.LogInformation("Shipping product {Sku} with quantity {Quantity}.", request.Sku, request.Quantity);

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
            _logger.LogWarning("Product with SKU: {Sku} does not have enough quantity ({CurrentQuantity}) to ship {Quantity}.", request.Sku, product.Quantity, request.Quantity);
            return decreaseResult;
        }

        DateTimeOffset occuredOnUtc = _dateTimeProvider.GetUtcNow();
        ProductShipped productShipped = new(request.Sku, request.Quantity, occuredOnUtc);
        stream.AppendOne(productShipped);

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Product with SKU: {Sku} was modified concurrently while shipping quantity {Quantity}.", request.Sku, request.Quantity);
            return ProductErrors.ConcurrentModification(request.Sku);
        }

        _logger.LogDebug("Shipping product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}