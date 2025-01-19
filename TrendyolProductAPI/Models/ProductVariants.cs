using System.Collections.Generic;

namespace TrendyolProductAPI.Models
{
    public class ProductVariants
    {
        public List<string> Colors { get; set; } = new List<string>();
        public List<string> Sizes { get; set; } = new List<string>();
        public List<Product> Variants { get; set; } = new List<Product>();
    }
}
