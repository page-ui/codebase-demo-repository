using System.Reflection;

namespace Page.Ui.Backend.Tests.Auth;

public class AuthRetroEmailBuilderTests
{
    [Fact]
    public void Build_EmailVerification_DisplaysTenMinuteExpiry()
    {
        var html = BuildRetroEmail("EMAIL_VERIFICATION");

        Assert.Contains("EXPIRES_IN:&nbsp;<span style=\"color:#4ade80;\">10 MINUTES</span>", html);
    }

    private static string BuildRetroEmail(string actionName)
    {
        var builderType = Type.GetType(
            "Page.Ui.Presentation.Auth.GraphQl.Support.AuthRetroEmailBuilder, Page.Ui.Presentation")
            ?? throw new InvalidOperationException("AuthRetroEmailBuilder type was not found.");

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AuthRetroEmailBuilder.Build was not found.");

        return (string)build.Invoke(null, new object[]
        {
            "user@example.com",
            "12345",
            actionName,
            "SYSTEM - REGISTRATION LOG"
        })!;
    }
}
