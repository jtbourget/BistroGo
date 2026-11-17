using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BistroGo.Core.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Http;

namespace BistroGo.Web.Pages.Inventory;

// Manager-only page for managing menu items and categories (add, edit, delete).
// Uses a mix of [BindProperty] inputs and named POST handlers for each action.
[Authorize(Roles = RoleNames.Manager)]
public class IndexModel : PageModel
{
    private readonly IMenuItemRepository _menuItemRepo;
    private readonly IMenuCategoryRepository _categoryRepo;

    public IndexModel(IMenuItemRepository menuItemRepo, IMenuCategoryRepository categoryRepo)
    {
        _menuItemRepo = menuItemRepo;
        _categoryRepo = categoryRepo;
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
        await LoadDataAsync();
    }

    // Adds a new menu item; reopens the modal with errors if the form is invalid.
    public async Task<IActionResult> OnPostAddItemAsync()
    {
        // Invalid form: reload data, flag the modal to reopen, and re-render.
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            ViewData["OpenModal"] = true;
            return Page();
        }

        // Get the category by name, creating it on the fly if it doesn't exist.
        var cat = await _categoryRepo.GetByNameAsync(NewItem.Category.Trim());
        if (cat == null)
        {
            cat = await _categoryRepo.AddAsync(new MenuCategory { Name = NewItem.Category.Trim() });
        }

        // Handle Images Upload
        string? imagePath = null;
        if (NewItem.ImageFile != null && NewItem.ImageFile.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(NewItem.ImageFile.FileName);
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/menu", fileName);
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await NewItem.ImageFile.CopyToAsync(stream);
            }
            imagePath = "/images/menu/" + fileName;
        }

        await _menuItemRepo.AddAsync(new MenuItem
        {
            Name = NewItem.Name.Trim(),
            Description = NewItem.Description.Trim(),
            Price = NewItem.Price,
            CategoryId = cat.Id,
            ImageUrl = imagePath,
            IsAvailable = true
        });

        TempData["Message"] = $"Added \"{NewItem.Name}\" to the menu.";
        return RedirectToPage();
    }

    // Updates an existing item's fields; reopens the edit modal if the form is invalid.
    public async Task<IActionResult> OnPostEditItemAsync()
    {
        // Invalid form: reload data, flag the edit modal to reopen, and re-render.
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

        // Handle File Upload for Edit
        string? imagePath = existingItem.ImageUrl; 
        if (EditItem.ImageFile != null && EditItem.ImageFile.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(EditItem.ImageFile.FileName);
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/menu", fileName);
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await EditItem.ImageFile.CopyToAsync(stream);
            }
            imagePath = "/images/menu/" + fileName;

            // Delete the old image to save space
            if (!string.IsNullOrEmpty(existingItem.ImageUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingItem.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
        }
        // Update fields
        existingItem.Name = EditItem.Name.Trim();
        existingItem.Description = EditItem.Description.Trim();
        existingItem.Price = EditItem.Price;
        existingItem.CategoryId = cat.Id;
        existingItem.ImageUrl = imagePath;

        await _menuItemRepo.UpdateAsync(existingItem);

        TempData["Message"] = $"Updated \"{existingItem.Name}\".";
        return RedirectToPage();
    }

    // Renames a category, rejecting a name already used by a different category.
    public async Task<IActionResult> OnPostEditCategoryAsync()
    {
        // Invalid form: reload data, flag the edit-category modal to reopen, and re-render.
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

    // Adds a category from the quick-add field, rejecting duplicates.
    public async Task<IActionResult> OnPostAddCategoryAsync()
    {
        // Ignore blank submissions; otherwise check for an existing name first.
        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            var trimmedName = NewCategoryName.Trim();
            var existing = await _categoryRepo.GetByNameAsync(trimmedName);

            // Duplicate: report an error instead of adding; otherwise create it.
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

    // Deletes a menu item by id and reports success via TempData.
    public async Task<IActionResult> OnPostDeleteItemAsync(int id)
    {
        var deleted = await _menuItemRepo.DeleteAsync(id);
        if (deleted)
        {
            TempData["Message"] = "Item removed.";
        }
        return RedirectToPage();
    }

    // Deletes a category by id and reports success via TempData.
    public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
    {
        var deleted = await _categoryRepo.DeleteAsync(id);
        if (deleted)
        {
            TempData["Message"] = "Category removed.";
        }
        return RedirectToPage();
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
        public IFormFile? ImageFile { get; set; } // Optional file upload for the image
    }

    public class EditItemInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public IFormFile? ImageFile { get; set; } // Optional file upload for the image
        public string? ExistingImageUrl { get; set; } // For keeping the old image if no new one is uploaded
    }

    public class EditCategoryInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}