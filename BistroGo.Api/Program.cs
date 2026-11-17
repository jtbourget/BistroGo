using BistroGo.Api.Errors;
using BistroGo.Api.Middleware;
using BistroGo.Api.Security;
using BistroGo.Core.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Scheduling;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Identity;
using BistroGo.Data.Repositories;
using BistroGo.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Configure console log formatting based on environment (human-readable in Dev, structured JSON in Production)
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    });
}

// Add services to the container (repository-per-entity pattern)
builder.Services.AddScoped<IMenuCategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<IMenuItemRepository, EfMenuItemRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, EfOrderItemRepository>();
builder.Services.AddScoped<IPickupTimeSlotRepository, EfPickupTimeSlotRepository>();
builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
builder.Services.AddScoped<ICartService, EfCartService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPickupSlotService, PickupSlotService>();

// Tax rate service: singleton so the in-memory rate persists across requests
// Uses file-based persistence to survive application restarts
builder.Services.AddSingleton<ITaxRateService>(provider =>
    new TaxRateService(
        provider.GetRequiredService<IWebHostEnvironment>(),
        provider.GetRequiredService<ILogger<TaxRateService>>()));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BistroGoDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=../BistroGo.Web/bistrogo.db"));

// Add Identity and cookie authentication so the API can validate roles from the Web app
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

builder.Services.Configure<PickupSlotOptions>(builder.Configuration.GetSection(PickupSlotOptions.SectionName));

var app = builder.Build();

// Correlation ID middleware runs first so all log messages, scopes, and exception handlers carry the ID
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();   // must wrap everything below it
app.UseStatusCodePages();    // turns empty 404/401/etc. responses into ProblemDetails too

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();   // reject /api calls that don't carry the shared key
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
