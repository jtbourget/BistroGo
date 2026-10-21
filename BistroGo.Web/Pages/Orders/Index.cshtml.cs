using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using BistroGo.Data.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Web.Pages.Orders;

[Authorize(Roles = Roles.Manager + "," + Roles.KitchenStaff)]
public class IndexModel : PageModel
{
    private readonly IOrderRepository _orderRepo;

    public IndexModel(IOrderRepository orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public List<OrderSummaryDto> CurrentOrders { get; set; } = new();
    public List<TableDto> Tables { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    // Completes an order — sets status to PickedUp/Served
    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order != null)
        {
            order.StatusId = OrderStatusIds.PickedUp;
            await _orderRepo.UpdateAsync(order);
            TempData["Message"] = $"Order #{order.Id} marked as Picked Up / Completed.";
        }
        return RedirectToPage();
    }

    // Changes status to a specific value (Preparing, Ready, etc.)
    public async Task<IActionResult> OnPostSetStatusAsync(int id, string status)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order != null && !string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("Preparing", StringComparison.OrdinalIgnoreCase))
                order.StatusId = OrderStatusIds.Preparing;
            else if (status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                order.StatusId = OrderStatusIds.Ready;
            else if (status.Equals("Waiting", StringComparison.OrdinalIgnoreCase) || status.Equals("Received", StringComparison.OrdinalIgnoreCase))
                order.StatusId = OrderStatusIds.Received;

            await _orderRepo.UpdateAsync(order);
            TempData["Message"] = $"Order #{order.Id} set to {status}.";
        }
        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        var allOrders = await _orderRepo.GetAllAsync();

        CurrentOrders = allOrders
            .Where(o => o.IsFinalized)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                CustomerName = !string.IsNullOrWhiteSpace(o.GuestName) ? o.GuestName : (!string.IsNullOrWhiteSpace(o.UserId) ? o.UserId : "Customer"),
                TableNumber = o.PickupSlotId ?? 1,
                PlacedAt = o.FinalizedAt ?? o.CreatedAt,
                Status = o.Status?.Name ?? "Received",
                Total = o.Total
            })
            .ToList();

        Tables = new List<TableDto>
        {
            new() { Number = 1,  Status = "Occupied"  },
            new() { Number = 2,  Status = "Available" },
            new() { Number = 3,  Status = "Occupied"  },
            new() { Number = 4,  Status = "Available" },
            new() { Number = 5,  Status = "Reserved"  },
            new() { Number = 6,  Status = "Available" },
            new() { Number = 7,  Status = "Occupied"  },
            new() { Number = 8,  Status = "Available" },
            new() { Number = 9,  Status = "Available" },
            new() { Number = 10, Status = "Available" },
            new() { Number = 11, Status = "Reserved"  },
            new() { Number = 12, Status = "Occupied"  },
        };

        Customers = allOrders
            .Where(o => !string.IsNullOrWhiteSpace(o.GuestName))
            .GroupBy(o => o.GuestName!)
            .Select(g => new CustomerDto
            {
                Name = g.Key,
                Phone = g.FirstOrDefault()?.GuestPhone ?? "N/A",
                VisitCount = g.Count()
            })
            .ToList();
    }

    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public int TableNumber { get; set; }
        public DateTime PlacedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class TableDto
    {
        public int Number { get; set; }
        public string Status { get; set; } = "";
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public int VisitCount { get; set; }
    }
}
