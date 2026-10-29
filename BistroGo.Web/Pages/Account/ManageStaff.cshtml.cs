using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BistroGo.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Web.Pages.Account
{
    /// <summary>
    /// Page model for the Manage Staff page.
    /// This page allows a Manager to view, create, edit, and delete staff accounts (KitchenStaff and Manager).
    /// Requires the user to have the Manager role.
    /// </summary>
    [Authorize(Roles = Roles.Manager)]
    public class ManageStaffModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Constructor for ManageStaffModel.
        /// </summary>
        /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
        public ManageStaffModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public List<StaffMemberViewModel> StaffMembers { get; set; } = new();

        [BindProperty]
        public CreateStaffInputModel CreateInput { get; set; } = new();

        [BindProperty]
        public EditStaffInputModel EditInput { get; set; } = new();

        // Backward compatibility property for any code referencing Input
        public CreateStaffInputModel Input
        {
            get => CreateInput;
            set => CreateInput = value;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class StaffMemberViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsCurrentUser { get; set; }
        }

        public class CreateStaffInputModel
        {
            [Required]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; } = Roles.KitchenStaff;

            [Required]
            [DataType(DataType.Password)]
            [MinLength(8, ErrorMessage = "The password must be at least 8 characters long.")]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords don't match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Backward compatibility alias for InputModel
        public class InputModel : CreateStaffInputModel
        {
        }

        public class EditStaffInputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; } = Roles.KitchenStaff;

            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            public string? ConfirmNewPassword { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadStaffMembersAsync();
        }

        /// <summary>
        /// Handles the default POST request (delegating to Create for backward compatibility).
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            return await OnPostCreateAsync();
        }

        /// <summary>
        /// Handles the POST request to create a new staff account.
        /// </summary>
        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Remove validation errors belonging to the edit form
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith(nameof(EditInput)) || k.StartsWith("EditInput.")).ToList())
            {
                ModelState.Remove(key);
            }

            if (CreateInput.Role != Roles.KitchenStaff && CreateInput.Role != Roles.Manager)
            {
                ModelState.AddModelError("CreateInput.Role", "Invalid role selected.");
            }

            if (!ModelState.IsValid)
            {
                await LoadStaffMembersAsync();
                ViewData["OpenCreateModal"] = true;
                return Page();
            }

            var existing = await _userManager.FindByEmailAsync(CreateInput.Email);
            if (existing != null)
            {
                ModelState.AddModelError("CreateInput.Email", $"A user with email '{CreateInput.Email}' already exists.");
                await LoadStaffMembersAsync();
                ViewData["OpenCreateModal"] = true;
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = CreateInput.Email,
                Email = CreateInput.Email,
                FirstName = CreateInput.FirstName,
                LastName = CreateInput.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, CreateInput.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadStaffMembersAsync();
                ViewData["OpenCreateModal"] = true;
                return Page();
            }

            await _userManager.AddToRoleAsync(user, CreateInput.Role);

            StatusMessage = $"Staff account for {user.FirstName} {user.LastName} was successfully created.";
            return RedirectToPage();
        }

        /// <summary>
        /// Handles the POST request to edit an existing staff account.
        /// </summary>
        public async Task<IActionResult> OnPostEditAsync()
        {
            // Remove validation errors belonging to the create form
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith(nameof(CreateInput)) || k.StartsWith("CreateInput.") || k.StartsWith("Input.")).ToList())
            {
                ModelState.Remove(key);
            }

            if (EditInput.Role != Roles.KitchenStaff && EditInput.Role != Roles.Manager)
            {
                ModelState.AddModelError("EditInput.Role", "Invalid role selected.");
            }

            if (!string.IsNullOrWhiteSpace(EditInput.NewPassword))
            {
                if (EditInput.NewPassword.Length < 8)
                {
                    ModelState.AddModelError("EditInput.NewPassword", "The password must be at least 8 characters long.");
                }
                if (EditInput.NewPassword != EditInput.ConfirmNewPassword)
                {
                    ModelState.AddModelError("EditInput.ConfirmNewPassword", "Passwords don't match.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadStaffMembersAsync();
                ViewData["OpenEditModal"] = true;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(EditInput.Id);
            if (user == null)
            {
                ErrorMessage = "Staff account not found.";
                return RedirectToPage();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(Roles.KitchenStaff) && !currentRoles.Contains(Roles.Manager))
            {
                ErrorMessage = "Only staff accounts can be edited from this page.";
                return RedirectToPage();
            }

            if (!string.Equals(user.Email, EditInput.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(EditInput.Email);
                if (existing != null && existing.Id != user.Id)
                {
                    ModelState.AddModelError("EditInput.Email", $"A user with email '{EditInput.Email}' already exists.");
                    await LoadStaffMembersAsync();
                    ViewData["OpenEditModal"] = true;
                    return Page();
                }

                user.Email = EditInput.Email;
                user.UserName = EditInput.Email;
                user.NormalizedEmail = EditInput.Email.ToUpperInvariant();
                user.NormalizedUserName = EditInput.Email.ToUpperInvariant();
            }

            user.FirstName = EditInput.FirstName;
            user.LastName = EditInput.LastName;

            if (!currentRoles.Contains(EditInput.Role))
            {
                if (currentRoles.Contains(Roles.Manager) && EditInput.Role != Roles.Manager)
                {
                    var managers = await _userManager.GetUsersInRoleAsync(Roles.Manager);
                    if (managers.Count <= 1 && managers.Any(m => m.Id == user.Id))
                    {
                        ModelState.AddModelError("EditInput.Role", "Cannot change the role of the only manager in the system.");
                        await LoadStaffMembersAsync();
                        ViewData["OpenEditModal"] = true;
                        return Page();
                    }
                }

                var rolesToRemove = currentRoles.Where(r => r == Roles.KitchenStaff || r == Roles.Manager).ToList();
                if (rolesToRemove.Count > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        foreach (var error in removeResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await LoadStaffMembersAsync();
                        ViewData["OpenEditModal"] = true;
                        return Page();
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(user, EditInput.Role);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadStaffMembersAsync();
                    ViewData["OpenEditModal"] = true;
                    return Page();
                }
            }

            if (!string.IsNullOrWhiteSpace(EditInput.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, EditInput.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadStaffMembersAsync();
                    ViewData["OpenEditModal"] = true;
                    return Page();
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadStaffMembersAsync();
                ViewData["OpenEditModal"] = true;
                return Page();
            }

            StatusMessage = $"Staff account for {user.FirstName} {user.LastName} was successfully updated.";
            return RedirectToPage();
        }

        /// <summary>
        /// Handles the POST request to delete a staff account.
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ErrorMessage = "Invalid staff account ID.";
                return RedirectToPage();
            }

            var currentUserId = _userManager.GetUserId(User) ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId) && id == currentUserId)
            {
                ErrorMessage = "You cannot delete your own account.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ErrorMessage = "Staff account not found.";
                return RedirectToPage();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(Roles.KitchenStaff) && !currentRoles.Contains(Roles.Manager))
            {
                ErrorMessage = "Only staff accounts can be deleted from this page.";
                return RedirectToPage();
            }

            if (currentRoles.Contains(Roles.Manager))
            {
                var managers = await _userManager.GetUsersInRoleAsync(Roles.Manager);
                if (managers.Count <= 1 && managers.Any(m => m.Id == user.Id))
                {
                    ErrorMessage = "Cannot delete the only manager account in the system.";
                    return RedirectToPage();
                }
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                }
                else
                {
                    StatusMessage = $"Staff account '{user.Email}' was successfully deleted.";
                }
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Cannot delete this staff member because they have associated records in the system.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteItemAsync(string id)
        {
            return await OnPostDeleteAsync(id);
        }

        private async Task LoadStaffMembersAsync()
        {
            var currentUserId = _userManager.GetUserId(User) ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var kitchenStaff = await _userManager.GetUsersInRoleAsync(Roles.KitchenStaff) ?? new List<ApplicationUser>();
            var managers = await _userManager.GetUsersInRoleAsync(Roles.Manager) ?? new List<ApplicationUser>();

            var allStaff = kitchenStaff
                .Concat(managers)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToList();

            StaffMembers = new List<StaffMemberViewModel>();

            foreach (var staff in allStaff)
            {
                var roles = await _userManager.GetRolesAsync(staff) ?? new List<string>();
                var roleDisplay = roles.Contains(Roles.Manager) ? Roles.Manager : Roles.KitchenStaff;

                StaffMembers.Add(new StaffMemberViewModel
                {
                    Id = staff.Id,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.Email ?? staff.UserName ?? string.Empty,
                    Role = roleDisplay,
                    CreatedAt = staff.CreatedAt,
                    IsCurrentUser = !string.IsNullOrEmpty(currentUserId) && staff.Id == currentUserId
                });
            }
        }
    }
}
