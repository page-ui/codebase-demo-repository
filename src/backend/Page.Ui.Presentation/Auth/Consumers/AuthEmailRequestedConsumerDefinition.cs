using MassTransit;

namespace Page.Ui.Presentation.Auth.Consumers;

public class AuthEmailRequestedConsumerDefinition : ConsumerDefinition<AuthEmailRequestedConsumer>
{
    public AuthEmailRequestedConsumerDefinition()
    {
        EndpointName = "auth-email-dispatch";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AuthEmailRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(
            5,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(10)));
        endpointConfigurator.PrefetchCount = 16;
    }
}
