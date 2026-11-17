using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BistroGo.Core.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Pages.Inventory;

// Manager-only page for managing menu items and categories (add, edit, delete).
// Uses a mix of [BindProperty] inputs and named POST handlers for each action.
[Authorize(Roles = RoleNames.Manager)]
public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menuItemRepo;
    private readonly IMenuCategoryRepository _categoryRepo;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IMenuItemRepository menuItemRepo,
        IMenuCategoryRepository categoryRepo,
        ILogger<IndexModel>? logger = null)
    {
        _menuItemRepo = menuItemRepo;
        _categoryRepo = categoryRepo;
        _logger = logger ?? NullLogger<IndexModel>.Instance;
    }

    // Data rendered on the page: all items (grouped by category) and all categories.
    public IEnumerable<MenuItem> Items { get; set; } = new List<MenuItem>();
    public IEnumerable<MenuCategory> Categories { get; set; } = new List<MenuCategory>();

    // Bound inputs for the add-item form, add-category field, and the two edit modals.
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
        _logger.LogInformation("Inventory page loaded by user {UserName}", User.Identity?.Name);
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inventory page data");
            throw;
        }
    }

    // Adds a new menu item; reopens the modal with errors if the form is invalid.
    public async Task<IActionResult> OnPostAddItemAsync()
    {
        _logger.LogInformation("Attempting to add menu item '{ItemName}'", NewItem.Name);
        try
        {
            // Invalid form: reload data, flag the modal to reopen, and re-render.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for new menu item form");
                await LoadDataAsync();
                ViewData["OpenModal"] = true;
                return Page();
            }

            // Get the category by name, creating it on the fly if it doesn't exist.
            var cat = await _categoryRepo.GetByNameAsync(NewItem.Category.Trim());
            if (cat == null)
            {
                _logger.LogInformation("Category '{CategoryName}' does not exist; creating automatically", NewItem.Category.Trim());
                cat = await _categoryRepo.AddAsync(new MenuCategory { Name = NewItem.Category.Trim() });
            }

            var item = await _menuItemRepo.AddAsync(new MenuItem
            {
                Name = NewItem.Name.Trim(),
                Description = NewItem.Description.Trim(),
                Price = NewItem.Price,
                CategoryId = cat.Id,
                ImageUrl = string.IsNullOrWhiteSpace(NewItem.ImageUrl) ? null : NewItem.ImageUrl.Trim(),
                IsAvailable = true
            });

            _logger.LogInformation("Successfully added menu item '{ItemName}' with id {ItemId}", item.Name, item.Id);
            TempData["Message"] = $"Added \"{NewItem.Name}\" to the menu.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding menu item '{ItemName}'", NewItem.Name);
            throw;
        }
    }

    // Updates an existing item's fields; reopens the edit modal if the form is invalid.
    public async Task<IActionResult> OnPostEditItemAsync()
    {
        _logger.LogInformation("Attempting to edit menu item {ItemId}", EditItem.Id);
        try
        {
            // Invalid form: reload data, flag the edit modal to reopen, and re-render.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for edit menu item form {ItemId}", EditItem.Id);
                await LoadDataAsync();
                ViewData["OpenEditModal"] = true;
                return Page();
            }

            var existingItem = await _menuItemRepo.GetByIdAsync(EditItem.Id);
            if (existingItem == null)
            {
                _logger.LogWarning("Edit failed: menu item {ItemId} not found", EditItem.Id);
                TempData["Error"] = "Item not found.";
                return RedirectToPage();
            }

            // Handle category (get or create)
            var cat = await _categoryRepo.GetByNameAsync(EditItem.Category.Trim());
            if (cat == null)
            {
                _logger.LogInformation("Category '{CategoryName}' does not exist; creating automatically", EditItem.Category.Trim());
                cat = await _categoryRepo.AddAsync(new MenuCategory { Name = EditItem.Category.Trim() });
            }

            // Update fields
            existingItem.Name = EditItem.Name.Trim();
            existingItem.Description = EditItem.Description.Trim();
            existingItem.Price = EditItem.Price;
            existingItem.CategoryId = cat.Id;
            existingItem.ImageUrl = string.IsNullOrWhiteSpace(EditItem.ImageUrl) ? null : EditItem.ImageUrl.Trim();

            await _menuItemRepo.UpdateAsync(existingItem);

            _logger.LogInformation("Successfully updated menu item {ItemId}", EditItem.Id);
            TempData["Message"] = $"Updated \"{existingItem.Name}\".";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while editing menu item {ItemId}", EditItem.Id);
            throw;
        }
    }

    // Renames a category, rejecting a name already used by a different category.
    public async Task<IActionResult> OnPostEditCategoryAsync()
    {
        _logger.LogInformation("Attempting to edit category {CategoryId}", EditCategory.Id);
        try
        {
            // Invalid form: reload data, flag the edit-category modal to reopen, and re-render.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for edit category form {CategoryId}", EditCategory.Id);
                await LoadDataAsync();
                ViewData["OpenEditCategoryModal"] = true;
                return Page();
            }

            var existingCategory = await _categoryRepo.GetByIdAsync(EditCategory.Id);
            if (existingCategory == null)
            {
                _logger.LogWarning("Edit failed: category {CategoryId} not found", EditCategory.Id);
                TempData["Error"] = "Category not found.";
                return RedirectToPage();
            }

            // Check if the new name is already taken by a DIFFERENT category
            var duplicate = await _categoryRepo.GetByNameAsync(EditCategory.Name.Trim());
            if (duplicate != null && duplicate.Id != existingCategory.Id)
            {
                _logger.LogWarning("Rename rejected: category '{CategoryName}' already exists", EditCategory.Name.Trim());
                TempData["Error"] = $"A category named \"{EditCategory.Name}\" already exists.";
                await LoadDataAsync();
                ViewData["OpenEditCategoryModal"] = true;
                return Page();
            }

            existingCategory.Name = EditCategory.Name.Trim();
            await _categoryRepo.UpdateAsync(existingCategory);

            _logger.LogInformation("Successfully renamed category {CategoryId} to '{NewName}'", EditCategory.Id, existingCategory.Name);
            TempData["Message"] = $"Renamed category to \"{existingCategory.Name}\".";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while editing category {CategoryId}", EditCategory.Id);
            throw;
        }
    }

    // Adds a category from the quick-add field, rejecting duplicates.
    public async Task<IActionResult> OnPostAddCategoryAsync()
    {
        try
        {
            // Ignore blank submissions; otherwise check for an existing name first.
            if (!string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var trimmedName = NewCategoryName.Trim();
                _logger.LogInformation("Attempting to add category '{CategoryName}'", trimmedName);
                var existing = await _categoryRepo.GetByNameAsync(trimmedName);

                // Duplicate: report an error instead of adding; otherwise create it.
                if (existing != null)
                {
                    _logger.LogWarning("Category creation rejected: '{CategoryName}' already exists", trimmedName);
                    TempData["Error"] = $"Category \"{trimmedName}\" already exists.";
                }
                else
                {
                    var created = await _categoryRepo.AddAsync(new MenuCategory { Name = trimmedName });
                    _logger.LogInformation("Successfully created category '{CategoryName}' with id {CategoryId}", created.Name, created.Id);
                    TempData["Message"] = $"Added category \"{created.Name}\".";
                }
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding category '{CategoryName}'", NewCategoryName);
            throw;
        }
    }

    // Deletes a menu item by id and reports success via TempData.
    public async Task<IActionResult> OnPostDeleteItemAsync(int id)
    {
        _logger.LogInformation("Deleting menu item {ItemId}", id);
        try
        {
            var deleted = await _menuItemRepo.DeleteAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Successfully deleted menu item {ItemId}", id);
                TempData["Message"] = "Item removed.";
            }
            else
            {
                _logger.LogWarning("Failed to delete menu item {ItemId}: not found", id);
            }
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting menu item {ItemId}", id);
            throw;
        }
    }

    // Deletes a category by id and reports success via TempData.
    public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
    {
        _logger.LogInformation("Deleting category {CategoryId}", id);
        try
        {
            var deleted = await _categoryRepo.DeleteAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Successfully deleted category {CategoryId}", id);
                TempData["Message"] = "Category removed.";
            }
            else
            {
                _logger.LogWarning("Failed to delete category {CategoryId}: not found", id);
            }
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting category {CategoryId}", id);
            throw;
        }
    }

    // Loads categories (alphabetical) and items (by category name, then item name) for the page.
    private async Task LoadDataAsync()
    {
        Categories = (await _categoryRepo.GetAllAsync()).OrderBy(c => c.Name).ToList();
        Items = (await _menuItemRepo.GetAllAsync())
            .OrderBy(i => i.Category?.Name ?? "")
            .ThenBy(i => i.Name)
            .ToList();
    }

    // Form models for the add/edit actions; each maps directly to its modal's fields.
    public class NewItemInput
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
    }

    public class EditItemInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
    }

    public class EditCategoryInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
