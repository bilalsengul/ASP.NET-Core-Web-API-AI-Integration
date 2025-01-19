using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TrendyolProductAPI.Models;

namespace TrendyolProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ILogger<ProductService> _logger;
        private readonly List<Product> _products;

        public ProductService(ILogger<ProductService> logger)
        {
            _logger = logger;
            _products = new List<Product>
            {
                new Product
                {
                    Sku = "SHK123",
                    Name = "Shaka Taba Suni Deri, Çıtçıtlı, İki Bölmeli, Cüzdanlı, Makyaj Çantalı, Askılı, El, Kol ve Omuz Çant",
                    Brand = "Shaka",
                    Color = "Taba",
                    OriginalPrice = 399.99m,
                    DiscountedPrice = 342.39m,
                    Score = 5.0m,
                    RatingCount = 2,
                    FavoriteCount = 103,
                    ShippingInfo = "Tahmini Teslimat: Adresini seç ne zaman teslim edileceğini öğren!",
                    HasFastShipping = true,
                    PaymentOptions = new List<string>
                    {
                        "12 Aya Varan Taksit Fırsatı"
                    },
                    Images = new List<string>
                    {
                        "https://cdn.dsmcdn.com/ty952/product/media/images/20230718/18/394586641/960332347/1/1_org.jpg",
                        "https://cdn.dsmcdn.com/ty952/product/media/images/20230718/18/394586641/960332347/2/2_org.jpg",
                        "https://cdn.dsmcdn.com/ty953/product/media/images/20230718/18/394586641/960332347/3/3_org.jpg",
                        "https://cdn.dsmcdn.com/ty953/product/media/images/20230718/18/394586641/960332347/4/4_org.jpg"
                    },
                    Variants = new List<Product>
                    {
                        new Product
                        {
                            Sku = "SHK123-BLACK",
                            Name = "Shaka Siyah Suni Deri, Çıtçıtlı, İki Bölmeli, Cüzdanlı, Makyaj Çantalı, Askılı, El, Kol ve Omuz Çant",
                            Color = "Siyah",
                            Images = new List<string>
                            {
                                "https://cdn.dsmcdn.com/ty952/product/media/images/20230718/18/394586641/960332347/1/1_org_black.jpg"
                            }
                        }
                    }
                }
            };
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            _logger.LogInformation("Getting all products");
            return await Task.FromResult(_products);
        }

        public async Task<Product> GetProductBySkuAsync(string sku)
        {
            _logger.LogInformation("Getting product with SKU: {Sku}", sku);
            var product = _products.FirstOrDefault(p => p.Sku == sku);
            return await Task.FromResult(product);
        }

        public async Task<ProductVariants> GetProductVariantsAsync(string sku)
        {
            _logger.LogInformation("Getting variants for product with SKU: {Sku}", sku);
            var product = await GetProductBySkuAsync(sku);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with SKU {sku} not found");
            }

            var variants = product.Variants ?? new List<Product>();
            var colors = variants.Select(v => v.Color).Where(c => c != null).Distinct().ToList();
            var sizes = variants.Select(v => v.Size).Where(s => s != null).Distinct().ToList();

            return await Task.FromResult(new ProductVariants
            {
                Colors = colors,
                Sizes = sizes,
                Variants = variants
            });
        }

        public async Task<Product> SaveProductAsync(Product product)
        {
            _logger.LogInformation("Saving product with SKU: {Sku}", product.Sku);
            var existingProduct = _products.FirstOrDefault(p => p.Sku == product.Sku);
            if (existingProduct != null)
            {
                _products.Remove(existingProduct);
            }
            _products.Add(product);
            return await Task.FromResult(product);
        }

        public async Task<IEnumerable<Product>> CrawlProductAsync(string url)
        {
            _logger.LogInformation("Crawling product from URL: {Url}", url);
            // For now, return a mock product since we're not implementing actual crawling
            var mockProduct = new Product
            {
                Sku = "CRAWLED-001",
                Name = "Crawled Product",
                Brand = "Test Brand",
                OriginalPrice = 100.00m,
                DiscountedPrice = 90.00m,
                Images = new List<string> { "https://example.com/image.jpg" }
            };
            return await Task.FromResult(new List<Product> { mockProduct });
        }

        public async Task<Product> TransformProductAsync(string sku)
        {
            _logger.LogInformation("Transforming product with SKU: {Sku}", sku);
            var product = await GetProductBySkuAsync(sku);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with SKU {sku} not found");
            }

            // For now, just return a modified version of the product
            product.Name = $"Transformed - {product.Name}";
            return await Task.FromResult(product);
        }
    }
}
