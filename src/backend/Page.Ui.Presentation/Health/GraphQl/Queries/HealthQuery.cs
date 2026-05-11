using HotChocolate.Types;

namespace Page.Ui.Presentation.Health.GraphQl.Queries;

[ExtendObjectType("Query")]
public sealed class HealthQuery
{
    public string Health() => "Nice";
}
