using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Catalog.Errors;
using Modular.Common;

namespace Modular.Catalog.Create;

internal sealed record CreateProductCommand(string Sku, string Name, string Description, decimal Price) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ErrorOr<Unit>>
{
    private readonly CatalogDbContext _customerDbContext;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(CatalogDbContext customerDbContext, ILogger<CreateProductCommandHandler> logger)
    {
        _customerDbContext = customerDbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Product? product = await _customerDbContext.Products
                .FirstOrDefaultAsync(p => p.Sku == request.Sku, cancellationToken);

            if (product is not null)
            {
                return ProductErrors.ProductAlreadyExists(request.Sku);
            }

            ErrorOr<Product> productResult = Product.Create(request.Sku, request.Name, request.Description, Price.Create(request.Price));

            if (productResult.IsError)
            {
                return productResult.FirstError;
            }

            product = productResult.Value;

            await _customerDbContext.Products.AddAsync(product, cancellationToken);
            await _customerDbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating product {@Product} failed.", request);
            return ProductErrors.ProductNotCreated(request.Sku);
        }
    }
}