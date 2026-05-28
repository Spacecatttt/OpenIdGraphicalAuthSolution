using System.Security.Cryptography;
using System.Text;

namespace OpenIdProvider.Blazor.Services;

public class HelperService : IHelperService
{
    public string GeneratePassword(int length = 14)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+<,>.";
        var allChars = upper + lower + digits + special;
        char[] password = new char[length];

        // Ensure the password contains at least one of every required character type
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest of the password length with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        SecureShuffle(password);
        return new string(password);
    }

    public string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return RandomNumberGenerator.GetString(chars, length);
    }

    private static void SecureShuffle(char[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            // Get a secure random index safely bounded within the remaining elements
            int k = RandomNumberGenerator.GetInt32(n + 1);
            // Swap elements
            char value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }
}
