using System;

namespace EAM.Application.Helpers;

public static class StringExtensions
{    public static string? MaskNric(this string? nric)
    {
        if (string.IsNullOrWhiteSpace(nric))
            return nric;

        nric = nric.Trim();

        if (nric.Length <= 4)
            return new string('*', nric.Length);

        // Take 4 last
        string lastFour = nric.Substring(nric.Length - 4);
        string mask = new string('*', nric.Length - 4);

        return mask + lastFour;
    }
    //mask for bank account
    public static string? MaskBankAccount(this string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return accountNumber;

        var trimmed = accountNumber.Trim();
        if (trimmed.Length <= 4) return new string('*', trimmed.Length);

        string lastFour = trimmed.Substring(trimmed.Length - 4);
        string mask = new string('*', trimmed.Length - 4);

        return mask + lastFour;
    }
}