using BistroGo.Core.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Identity;
using BistroGo.Data.Repositories;
using BistroGo.Data.Services;
using BistroGo.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddDbContext<BistroGoDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMenuItemRepository, EfMenuItemRepository>();

builder.Services.AddScoped<ICartService, EfCartService>();
builder.Services.AddScoped<IMenuCategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemRepository, EfOrderItemRepository>();
builder.Services.AddScoped<IPickupTimeSlotRepository, EfPickupTimeSlotRepository>();
builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();

// Tax rate service: singleton so the in-memory rate persists across requests
// Uses file-based persistence to survive application restarts
builder.Services.AddSingleton<ITaxRateService>(provider =>
    new TaxRateService(provider.GetRequiredService<IWebHostEnvironment>()));

// API Clients
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7242/";

const string ApiKeyHeader = "X-Api-Key";   // must match ApiKeyMiddleware.HeaderName in the API
var apiKey = builder.Configuration["ApiSettings:Key"]
    ?? throw new InvalidOperationException("ApiSettings:Key is not configured.");

// Registers a typed HTTP client that always sends the shared key to the API.
void AddApiClient<T>() where T : class =>
    builder.Services.AddHttpClient<T>(c =>
    {
        c.BaseAddress = new Uri(apiBaseUrl);
        c.DefaultRequestHeaders.Add(ApiKeyHeader, apiKey);
    });

AddApiClient<CategoryApiClient>();
AddApiClient<MenuItemApiClient>();
AddApiClient<OrderApiClient>();
AddApiClient<PickupSlotApiClient>();
AddApiClient<ReviewApiClient>();
AddApiClient<ExpenseApiClient>();
AddApiClient<TaxRateApiClient>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<BistroGoDBContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Apply any pending migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BistroGoDBContext>();
    dbContext.Database.Migrate();

    if (!dbContext.PickupTimeSlots.Any())
    {
        dbContext.PickupTimeSlots.Add(new BistroGo.Core.Models.PickupTimeSlot
        {
            Id = 1,
            SlotDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(13, 0),
            MaxOrders = 20
        });
        dbContext.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { RoleNames.Customer, RoleNames.KitchenStaff, RoleNames.Manager })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = "manager@bistrogo.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Manager"
        };
        var result = await userManager.CreateAsync(adminUser, "Manager@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, RoleNames.Manager);
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
