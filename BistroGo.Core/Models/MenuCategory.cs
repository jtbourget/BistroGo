using System;
using System.Collections.Generic;
using System.Text;

namespace BistroGo.Core.Models
{
    public class MenuCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }
}
