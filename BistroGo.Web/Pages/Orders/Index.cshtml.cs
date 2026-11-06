using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BistroGo.Data.Identity;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;

namespace BistroGo.Web.Pages.Orders;

[Authorize(Roles = Roles.Manager + "," + Roles.KitchenStaff)]
public class IndexModel : PageModel
{
    private readonly IOrderRepository _orderRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IOrderRepository orderRepo, UserManager<ApplicationUser> userManager)
    {
        _orderRepo = orderRepo;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public List<OrderSummaryDto> CurrentOrders { get; set; } = new();
    public List<TableDto> Tables { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();

    public int AllCount { get; set; }
    public int ReceivedCount { get; set; }
    public int PreparingCount { get; set; }
    public int ReadyCount { get; set; }
    public int PickedUpCount { get; set; }
    public int CancelledCount { get; set; }

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
        return RedirectToPage(new { statusFilter = StatusFilter });
    }

    // Changes status to a specific value (Received, Preparing, Ready, PickedUp, Cancelled)
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
            else if (status.Equals("PickedUp", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                order.StatusId = OrderStatusIds.PickedUp;
            else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || status.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
                order.StatusId = OrderStatusIds.Cancelled;

            await _orderRepo.UpdateAsync(order);
            TempData["Message"] = $"Order #{order.Id} set to {status}.";
        }
        return RedirectToPage(new { statusFilter = StatusFilter });
    }

    private async Task LoadDataAsync()
    {
        var allOrders = await _orderRepo.GetAllAsync();
        var finalizedOrders = allOrders.Where(o => o.IsFinalized).ToList();

        var summaries = new List<OrderSummaryDto>();

        foreach (var o in finalizedOrders)
        {
            string customerName = "Customer";
            if (!string.IsNullOrWhiteSpace(o.GuestName))
            {
                customerName = o.GuestName;
            }
            else if (!string.IsNullOrWhiteSpace(o.UserId))
            {
                var user = await _userManager.FindByIdAsync(o.UserId);
                if (user != null)
                {
                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    customerName = !string.IsNullOrWhiteSpace(fullName) ? fullName : user.UserName ?? "Customer";
                }
            }

            summaries.Add(new OrderSummaryDto
            {
                Id = o.Id,
                CustomerName = customerName,
                CustomerPhone = o.GuestPhone,
                CustomerEmail = o.GuestEmail,
                TableNumber = o.PickupSlotId ?? 1,
                PlacedAt = o.FinalizedAt ?? o.CreatedAt,
                Status = o.Status?.Name ?? "Received",
                Total = o.Total,
                ConfirmationCode = o.ConfirmationCode,
                Items = o.Items.Select(i => new OrderItemSummaryDto
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    MenuItemName = i.MenuItem?.Name ?? $"Item #{i.MenuItemId}",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SpecialInstructions = i.SpecialInstructions
                }).ToList()
            });
        }

        AllCount = summaries.Count;
        ReceivedCount = summaries.Count(s => s.Status.Equals("Received", StringComparison.OrdinalIgnoreCase));
        PreparingCount = summaries.Count(s => s.Status.Equals("Preparing", StringComparison.OrdinalIgnoreCase));
        ReadyCount = summaries.Count(s => s.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase));
        PickedUpCount = summaries.Count(s => s.Status.Equals("PickedUp", StringComparison.OrdinalIgnoreCase));
        CancelledCount = summaries.Count(s => s.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(StatusFilter) && !StatusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            CurrentOrders = summaries
                .Where(s => s.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            CurrentOrders = summaries;
        }

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

        Customers = summaries
            .Where(s => !string.IsNullOrWhiteSpace(s.CustomerName))
            .GroupBy(s => s.CustomerName)
            .Select(g => new CustomerDto
            {
                Name = g.Key,
                Phone = g.FirstOrDefault()?.CustomerPhone ?? "N/A",
                VisitCount = g.Count()
            })
            .ToList();
    }

    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public int TableNumber { get; set; }
        public DateTime PlacedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public string? ConfirmationCode { get; set; }
        public List<OrderItemSummaryDto> Items { get; set; } = new();
    }

    public class OrderItemSummaryDto
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? SpecialInstructions { get; set; }
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
