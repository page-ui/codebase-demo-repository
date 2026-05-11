using System.Security.Cryptography;
using Page.Ui.Application.Common.Interfaces;

namespace Page.Ui.Backend.Tests.TestSupport;

internal sealed class TestRsaKeyService : IRsaKeyService, IDisposable
{
    private readonly RSA _private = RSA.Create(2048);
    private readonly RSA _public = RSA.Create();

    public TestRsaKeyService()
    {
        _public.ImportRSAPublicKey(_private.ExportRSAPublicKey(), out _);
    }

    public RSA GetPrivateKey() => _private;
    public RSA GetPublicKey() => _public;

    public void Dispose()
    {
        _private.Dispose();
        _public.Dispose();
    }
}
