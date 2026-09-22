using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Core.Models
{
    public class MenuItem
    {
        public int Id { get; set; } // Unique ID for the item
        public string Name { get; set; } = ""; // Name of the item
        public string Description { get; set; } = ""; // Short description of the item
        public decimal Price { get; set; } // $$$
        public string Category { get; set; } = ""; // Appetizer, Main, Dessert, etc.
        
    }
}
