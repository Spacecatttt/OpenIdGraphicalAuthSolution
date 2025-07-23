using System.Text;

namespace OpenIdProvider.Blazor.Services;

public class HelperService : IHelperService
{
    private static readonly Random _random = new Random();


    public string GeneratePassword(int length = 14)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+<,>.";
        var allChars = upper + lower + digits + special;
        var password = new StringBuilder();

        // Ensure the password contains at least one of every required character type
        password.Append(upper[_random.Next(upper.Length)]);
        password.Append(lower[_random.Next(lower.Length)]);
        password.Append(digits[_random.Next(digits.Length)]);
        password.Append(special[_random.Next(special.Length)]);

        // Fill the rest of the password length with random characters
        for (int i = 4; i < length; i++)
        {
            password.Append(allChars[_random.Next(allChars.Length)]);
        }

        return new string(password.ToString().ToCharArray().OrderBy(c => _random.Next()).ToArray());
    }

    public string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
