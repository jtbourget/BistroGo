using BistroGo.Core.Interfaces;
using BistroGo.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Services 
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Register repositories (in-memory for now)
builder.Services.AddSingleton<IMenuItemRepository, InMemoryMenuItemRepository>();
builder.Services.AddSingleton<IMenuCategoryRepository, InMemoryMenuCategoryRepository>();

// Controllers (you'll need these for the Api to expose endpoints)
builder.Services.AddControllers();

// App 
var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();