using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BistroGo.Data.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Web.Pages.Inventory;

[Authorize(Roles = Roles.Manager)]
public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menuItemRepo;
    private readonly IMenuCategoryRepository _categoryRepo;

    public IndexModel(IMenuItemRepository menuItemRepo, IMenuCategoryRepository categoryRepo)
    {
        _menuItemRepo = menuItemRepo;
        _categoryRepo = categoryRepo;
    }

    public IEnumerable<MenuItem> Items { get; set; } = new List<MenuItem>();
    public IEnumerable<MenuCategory> Categories { get; set; } = new List<MenuCategory>();

    [BindProperty]
    public NewItemInput NewItem { get; set; } = new();

    [BindProperty]
    public string? NewCategoryName { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAddItemAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ViewData["OpenModal"] = true;
            return Page();
        }

        var cat = await _categoryRepo.GetByNameAsync(NewItem.Category.Trim());
        if (cat == null)
        {
            cat = await _categoryRepo.AddAsync(new MenuCategory { Name = NewItem.Category.Trim() });
        }

        await _menuItemRepo.AddAsync(new MenuItem
        {
            Name = NewItem.Name.Trim(),
            Description = NewItem.Description.Trim(),
            Price = NewItem.Price,
            CategoryId = cat.Id,
            ImageUrl = string.IsNullOrWhiteSpace(NewItem.ImageUrl) ? null : NewItem.ImageUrl.Trim(),
            IsAvailable = true
        });

        TempData["Message"] = $"Added \"{NewItem.Name}\" to the menu.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddCategoryAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            var trimmedName = NewCategoryName.Trim();
            var existing = await _categoryRepo.GetByNameAsync(trimmedName);

            if (existing != null)
            {
                TempData["Error"] = $"Category \"{trimmedName}\" already exists.";
            }
            else
            {
                var created = await _categoryRepo.AddAsync(new MenuCategory { Name = trimmedName });
                TempData["Message"] = $"Added category \"{created.Name}\".";
            }
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(int id)
    {
        var deleted = await _menuItemRepo.DeleteAsync(id);
        if (deleted)
        {
            TempData["Message"] = "Item removed.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
    {
        var deleted = await _categoryRepo.DeleteAsync(id);
        if (deleted)
        {
            TempData["Message"] = "Category removed.";
        }
        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Categories = (await _categoryRepo.GetAllAsync()).OrderBy(c => c.Name).ToList();
        Items = (await _menuItemRepo.GetAllAsync())
            .OrderBy(i => i.Category?.Name ?? "")
            .ThenBy(i => i.Name)
            .ToList();
    }

    public class NewItemInput
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
    }
}
