using ErrorOr;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Warehouse.SourceModels;
using Modular.Warehouse.UseCases.Products.Models;

namespace Modular.Warehouse.UseCases.Products.Receiving;

internal sealed class ProductReceivingCommandValidator : AbstractValidator<ProductReceivingCommand>
{
    public ProductReceivingCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0u);
    }
}

internal sealed record ProductReceivingCommand(string Sku, uint Quantity);

internal sealed class ProductReceivingCommandHandler
{
    private readonly IDocumentStore _documentStore;
    private readonly IProductStreamStore _productStreamStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ProductReceivingCommandHandler> _logger;
    private readonly IValidator<ProductReceivingCommand> _validator;

    public ProductReceivingCommandHandler(IDocumentStore documentStore, IProductStreamStore productStreamStore, ILogger<ProductReceivingCommandHandler> logger,
        TimeProvider dateTimeProvider, IValidator<ProductReceivingCommand> validator)
    {
        _documentStore = documentStore;
        _productStreamStore = productStreamStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<ErrorOr<Unit>> Handle(ProductReceivingCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

        _logger.LogInformation("Receiving product {Sku} with quantity {Quantity}.", request.Sku, request.Quantity);

        await using var session = _documentStore.LightweightSession();
        ErrorOr<Product> productResult = await _productStreamStore.LoadAsync(session, request.Sku, cancellationToken);
        if (productResult.IsError)
        {
            return productResult.Errors;
        }

        ProductReceived productReceived = new(request.Sku, request.Quantity, _dateTimeProvider.GetUtcNow());
        session.Events.Append(productReceived.Sku, productReceived);

        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Receiving product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}