using FluentValidation;
using Modular.Customers.Configuration;

namespace Modular.Customers.Create;

internal sealed class ChangeCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public ChangeCustomerCommandValidator()
    {
        RuleFor(o => o.FirstName).NotEmpty()
            .Length(FullNameConfiguration.FirstNameLength);

        RuleFor(o => o.LastName).NotEmpty()
            .Length(FullNameConfiguration.LastNameLength);

        RuleFor(o => o.MiddleName)
            .Length(FullNameConfiguration.MiddleNameLength)
            .When(o => !string.IsNullOrEmpty(o.MiddleName));

        RuleFor(o => o.Address).NotNull().ChildRules(address =>
        {
            address.RuleFor(a => a.Street).NotEmpty()
                .Length(AddressConfiguration.StreetMaxLength);

            address.RuleFor(a => a.City).NotEmpty()
                .Length(AddressConfiguration.CityMaxLength);

            address.RuleFor(a => a.Zip).NotEmpty()
                .Length(AddressConfiguration.ZipMaxLength);

            address.RuleFor(a => a.State).NotEmpty()
                .Length(AddressConfiguration.StateMaxLength);
        });

        RuleFor(o => o.ShippingAddress).ChildRules(address =>
        {
            address.RuleFor(a => a.Street)
                .Length(AddressConfiguration.StreetMaxLength);

            address.RuleFor(a => a.City)
                .Length(AddressConfiguration.CityMaxLength);

            address.RuleFor(a => a.Zip)
                .Length(AddressConfiguration.ZipMaxLength);

            address.RuleFor(a => a.State)
                .Length(AddressConfiguration.StateMaxLength);
        })
            .When(o => o.ShippingAddress is not null);

        RuleFor(o => o.Email).EmailAddress()
            .Length(ContactConfiguration.EmailMaxLength)
            .When(o => o.Email is not null);

        RuleFor(o => o.Phone)
            .Length(ContactConfiguration.PhoneMaxLength)
            .When(o => o.Email is not null);
    }
}
