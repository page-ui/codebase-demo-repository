using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.SvelteRender.Controllers;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Services;

namespace Page.Ui.Backend.Tests.SvelteRender;

public sealed class RenderDiagnosticsTests
{
    [Fact]
    public void CompileScript_InjectsDiagnosticsBootstrap()
    {
        var script = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "Page.Ui.SvelteRender",
            "NodeWorker",
            "compile.js"));

        Assert.Contains("buildDiagnosticsScript", script);
        Assert.Contains("window.addEventListener('error'", script);
        Assert.Contains("unhandledrejection", script);
        Assert.Contains("/api/render-diagnostics/report", script);
        Assert.Contains(".diagnostics.js", script);
    }

    [Fact]
    public async Task RenderDiagnosticsController_RelaysReportByPublicRunToken()
    {
        var metadataStore = new Mock<IRenderRunMetadataStore>();
        metadataStore.Setup(x => x.GetByPublicRunTokenAsync("run-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderRun
            {
                RunId = "run-id",
                PublicRunToken = "run-token",
                ChatKey = "chat-key",
                VersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
            });

        HttpRequestMessage? capturedRequest = null;
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient("PageUiDiagnostics"))
            .Returns(new HttpClient(new StubHttpMessageHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }))
            {
                BaseAddress = new Uri("http://page-ui/")
            });

        var controller = new RenderDiagnosticsController(
            metadataStore.Object,
            httpClientFactory.Object,
            Options.Create(new RenderDiagnosticsOptions
            {
                PageUiBaseUrl = "http://page-ui/",
                RelayApiKey = "relay-key",
                ReportPath = "api/internal/render-diagnostics/report"
            }),
            NullLogger<RenderDiagnosticsController>.Instance);

        var result = await controller.Report(
            new RenderDiagnosticsReportRequest
            {
                PublicRunToken = "run-token",
                PagePath = "index",
                Entries = new List<RenderDiagnosticEntry>
                {
                    new() { Severity = "error", Message = "boom", Stack = "stack" },
                    new() { Severity = "warn", Message = "warn" }
                }
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(capturedRequest);
        Assert.EndsWith("api/internal/render-diagnostics/report", capturedRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("relay-key", capturedRequest.Headers.GetValues("X-Render-Diagnostics-Key").Single());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
