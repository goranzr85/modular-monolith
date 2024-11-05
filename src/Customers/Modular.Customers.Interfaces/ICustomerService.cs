namespace Modular.Customers.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse?> GetByIdAsync(Guid customerId);
}

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _dbContext;

    public CustomerService(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid customerId)
    {
        Models.Customer? customer = await _dbContext.Customers.FindAsync(customerId);

        if(customer is null)
        {
            return null;
        }

        return new CustomerResponse(customer.Id, customer.FullName.FirstName, customer.FullName.MiddleName, customer.FullName.LastName,
            customer.Address.Street, customer.Address.City, customer.Address.Zip, customer.Address.State,
            customer.Contact.Email, customer.Contact.Phone);
    }
}
