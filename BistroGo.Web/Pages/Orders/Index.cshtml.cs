using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BistroGo.Web.Pages.Orders;

public class IndexModel : PageModel
{
    public List<OrderSummaryDto> CurrentOrders { get; set; } = new();
    public List<TableDto> Tables { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();

    public void OnGet()
    {
        LoadData();
    }

    // Completes an order — sets status to Served
    public IActionResult OnPostComplete(int id)
    {
        var order = CurrentOrders.FirstOrDefault(o => o.Id == id);
        if (order != null)
        {
            // TODO: Replace with Api call:
            //   PUT /api/orders/{id} with { status: "Served" }
            order.Status = "Served";
            TempData["Message"] = $"Order #{order.Id} marked as Served.";
        }
        return RedirectToPage();
    }

    // Changes status to a specific value (Waiting, Preparing)
    public IActionResult OnPostSetStatus(int id, string status)
    {
        var order = CurrentOrders.FirstOrDefault(o => o.Id == id);
        if (order != null && !string.IsNullOrWhiteSpace(status))
        {
            // TODO: Replace with Api call:
            //   PUT /api/orders/{id} with { status }
            order.Status = status;
            TempData["Message"] = $"Order #{order.Id} set to {status}.";
        }
        return RedirectToPage();
    }

    private void LoadData()
    {
        // TODO: Replace each of these with Api calls:
        //   CurrentOrders ← GET /api/orders?status=open
        //   Tables        ← GET /api/tables
        //   Customers     ← GET /api/customers

        CurrentOrders = new List<OrderSummaryDto>
        {
            new() { Id = 101, CustomerName = "Alice Johnson", TableNumber = 3,  PlacedAt = DateTime.Now.AddMinutes(-12), Status = "Preparing", Total = 42.50m },
            new() { Id = 102, CustomerName = "Bob Smith",     TableNumber = 7,  PlacedAt = DateTime.Now.AddMinutes(-25), Status = "Served",    Total = 28.00m },
            new() { Id = 103, CustomerName = "Carol White",   TableNumber = 1,  PlacedAt = DateTime.Now.AddMinutes(-4),  Status = "Waiting",   Total = 15.50m },
            new() { Id = 104, CustomerName = "Dan Brown",     TableNumber = 12, PlacedAt = DateTime.Now.AddMinutes(-40), Status = "Preparing", Total = 67.25m },
        };

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

        Customers = new List<CustomerDto>
        {
            new() { Name = "Alice Johnson", Phone = "555-0101", VisitCount = 12 },
            new() { Name = "Bob Smith",     Phone = "555-0102", VisitCount = 5  },
            new() { Name = "Carol White",   Phone = "555-0103", VisitCount = 22 },
        };
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