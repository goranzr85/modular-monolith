using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Catalog.Errors;
using Modular.Common;

namespace Modular.Catalog.Change;

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
    private readonly CatalogDbContext _customerDbContext;

    public ChangeProductCommandHandler(CatalogDbContext customerDbContext)
    {
        _customerDbContext = customerDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(ChangeProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _customerDbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == request.Sku, cancellationToken);

        if (product is null)
        {
            return ProductErrors.ProductNotFound(request.Sku);
        }

        product.Change(request.Sku, request.Name, request.Description, Price.Create(request.Price));

        _customerDbContext.Update(product);
        await _customerDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}