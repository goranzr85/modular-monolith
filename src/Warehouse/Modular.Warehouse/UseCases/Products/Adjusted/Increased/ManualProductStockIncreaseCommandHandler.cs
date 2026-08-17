using ErrorOr;
using FluentValidation;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
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

internal sealed record ManualProductStockIncreaseCommand(string Sku, uint Quantity, string Reason) : IRequest<ErrorOr<Unit>>;

internal sealed class ManualProductStockIncreaseCommandHandler : IRequestHandler<ManualProductStockIncreaseCommand, ErrorOr<Unit>>
{
    private readonly IDocumentStore _documentStore;
    private readonly IProductStreamStore _productStreamStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ManualProductStockIncreaseCommandHandler> _logger;

    public ManualProductStockIncreaseCommandHandler(IDocumentStore documentStore, IProductStreamStore productStreamStore, ILogger<ManualProductStockIncreaseCommandHandler> logger, TimeProvider dateTimeProvider)
    {
        _documentStore = documentStore;
        _productStreamStore = productStreamStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<Unit>> Handle(ManualProductStockIncreaseCommand request, CancellationToken cancellationToken)
    {
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