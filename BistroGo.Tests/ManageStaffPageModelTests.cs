using System.Security.Claims;
using BistroGo.Data.Identity;
using BistroGo.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace BistroGo.Tests;

public class ManageStaffPageModelTests
{
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageStaffPageModelTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);

        _userManager.GetUserId(Arg.Any<ClaimsPrincipal>())
            .Returns(callInfo => callInfo.Arg<ClaimsPrincipal>()?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    private ManageStaffModel CreateModel(ClaimsPrincipal? user = null)
    {
        user ??= new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mgr-1"),
            new Claim(ClaimTypes.Name, "manager@bistrogo.com"),
            new Claim(ClaimTypes.Role, Roles.Manager)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new ManageStaffModel(_userManager)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGetAsync_LoadsKitchenStaffAndManagers_ExcludesCustomers()
    {
        var cook = new ApplicationUser { Id = "cook-1", FirstName = "Alice", LastName = "Cook", Email = "cook@bistro.com" };
        var manager = new ApplicationUser { Id = "mgr-1", FirstName = "Bob", LastName = "Boss", Email = "manager@bistrogo.com" };

        _userManager.GetUsersInRoleAsync(Roles.KitchenStaff).Returns(new List<ApplicationUser> { cook });
        _userManager.GetUsersInRoleAsync(Roles.Manager).Returns(new List<ApplicationUser> { manager });

        _userManager.GetRolesAsync(cook).Returns(new List<string> { Roles.KitchenStaff });
        _userManager.GetRolesAsync(manager).Returns(new List<string> { Roles.Manager });

        var model = CreateModel();

        await model.OnGetAsync();

        Assert.Equal(2, model.StaffMembers.Count);
        Assert.Contains(model.StaffMembers, s => s.Id == "cook-1" && s.Role == Roles.KitchenStaff && !s.IsCurrentUser);
        Assert.Contains(model.StaffMembers, s => s.Id == "mgr-1" && s.Role == Roles.Manager && s.IsCurrentUser);
    }

    [Fact]
    public async Task OnPostCreateAsync_WhenValidKitchenStaff_CreatesUserAndAddsRole()
    {
        var model = CreateModel();
        model.CreateInput = new ManageStaffModel.CreateStaffInputModel
        {
            FirstName = "Charlie",
            LastName = "Kitchen",
            Email = "charlie@bistro.com",
            Role = Roles.KitchenStaff,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync("charlie@bistro.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password123!").Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Roles.KitchenStaff).Returns(IdentityResult.Success);

        var result = await model.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.Email == "charlie@bistro.com" && u.FirstName == "Charlie" && u.LastName == "Kitchen"),
            "Password123!");
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<ApplicationUser>(), Roles.KitchenStaff);
        Assert.Contains("Charlie", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostCreateAsync_WhenValidManager_CreatesUserAndAddsManagerRole()
    {
        var model = CreateModel();
        model.CreateInput = new ManageStaffModel.CreateStaffInputModel
        {
            FirstName = "Dana",
            LastName = "Manager",
            Email = "dana@bistro.com",
            Role = Roles.Manager,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync("dana@bistro.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password123!").Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Roles.Manager).Returns(IdentityResult.Success);

        var result = await model.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<ApplicationUser>(), Roles.Manager);
    }

    [Fact]
    public async Task OnPostCreateAsync_WhenRoleIsInvalid_AddsModelError()
    {
        var model = CreateModel();
        model.CreateInput = new ManageStaffModel.CreateStaffInputModel
        {
            FirstName = "Evil",
            LastName = "User",
            Email = "evil@bistro.com",
            Role = "SuperAdmin",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.GetUsersInRoleAsync(Arg.Any<string>()).Returns(new List<ApplicationUser>());

        var result = await model.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("CreateInput.Role"));
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnPostCreateAsync_WhenEmailAlreadyExists_AddsModelError()
    {
        var model = CreateModel();
        model.CreateInput = new ManageStaffModel.CreateStaffInputModel
        {
            FirstName = "Duplicate",
            LastName = "User",
            Email = "existing@bistro.com",
            Role = Roles.KitchenStaff,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManager.FindByEmailAsync("existing@bistro.com").Returns(new ApplicationUser { Email = "existing@bistro.com" });
        _userManager.GetUsersInRoleAsync(Arg.Any<string>()).Returns(new List<ApplicationUser>());

        var result = await model.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("CreateInput.Email"));
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnPostCreateAsync_IgnoresEditInputErrors()
    {
        var model = CreateModel();
        model.CreateInput = new ManageStaffModel.CreateStaffInputModel
        {
            FirstName = "Eve",
            LastName = "Cook",
            Email = "eve@bistro.com",
            Role = Roles.KitchenStaff,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Simulate model binding adding error for EditInput fields
        model.ModelState.AddModelError("EditInput.FirstName", "The First name field is required.");
        model.ModelState.AddModelError("EditInput.Id", "The Id field is required.");

        _userManager.FindByEmailAsync("eve@bistro.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password123!").Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Roles.KitchenStaff).Returns(IdentityResult.Success);

        var result = await model.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).CreateAsync(Arg.Any<ApplicationUser>(), "Password123!");
    }

    [Fact]
    public async Task OnPostEditAsync_WhenValidWithoutPasswordChange_UpdatesUser()
    {
        var existingStaff = new ApplicationUser
        {
            Id = "staff-1",
            FirstName = "Frank",
            LastName = "Old",
            Email = "frank.old@bistro.com",
            UserName = "frank.old@bistro.com"
        };

        _userManager.FindByIdAsync("staff-1").Returns(existingStaff);
        _userManager.GetRolesAsync(existingStaff).Returns(new List<string> { Roles.KitchenStaff });
        _userManager.FindByEmailAsync("frank.new@bistro.com").Returns((ApplicationUser?)null);
        _userManager.UpdateAsync(existingStaff).Returns(IdentityResult.Success);

        var model = CreateModel();
        model.EditInput = new ManageStaffModel.EditStaffInputModel
        {
            Id = "staff-1",
            FirstName = "Frank",
            LastName = "New",
            Email = "frank.new@bistro.com",
            Role = Roles.KitchenStaff
        };

        var result = await model.OnPostEditAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Frank", existingStaff.FirstName);
        Assert.Equal("New", existingStaff.LastName);
        Assert.Equal("frank.new@bistro.com", existingStaff.Email);
        await _userManager.Received(1).UpdateAsync(existingStaff);
        await _userManager.DidNotReceive().ResetPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnPostEditAsync_WhenRoleChanged_UpdatesRoles()
    {
        var staff = new ApplicationUser
        {
            Id = "staff-2",
            FirstName = "Grace",
            LastName = "Cook",
            Email = "grace@bistro.com",
            UserName = "grace@bistro.com"
        };

        _userManager.FindByIdAsync("staff-2").Returns(staff);
        _userManager.GetRolesAsync(staff).Returns(new List<string> { Roles.KitchenStaff });
        _userManager.RemoveFromRolesAsync(staff, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(staff, Roles.Manager).Returns(IdentityResult.Success);
        _userManager.UpdateAsync(staff).Returns(IdentityResult.Success);

        var model = CreateModel();
        model.EditInput = new ManageStaffModel.EditStaffInputModel
        {
            Id = "staff-2",
            FirstName = "Grace",
            LastName = "Cook",
            Email = "grace@bistro.com",
            Role = Roles.Manager
        };

        var result = await model.OnPostEditAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).RemoveFromRolesAsync(staff, Arg.Is<IEnumerable<string>>(r => r.Contains(Roles.KitchenStaff)));
        await _userManager.Received(1).AddToRoleAsync(staff, Roles.Manager);
    }

    [Fact]
    public async Task OnPostEditAsync_WhenDemotingOnlyManager_FailsWithModelError()
    {
        var soleManager = new ApplicationUser
        {
            Id = "mgr-sole",
            FirstName = "Sole",
            LastName = "Manager",
            Email = "sole@bistro.com"
        };

        _userManager.FindByIdAsync("mgr-sole").Returns(soleManager);
        _userManager.GetRolesAsync(soleManager).Returns(new List<string> { Roles.Manager });
        _userManager.GetUsersInRoleAsync(Roles.Manager).Returns(new List<ApplicationUser> { soleManager });
        _userManager.GetUsersInRoleAsync(Roles.KitchenStaff).Returns(new List<ApplicationUser>());

        var model = CreateModel();
        model.EditInput = new ManageStaffModel.EditStaffInputModel
        {
            Id = "mgr-sole",
            FirstName = "Sole",
            LastName = "Manager",
            Email = "sole@bistro.com",
            Role = Roles.KitchenStaff
        };

        var result = await model.OnPostEditAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("EditInput.Role"));
        await _userManager.DidNotReceive().UpdateAsync(soleManager);
    }

    [Fact]
    public async Task OnPostEditAsync_WhenPasswordProvided_ResetsPassword()
    {
        var staff = new ApplicationUser
        {
            Id = "staff-3",
            FirstName = "Hank",
            LastName = "Chef",
            Email = "hank@bistro.com"
        };

        _userManager.FindByIdAsync("staff-3").Returns(staff);
        _userManager.GetRolesAsync(staff).Returns(new List<string> { Roles.KitchenStaff });
        _userManager.GeneratePasswordResetTokenAsync(staff).Returns("reset-token");
        _userManager.ResetPasswordAsync(staff, "reset-token", "BrandNewPassword123!").Returns(IdentityResult.Success);
        _userManager.UpdateAsync(staff).Returns(IdentityResult.Success);

        var model = CreateModel();
        model.EditInput = new ManageStaffModel.EditStaffInputModel
        {
            Id = "staff-3",
            FirstName = "Hank",
            LastName = "Chef",
            Email = "hank@bistro.com",
            Role = Roles.KitchenStaff,
            NewPassword = "BrandNewPassword123!",
            ConfirmNewPassword = "BrandNewPassword123!"
        };

        var result = await model.OnPostEditAsync();

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).ResetPasswordAsync(staff, "reset-token", "BrandNewPassword123!");
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenValidStaff_DeletesSuccessfully()
    {
        var cook = new ApplicationUser
        {
            Id = "cook-delete",
            FirstName = "Cook",
            LastName = "Temp",
            Email = "cooktemp@bistro.com"
        };

        _userManager.FindByIdAsync("cook-delete").Returns(cook);
        _userManager.GetRolesAsync(cook).Returns(new List<string> { Roles.KitchenStaff });
        _userManager.DeleteAsync(cook).Returns(IdentityResult.Success);

        var model = CreateModel();

        var result = await model.OnPostDeleteAsync("cook-delete");

        Assert.IsType<RedirectToPageResult>(result);
        await _userManager.Received(1).DeleteAsync(cook);
        Assert.Contains("cooktemp@bistro.com", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenDeletingSelf_SetsErrorMessage()
    {
        var model = CreateModel(); // current user is "mgr-1"

        var result = await model.OnPostDeleteAsync("mgr-1");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("You cannot delete your own account.", model.ErrorMessage);
        await _userManager.DidNotReceive().DeleteAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenDeletingOnlyManager_SetsErrorMessage()
    {
        var soleManager = new ApplicationUser
        {
            Id = "mgr-other",
            FirstName = "Other",
            LastName = "Manager",
            Email = "other@bistro.com"
        };

        _userManager.FindByIdAsync("mgr-other").Returns(soleManager);
        _userManager.GetRolesAsync(soleManager).Returns(new List<string> { Roles.Manager });
        _userManager.GetUsersInRoleAsync(Roles.Manager).Returns(new List<ApplicationUser> { soleManager });

        var model = CreateModel(); // current user is "mgr-1"

        var result = await model.OnPostDeleteAsync("mgr-other");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Cannot delete the only manager account in the system.", model.ErrorMessage);
        await _userManager.DidNotReceive().DeleteAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenTargetIsNotStaff_SetsErrorMessage()
    {
        var customer = new ApplicationUser
        {
            Id = "cust-1",
            FirstName = "Cust",
            LastName = "Omer",
            Email = "cust@example.com"
        };

        _userManager.FindByIdAsync("cust-1").Returns(customer);
        _userManager.GetRolesAsync(customer).Returns(new List<string> { Roles.Customer });

        var model = CreateModel();

        var result = await model.OnPostDeleteAsync("cust-1");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Only staff accounts can be deleted from this page.", model.ErrorMessage);
        await _userManager.DidNotReceive().DeleteAsync(customer);
    }
}
