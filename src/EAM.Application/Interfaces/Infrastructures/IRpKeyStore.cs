using System.Security.Cryptography;

namespace EAM.Infrastructure.Auth
{
    public interface IRpKeyStore
    {
        ECDsa SigningKey { get; }
        ECDiffieHellman EncryptionKey { get; }
        string SigningKeyId { get; }
        string EncryptionKeyId { get; }
    }
}
