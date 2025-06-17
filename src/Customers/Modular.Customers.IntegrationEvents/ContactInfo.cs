using Modular.Common.User;

namespace Modular.Customers.IntegrationEvents;

public sealed record ContactInfo(string? Email, string? PhoneNumber, PrimaryContactType PrimaryContactType);
