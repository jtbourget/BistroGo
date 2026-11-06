using BistroGo.Core.Identity;
using BistroGo.Core.Models;
using BistroGo.Core.Pricing;

namespace BistroGo.Tests;

public class OrderPricingTests
{
    // Builds an order containing the given lines, so each test stays short.
    private static Order OrderWith(params OrderItem[] lines) =>
        new() { Items = lines.ToList() };

    [Fact]
    public void Recalculate_AddsEightPercentTaxToTheSubtotal()
    {
        var order = OrderWith(
            new OrderItem { Quantity = 2, UnitPrice = 14.00m },
            new OrderItem { Quantity = 1, UnitPrice = 9.00m });

        OrderPricing.Recalculate(order);

        Assert.Equal(37.00m, order.Subtotal);
        Assert.Equal(2.96m, order.Tax);
        Assert.Equal(39.96m, order.Total);
    }

    [Fact]
    public void Recalculate_RoundsTaxToTheCent()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 10.01m });

        OrderPricing.Recalculate(order);

        Assert.Equal(0.80m, order.Tax);      // 10.01 * 0.08 = 0.8008
        Assert.Equal(10.81m, order.Total);
    }

    [Fact]
    public void Recalculate_IgnoresArchivedLines()
    {
        var order = OrderWith(
            new OrderItem { Quantity = 1, UnitPrice = 10.00m },
            new OrderItem { Quantity = 5, UnitPrice = 100.00m, IsArchived = true });

        OrderPricing.Recalculate(order);

        Assert.Equal(10.00m, order.Subtotal);
        Assert.Equal(10.80m, order.Total);
    }

    [Fact]
    public void Recalculate_WithNoActiveLines_ResetsTotalsToZero()
    {
        var order = new Order { Subtotal = 20m, Tax = 1.60m, Total = 21.60m };

        OrderPricing.Recalculate(order);

        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.Tax);
        Assert.Equal(0m, order.Total);
    }

    // ============================================================================
    // EMPLOYEE DISCOUNT TESTS
    // ============================================================================

    [Fact]
    public void Recalculate_WithNullRoles_DoesNotApplyDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });

        OrderPricing.Recalculate(order, userRoles: null);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(8.00m, order.Tax);      // Tax on full subtotal
        Assert.Equal(108.00m, order.Total);
    }

    [Fact]
    public void Recalculate_WithCustomerRole_DoesNotApplyDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.Customer };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(8.00m, order.Tax);
        Assert.Equal(108.00m, order.Total);
    }

    [Fact]
    public void Recalculate_WithKitchenStaffRole_AppliesTwentyPercentDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.KitchenStaff };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);                    // 20% of $100
        Assert.Equal(80.00m, order.Total - order.Tax);                 // Discounted subtotal
        Assert.Equal(6.40m, order.Tax);                                // Tax on $80 (8% of $80)
        Assert.Equal(86.40m, order.Total);                             // $80 + $6.40
    }

    [Fact]
    public void Recalculate_WithManagerRole_AppliesTwentyPercentDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.Manager };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(80.00m, order.Total - order.Tax);
        Assert.Equal(6.40m, order.Tax);
        Assert.Equal(86.40m, order.Total);
    }

    [Fact]
    public void Recalculate_WithMultipleRoles_IncludingKitchenStaff_AppliesDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.Customer, RoleNames.KitchenStaff };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(6.40m, order.Tax);
        Assert.Equal(86.40m, order.Total);
    }

    [Fact]
    public void Recalculate_WithMultipleRoles_IncludingManager_AppliesDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.Customer, RoleNames.Manager };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(6.40m, order.Tax);
        Assert.Equal(86.40m, order.Total);
    }

    [Fact]
    public void Recalculate_DiscountAnimountIsZeroForNonEligibleRoles()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = new[] { RoleNames.Customer };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(0m, order.DiscountAmount);
    }

    [Fact]
    public void Recalculate_DiscountIsAppliedBeforeTaxCalculation()
    {
        // This test ensures that tax is calculated on the *discounted* subtotal,
        // not the original subtotal. That's a critical part of the discount logic.
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 50.00m });
        var roles = new[] { RoleNames.KitchenStaff };

        OrderPricing.Recalculate(order, userRoles: roles);

        // Without discount: Tax would be 50.00 * 0.08 = 4.00
        // With discount: Tax should be 40.00 * 0.08 = 3.20
        Assert.Equal(50.00m, order.Subtotal);
        Assert.Equal(10.00m, order.DiscountAmount);
        Assert.Equal(3.20m, order.Tax);
        Assert.NotEqual(4.00m, order.Tax);
    }

    [Fact]
    public void Recalculate_WithMultipleItems_DiscountAppliedToFullSubtotal()
    {
        var order = OrderWith(
            new OrderItem { Quantity = 2, UnitPrice = 25.00m },
            new OrderItem { Quantity = 1, UnitPrice = 50.00m });
        var roles = new[] { RoleNames.Manager };

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(80.00m, order.Total - order.Tax);
        Assert.Equal(6.40m, order.Tax);
        Assert.Equal(86.40m, order.Total);
    }

    [Fact]
    public void Recalculate_WithMultipleItems_IgnoresArchivedItemsInDiscount()
    {
        var order = OrderWith(
            new OrderItem { Quantity = 1, UnitPrice = 100.00m },
            new OrderItem { Quantity = 5, UnitPrice = 100.00m, IsArchived = true });
        var roles = new[] { RoleNames.KitchenStaff };

        OrderPricing.Recalculate(order, userRoles: roles);

        // Only the active $100 item is considered
        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(80.00m, order.Total - order.Tax);
        Assert.Equal(6.40m, order.Tax);
        Assert.Equal(86.40m, order.Total);
    }

    [Fact]
    public void Recalculate_DiscountAmountRoundsCorrectly()
    {
        // Test with a price that doesn't divide evenly
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 33.33m });
        var roles = new[] { RoleNames.Manager };

        OrderPricing.Recalculate(order, userRoles: roles);

        // 33.33 * 0.2 = 6.666, should round to 6.67 (banker's rounding)
        Assert.Equal(33.33m, order.Subtotal);
        Assert.Equal(6.67m, order.DiscountAmount);
        Assert.Equal(26.66m, order.Total - order.Tax);
    }

    [Fact]
    public void Recalculate_TaxRoundsCorrectlyAfterDiscount()
    {
        // Test with values that create rounding scenarios
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 10.01m });
        var roles = new[] { RoleNames.KitchenStaff };

        OrderPricing.Recalculate(order, userRoles: roles);

        // Subtotal: 10.01
        // Discount: 10.01 * 0.2 = 2.002 rounds to 2.00
        // Discounted subtotal: 10.01 - 2.00 = 8.01
        // Tax: 8.01 * 0.08 = 0.6408 rounds to 0.64
        Assert.Equal(10.01m, order.Subtotal);
        Assert.Equal(2.00m, order.DiscountAmount);
        Assert.Equal(0.64m, order.Tax);
        Assert.Equal(8.65m, order.Total);
    }

    [Fact]
    public void Recalculate_EmptyRolesCollection_DoesNotApplyDiscount()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });
        var roles = Array.Empty<string>();

        OrderPricing.Recalculate(order, userRoles: roles);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(8.00m, order.Tax);
        Assert.Equal(108.00m, order.Total);
    }

    [Fact]
    public void Recalculate_WithNoRoles_AllowsRecalculationWithoutRoles()
    {
        var order = OrderWith(new OrderItem { Quantity = 1, UnitPrice = 100.00m });

        // Call the overload that takes no roles parameter
        OrderPricing.Recalculate(order);

        Assert.Equal(100.00m, order.Subtotal);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal(8.00m, order.Tax);
        Assert.Equal(108.00m, order.Total);
    }
}