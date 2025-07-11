using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenIdProvider.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    [BindProperty]
    public int TotalUsers { get; set; }

    [BindProperty]
    public int NewUsersToday { get; set; }

    [BindProperty]
    public int NewUsersPast7Days { get; set; }

    [BindProperty]
    public int NewUsersPast30Days { get; set; }
    public string ChartDataJson { get; private set; }

    public DashboardModel()
    {
        // Конструктор можна залишити порожнім або використати для ін'єкції залежностей
    }

    public void OnGet()
    {
        // --- ТУТ ВИ БУДЕТЕ ОТРИМУВАТИ РЕАЛЬНІ ДАНІ З БАЗИ ДАНИХ ---

        // Поки що, використаємо ті ж самі статичні дані, що й у HTML
        TotalUsers = 3554;
        NewUsersToday = 0;
        NewUsersPast7Days = 0;
        NewUsersPast30Days = 0;

        // Згенеруємо дані для графіка з кривою лінією в діапазоні від 400 до 2000
        var chartData = new List<int>();
        var random = new Random();
        int value = 400;
        for (int i = 0; i < 30; i++)
        {
            // Додаємо випадковий приріст або спад, щоб лінія була "кривою"
            value += random.Next(-100, 150);
            // Обмежуємо значення в межах 400..2000
            value = Math.Max(400, Math.Min(2000, value));
            chartData.Add(value);
        }

        // Серіалізуємо дані в JSON, щоб легко передати їх у JavaScript
        ChartDataJson = JsonSerializer.Serialize(chartData);
    }
}
