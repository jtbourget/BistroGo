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
}