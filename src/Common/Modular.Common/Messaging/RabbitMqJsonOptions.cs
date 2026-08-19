using System.Text.Json;

namespace Modular.Common.Messaging;

public static class RabbitMqJsonOptions
{
    // IncludeFields: several integration event contracts carry ValueTuple members (e.g.
    // OrderShippedIntegrationEvent.Products), and System.Text.Json only serializes fields when this is set.
    public static readonly JsonSerializerOptions Default = new()
    {
        IncludeFields = true
    };
}
