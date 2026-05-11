using MassTransit;

namespace Page.Ui.Worker.Ai.Consumers;

public class ChatMessageCreatedConsumerDefinition : ConsumerDefinition<ChatMessageCreatedConsumer>
{
    public ChatMessageCreatedConsumerDefinition()
    {
        EndpointName = "ai-service-queue";
        ConcurrentMessageLimit = 5;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ChatMessageCreatedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10)));
        endpointConfigurator.PrefetchCount = 10;
    }
}
