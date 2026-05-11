using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Page.Ui.Application.Common.Interfaces;

namespace Page.Ui.Infrastructure.Auth.Services
{
    public class RsaKeyService : IRsaKeyService
    {
        private readonly string? _privateKeyBase64;
        private readonly string? _publicKeyBase64;
        private readonly string _privateKeyPath;
        private readonly string _publicKeyPath;

        public RsaKeyService(IConfiguration configuration)
        {
            var section = configuration.GetSection("Security");
            _privateKeyBase64 = configuration["Security:RsaPrivateKeyBase64"]
                                ?? configuration["SECURITY__RSA_PRIVATE_KEY_BASE64"]
                                ?? Environment.GetEnvironmentVariable("SECURITY__RSA_PRIVATE_KEY_BASE64")
                                ?? Environment.GetEnvironmentVariable("SECURITY_RSA_PRIVATE_KEY_BASE64");
            _publicKeyBase64 = configuration["Security:RsaPublicKeyBase64"]
                               ?? configuration["SECURITY__RSA_PUBLIC_KEY_BASE64"]
                               ?? Environment.GetEnvironmentVariable("SECURITY__RSA_PUBLIC_KEY_BASE64")
                               ?? Environment.GetEnvironmentVariable("SECURITY_RSA_PUBLIC_KEY_BASE64");
            
            _privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), section["RsaPrivateKeyPath"] ?? "secrets/rsa/private.key");
            _publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), section["RsaPublicKeyPath"] ?? "secrets/rsa/public.key");

            if (string.IsNullOrWhiteSpace(_privateKeyBase64) || string.IsNullOrWhiteSpace(_publicKeyBase64))
            {
                var keyDir = Path.GetDirectoryName(_privateKeyPath);
                if (!string.IsNullOrEmpty(keyDir) && !Directory.Exists(keyDir))
                {
                    try
                    {
                        Directory.CreateDirectory(keyDir);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public RSA GetPrivateKey()
        {
            var rsa = RSA.Create();
            
            if (!string.IsNullOrWhiteSpace(_privateKeyBase64))
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(_privateKeyBase64), out _);
                return rsa;
            }

            EnsureKeysExist();
            rsa.ImportRSAPrivateKey(File.ReadAllBytes(_privateKeyPath), out _);
            return rsa;
        }

        public RSA GetPublicKey()
        {
            var rsa = RSA.Create();

            if (!string.IsNullOrWhiteSpace(_publicKeyBase64))
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(_publicKeyBase64), out _);
                return rsa;
            }

            EnsureKeysExist();
            rsa.ImportRSAPublicKey(File.ReadAllBytes(_publicKeyPath), out _);
            return rsa;
        }

        private void EnsureKeysExist()
        {
            if (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath))
            {
                using var rsa = RSA.Create(2048);
                var privateKey = rsa.ExportRSAPrivateKey();
                var publicKey = rsa.ExportRSAPublicKey();

                File.WriteAllBytes(_privateKeyPath, privateKey);
                File.WriteAllBytes(_publicKeyPath, publicKey);
            }
        }
    }
}
