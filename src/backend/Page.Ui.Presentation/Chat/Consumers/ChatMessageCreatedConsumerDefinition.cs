using MassTransit;

namespace Page.Ui.Presentation.Chat.Consumers;

public class ChatMessageCreatedConsumerDefinition : ConsumerDefinition<ChatMessageCreatedConsumer>
{
    public ChatMessageCreatedConsumerDefinition()
    {
        EndpointName = "auth-service-notifications";

        ConcurrentMessageLimit = 20;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ChatMessageCreatedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        endpointConfigurator.PrefetchCount = 32;
    }
}
