using System.Text;

namespace Modular.Common.Messaging;

public static class RabbitMqIntegrationEventNaming
{
    // One fanout exchange per message CLR type - mirrors MassTransit's default per-message-type topology.
    public static string ExchangeFor(Type messageType) => messageType.FullName!;

    // Kebab-case of the consumer class name - mirrors MassTransit's SetKebabCaseEndpointNameFormatter default.
    public static string QueueFor(Type consumerType) => ToKebabCase(consumerType.Name);

    private static string ToKebabCase(string value)
    {
        StringBuilder builder = new();

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
