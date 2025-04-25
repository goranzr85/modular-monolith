//using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
//using Modular.Orders.UseCases.Orders.Models;

//public class ProductShippedStatusConverter : ValueConverter<ProductShippedStatus, string>
//{
//    public ProductShippedStatusConverter() : base(
//        status => ConvertToString(status),
//        value => ConvertFromString(value))
//    {
//    }

//    private static string ConvertToString(ProductShippedStatus status)
//    {
//        // Format: "true|2024-04-25T10:00:00Z"
//        return $"{status.IsShipped}|{status.Date?.ToString("O")}";
//    }

//    private static ProductShippedStatus ConvertFromString(string value)
//    {
//        var parts = value.Split('|');
//        var isShipped = bool.Parse(parts[0]);
//        var hasDate = parts.Length > 1 && DateTimeOffset.TryParse(parts[1], out var date);

//        return isShipped
//            ? new ShippedStatus { Date = hasDate ? date : DateTimeOffset.UtcNow } // fallback
//            : new NotShippedStatus();
//    }
//}
