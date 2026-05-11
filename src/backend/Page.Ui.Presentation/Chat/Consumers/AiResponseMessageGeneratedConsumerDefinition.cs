using MassTransit;

namespace Page.Ui.Presentation.Chat.Consumers;

public class AiResponseMessageGeneratedConsumerDefinition : ConsumerDefinition<AiResponseMessageGeneratedConsumer>
{
    public AiResponseMessageGeneratedConsumerDefinition()
    {
        EndpointName = "chat-ai-responses";
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<AiResponseMessageGeneratedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
        endpointConfigurator.PrefetchCount = 16;
    }
}
