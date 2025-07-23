using System.Text;

namespace OpenIdProvider.Blazor.Services;

public interface IHelperService
{
    string GeneratePassword(int length);
    string GenerateRandomString(int length);
}
