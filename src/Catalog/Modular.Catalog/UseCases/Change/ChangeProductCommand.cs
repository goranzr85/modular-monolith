using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Catalog.Errors;
using Modular.Common;

namespace Modular.Catalog.UseCases.Change;

internal sealed class ChangeProductCommandValidator : AbstractValidator<ChangeProductCommand>
{
    public ChangeProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

internal sealed record ChangeProductCommand(string Sku, string Name, string Description, decimal Price) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class ChangeProductCommandHandler : IRequestHandler<ChangeProductCommand, ErrorOr<Unit>>
{
    private readonly CatalogDbContext _catalogDbContext;

    public ChangeProductCommandHandler(CatalogDbContext catalogDbContext)
    {
        _catalogDbContext = catalogDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(ChangeProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _catalogDbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == request.Sku, cancellationToken);

        if (product is null)
        {
            return ProductErrors.ProductNotFound(request.Sku);
        }

        product.Change(request.Sku, request.Name, request.Description, Price.Create(request.Price));

        _catalogDbContext.Update(product);
        await _catalogDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}