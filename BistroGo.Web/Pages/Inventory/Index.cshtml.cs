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

    [BindProperty]
    public EditItemInput EditItem { get; set; } = new();

    [BindProperty]
    public EditCategoryInput EditCategory { get; set; } = new();

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

    public async Task<IActionResult> OnPostEditItemAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ViewData["OpenEditModal"] = true;
            return Page();
        }

        var existingItem = await _menuItemRepo.GetByIdAsync(EditItem.Id);
        if (existingItem == null)
        {
            TempData["Error"] = "Item not found.";
            return RedirectToPage();
        }

        // Handle category (get or create)
        var cat = await _categoryRepo.GetByNameAsync(EditItem.Category.Trim());
        if (cat == null)
        {
            cat = await _categoryRepo.AddAsync(new MenuCategory { Name = EditItem.Category.Trim() });
        }

        // Update fields
        existingItem.Name = EditItem.Name.Trim();
        existingItem.Description = EditItem.Description.Trim();
        existingItem.Price = EditItem.Price;
        existingItem.CategoryId = cat.Id;
        existingItem.ImageUrl = string.IsNullOrWhiteSpace(EditItem.ImageUrl) ? null : EditItem.ImageUrl.Trim();

        await _menuItemRepo.UpdateAsync(existingItem);

        TempData["Message"] = $"Updated \"{existingItem.Name}\".";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditCategoryAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ViewData["OpenEditCategoryModal"] = true;
            return Page();
        }

        var existingCategory = await _categoryRepo.GetByIdAsync(EditCategory.Id);
        if (existingCategory == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToPage();
        }

        // Check if the new name is already taken by a DIFFERENT category
        var duplicate = await _categoryRepo.GetByNameAsync(EditCategory.Name.Trim());
        if (duplicate != null && duplicate.Id != existingCategory.Id)
        {
            TempData["Error"] = $"A category named \"{EditCategory.Name}\" already exists.";
            await LoadDataAsync();
            ViewData["OpenEditCategoryModal"] = true;
            return Page();
        }

        existingCategory.Name = EditCategory.Name.Trim();
        await _categoryRepo.UpdateAsync(existingCategory);

        TempData["Message"] = $"Renamed category to \"{existingCategory.Name}\".";
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
