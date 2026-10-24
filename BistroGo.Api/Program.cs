using BistroGo.Core.Interfaces;
using BistroGo.Data.DataAccess;
using BistroGo.Data.Repositories;
using BistroGo.Data.Services;
using Microsoft.EntityFrameworkCore;
using BistroGo.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (repository-per-entity pattern)
builder.Services.AddScoped<IMenuCategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<IMenuItemRepository, EfMenuItemRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, EfOrderItemRepository>();
builder.Services.AddScoped<IPickupTimeSlotRepository, EfPickupTimeSlotRepository>();
builder.Services.AddScoped<IReviewRepository, EfReviewRepository>();
builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
builder.Services.AddScoped<ICartService, EfCartService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();   // reject /api calls that don't carry the shared key
app.UseAuthorization();
app.MapControllers();

app.Run();
