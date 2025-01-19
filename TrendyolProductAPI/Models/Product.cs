using System;
using System.Collections.Generic;
using System.Linq;

namespace TrendyolProductAPI.Models
{
    public class ProductAttribute
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    public class Product : ICloneable
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Sku { get; set; }
        public string? ParentSku { get; set; }
        public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public decimal? Score { get; set; }
        public bool IsSaved { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? VariantId { get; set; }
        public bool IsMainVariant { get; set; }
        public List<Product> Variants { get; set; } = new List<Product>();

        public object Clone()
        {
            return new Product
            {
                Name = this.Name,
                Description = this.Description,
                Sku = this.Sku,
                ParentSku = this.ParentSku,
                Attributes = this.Attributes.Select(a => new ProductAttribute 
                { 
                    Name = a.Name,
                    Value = a.Value 
                }).ToList(),
                Category = this.Category,
                Brand = this.Brand,
                OriginalPrice = this.OriginalPrice,
                DiscountedPrice = this.DiscountedPrice,
                Images = new List<string>(this.Images),
                Score = this.Score,
                IsSaved = this.IsSaved,
                Color = this.Color,
                Size = this.Size,
                VariantId = this.VariantId,
                IsMainVariant = this.IsMainVariant,
                Variants = this.Variants.Select(v => (Product)v.Clone()).ToList()
            };
        }
    }
} 