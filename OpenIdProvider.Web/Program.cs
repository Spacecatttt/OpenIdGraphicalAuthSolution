using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Logging configuration
builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(ctx.Configuration));

// Database connection string and migrations assembly
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name;

// 2. Add ASP.NET Core Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly))
                                .UseLazyLoadingProxies());

// 3. Register ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 4. Register Duende IdentityServer and its stores
builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
        options.EmitStaticAudienceClaim = true;
    })
    // Use EF Core for configuration data (Clients, Resources)
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly));
    })
    // Use EF Core for operational data (Grants, Tokens)
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly));
    })
    // Integrate with ASP.NET Core Identity
    .AddAspNetIdentity<ApplicationUser>();


// here RequireConfirmedAccount
//builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();
//
//
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
});



// Add services to the container.
builder.Services.AddRazorPages();

//builder.Services.AddRazorPages(options =>
//{
//    // Захищаємо всі сторінки в папці "Private"
//    // Це означає, що будь-яка Razor Page в /Pages/Private (і її підпапках)
//    // вимагатиме авторизації.
//    options.Conventions.AuthorizeFolder("/Private");
//
//    // Можна також захистити конкретну сторінку
//    // options.Conventions.AuthorizePage("/SecretPage");
//
//    // Або дозволити анонімний доступ до певної сторінки в захищеній папці
//    // options.Conventions.AllowAnonymousToPage("/Private/PublicInfo");
//});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();

app.UseAuthorization();

app.MapRazorPages();

// Initialize IdentityServer
// SeedData.EnsureSeedData(app);

app.Run();
