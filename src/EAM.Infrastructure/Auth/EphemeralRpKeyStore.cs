using System;
using System.Security.Cryptography;

namespace EAM.Infrastructure.Auth
{
    public sealed class EphemeralRpKeyStore : IRpKeyStore, IDisposable
    {
        public ECDsa SigningKey { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public ECDiffieHellman EncryptionKey { get; } = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        public string SigningKeyId { get; } = "fapi-rp-sig-key-01";
        public string EncryptionKeyId { get; } = "fapi-rp-enc-key-01";

        public void Dispose()
        {
            SigningKey.Dispose();
            EncryptionKey.Dispose();
        }
    }
}
