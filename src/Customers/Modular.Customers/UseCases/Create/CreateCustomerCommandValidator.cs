using FluentValidation;
using Modular.Common.User.Configuration;

namespace Modular.Customers.UseCases.Create;

internal sealed class ChangeCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public ChangeCustomerCommandValidator()
    {
        RuleFor(o => o.FirstName).NotEmpty()
            .MaximumLength(FullNameConfiguration.FirstNameLength);

        RuleFor(o => o.LastName).NotEmpty()
            .MaximumLength(FullNameConfiguration.LastNameLength);

        RuleFor(o => o.MiddleName)
            .MaximumLength(FullNameConfiguration.MiddleNameLength)
            .When(o => !string.IsNullOrEmpty(o.MiddleName));

        RuleFor(o => o.Address).NotNull().ChildRules(address =>
        {
            address.RuleFor(a => a.Street).NotEmpty()
                .MaximumLength(AddressConfiguration.StreetMaxLength);

            address.RuleFor(a => a.City).NotEmpty()
                .MaximumLength(AddressConfiguration.CityMaxLength);

            address.RuleFor(a => a.Zip).NotEmpty()
                .MaximumLength(AddressConfiguration.ZipMaxLength);

            address.RuleFor(a => a.State).NotEmpty()
                .MaximumLength(AddressConfiguration.StateMaxLength);
        });

        RuleFor(o => o.ShippingAddress).ChildRules(address =>
        {
            address.RuleFor(a => a.Street)
                .MaximumLength(AddressConfiguration.StreetMaxLength);

            address.RuleFor(a => a.City)
                .MaximumLength(AddressConfiguration.CityMaxLength);

            address.RuleFor(a => a.Zip)
                .MaximumLength(AddressConfiguration.ZipMaxLength);

            address.RuleFor(a => a.State)
                .MaximumLength(AddressConfiguration.StateMaxLength);
        })
            .When(o => o.ShippingAddress is not null);

        RuleFor(o => o.Email).EmailAddress()
            .MaximumLength(ContactConfiguration.EmailMaxLength)
            .When(o => o.Email is not null);

        RuleFor(o => o.Phone)
            .MaximumLength(ContactConfiguration.PhoneMaxLength)
            .When(o => o.Phone is not null);
    }
}
