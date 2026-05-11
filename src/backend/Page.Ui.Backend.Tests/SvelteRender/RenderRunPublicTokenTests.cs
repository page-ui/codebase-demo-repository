using Page.Ui.SvelteRender.Services;

namespace Page.Ui.Backend.Tests.SvelteRender;

public class RenderRunPublicTokenTests
{
    [Fact]
    public void FromRunContext_BindsTokenToUserChatVersionAndRun()
    {
        const string runId = "11111111-1111-1111-1111-111111111111";
        var firstMetadata = new Dictionary<string, string>
        {
            ["userStorageKey"] = "user-a",
            ["chatKey"] = "chat-a",
            ["versionId"] = "22222222-2222-2222-2222-222222222222"
        };
        var secondMetadata = new Dictionary<string, string>
        {
            ["userStorageKey"] = "user-b",
            ["chatKey"] = "chat-a",
            ["versionId"] = "22222222-2222-2222-2222-222222222222"
        };

        var firstToken = RenderRunPublicToken.FromRunContext(runId, firstMetadata);
        var repeatedFirstToken = RenderRunPublicToken.FromRunContext(runId, firstMetadata);
        var secondToken = RenderRunPublicToken.FromRunContext(runId, secondMetadata);

        Assert.Equal(firstToken, repeatedFirstToken);
        Assert.NotEqual(firstToken, secondToken);
        Assert.StartsWith("run_pub_", firstToken);
    }

    [Fact]
    public void FromRunContext_FallsBackToRunIdTokenWhenContextMetadataIsMissing()
    {
        const string runId = "11111111-1111-1111-1111-111111111111";

        var token = RenderRunPublicToken.FromRunContext(runId, new Dictionary<string, string>());

        Assert.Equal(RenderRunPublicToken.FromRunId(runId), token);
    }
}
