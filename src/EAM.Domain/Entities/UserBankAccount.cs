namespace EAM.Domain.Entities
{
    public class UserBankAccount
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string BankCode { get; private set; } = string.Empty;

        public string BankName { get; private set; } = string.Empty;

        public string EncryptedAccountNumber { get; private set; } = string.Empty;

        public string Last4 { get; private set; } = string.Empty;

        public bool IsPrimary { get; private set; }

        public bool IsVerified { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        private UserBankAccount() { }

        public UserBankAccount(
            Guid userId,
            string bankCode,
            string bankName,
            string encryptedAccountNumber,
            string last4)
        {
            UserId = userId;
            BankCode = bankCode;
            BankName = bankName;
            EncryptedAccountNumber = encryptedAccountNumber;
            Last4 = last4;

            IsPrimary = false;
            IsVerified = false;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPrimary(bool value)
        {
            IsPrimary = value;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Verify()
        {
            IsVerified = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateEncryptedAccount(string encrypted, string last4)
        {
            EncryptedAccountNumber = encrypted;
            Last4 = last4;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}