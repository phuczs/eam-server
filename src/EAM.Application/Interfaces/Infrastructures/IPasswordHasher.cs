namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Salted password hashing — never stores plaintext.</summary>
public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
    string GenerateStrongPassword(int length = 14);
}
