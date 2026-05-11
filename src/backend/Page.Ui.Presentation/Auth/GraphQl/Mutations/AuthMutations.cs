using HotChocolate;
using HotChocolate.Types;
using Page.Ui.Application.Auth.DTOs;
using Page.Ui.Presentation.Auth.Services;

namespace Page.Ui.Presentation.Auth.GraphQl.Mutations;

[ExtendObjectType("Mutation")]
public sealed class AuthMutations
{
    public Task<bool> Register(
        [Service] AuthMutationWorkflow workflow,
        RegisterInput input)
        => workflow.RegisterAsync(input);

    public Task<LoginResult?> Login(
        [Service] AuthMutationWorkflow workflow,
        LoginInput input)
        => workflow.LoginAsync(input);

    public Task<LoginResult?> RefreshToken(
        [Service] AuthMutationWorkflow workflow,
        string refreshToken)
        => workflow.RefreshTokenAsync(refreshToken);

    public Task<bool> ForgotPasswordRequest(
        [Service] AuthMutationWorkflow workflow,
        string email)
        => workflow.ForgotPasswordRequestAsync(email);

    public Task<string?> VerifyResetCode(
        [Service] AuthMutationWorkflow workflow,
        string email,
        string code)
        => workflow.VerifyResetCodeAsync(email, code);

    public Task<bool> ResetPassword(
        [Service] AuthMutationWorkflow workflow,
        ResetPasswordInput input)
        => workflow.ResetPasswordAsync(input);

    public Task<bool> SignOut(
        [Service] AuthMutationWorkflow workflow,
        string refreshToken)
        => workflow.SignOutAsync(refreshToken);

    [HotChocolate.Authorization.Authorize(Policy = "UserApiPolicy")]
    public Task<bool> RequestAccountDeletion(
        [Service] AuthMutationWorkflow workflow,
        System.Security.Claims.ClaimsPrincipal claimsPrincipal)
        => workflow.RequestAccountDeletionAsync(claimsPrincipal);

    [HotChocolate.Authorization.Authorize(Policy = "UserApiPolicy")]
    public Task<bool> DeleteAccount(
        [Service] AuthMutationWorkflow workflow,
        string code,
        System.Security.Claims.ClaimsPrincipal claimsPrincipal)
        => workflow.DeleteAccountAsync(code, claimsPrincipal);

    public Task<bool> VerifyEmail(
        [Service] AuthMutationWorkflow workflow,
        string email,
        string code)
        => workflow.VerifyEmailAsync(email, code);

    public Task<bool> ResendVerification(
        [Service] AuthMutationWorkflow workflow,
        string email)
        => workflow.ResendVerificationAsync(email);
}
