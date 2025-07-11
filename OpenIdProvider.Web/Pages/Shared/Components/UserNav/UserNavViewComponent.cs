using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using OpenIdProvider.Data.Models;

public class UserNavViewModel
{
    public List<Organization> UserOrganizations { get; set; }
    public string UserName { get; set; }
    public string UserAvatarUrl { get; set; }
}

public class UserNavViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new UserNavViewModel();

        model.UserName = User.Identity.IsAuthenticated ? User.Identity.Name : "Guest";

        // Приклад: отримати GivenName замість повного імені
        var givenNameClaim = ((ClaimsPrincipal)User).FindFirst(ClaimTypes.GivenName);
        if (givenNameClaim != null)
        {
            model.UserName = givenNameClaim.Value;
        }
        else if (User.Identity.IsAuthenticated)
        {
            model.UserName = User.Identity.Name ?? "Авторизований користувач";
        }
        else
        {
            model.UserName = "Гість";
        }


        // Get avatar URL (if available) //TODO
        // model.UserAvatarUrl = ((ClaimsPrincipal)User).FindFirst("AvatarUrl")?.Value ?? "https://i.pravatar.cc/32";
        model.UserAvatarUrl = "https://i.pravatar.cc/32";

        // Отримання списку організацій (приклад, у реальності буде з БД)
        model.UserOrganizations = new List<Organization>
        {
            new Organization { Id = new Guid(), Name = "Built-in Organization" },
            new Organization { Id = new Guid(), Name = "Company A" },
            new Organization { Id = new Guid(), Name = "Company B" }
        };

        return View(model);
    }
}