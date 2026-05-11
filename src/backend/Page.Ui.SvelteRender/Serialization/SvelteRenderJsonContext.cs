using System.Text.Json.Serialization;
using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(SortedDictionary<string, string>))]
[JsonSerializable(typeof(RenderResponse))]
[JsonSerializable(typeof(RenderRequest))]
[JsonSerializable(typeof(RenderObjectRequest))]
[JsonSerializable(typeof(RenderObjectPage))]
[JsonSerializable(typeof(RenderSourceFile))]
[JsonSerializable(typeof(List<RenderSourceFile>))]
[JsonSerializable(typeof(RenderDiagnosticsReportRequest))]
[JsonSerializable(typeof(RenderDiagnosticEntry))]
[JsonSerializable(typeof(SandboxRenderPayload))]
[JsonSerializable(typeof(RenderPage))]
[JsonSerializable(typeof(List<RenderPage>))]
internal partial class SvelteRenderJsonContext : JsonSerializerContext
{
}
