using System.Collections.Generic;
using System.Threading.Tasks;
using TrendyolProductAPI.Models;

namespace TrendyolProductAPI.Services
{
    public interface IProductService
    {
        Task<List<Product>> CrawlProductAsync(string productUrl);
        Task<Product> TransformProductAsync(string sku);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> GetProductBySkuAsync(string sku);
    }
} 