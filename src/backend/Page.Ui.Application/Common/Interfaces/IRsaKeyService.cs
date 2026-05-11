using System.Security.Cryptography;

namespace Page.Ui.Application.Common.Interfaces
{
    public interface IRsaKeyService
    {
        RSA GetPrivateKey();
        RSA GetPublicKey();
    }
}
