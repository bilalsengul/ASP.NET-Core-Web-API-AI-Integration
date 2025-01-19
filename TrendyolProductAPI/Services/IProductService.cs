using System.Collections.Generic;
using System.Threading.Tasks;
using TrendyolProductAPI.Models;

namespace TrendyolProductAPI.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> GetProductBySkuAsync(string sku);
        Task<ProductVariants> GetProductVariantsAsync(string sku);
        Task<Product> SaveProductAsync(Product product);
        Task<IEnumerable<Product>> CrawlProductAsync(string url);
        Task<Product> TransformProductAsync(string sku);
    }
}