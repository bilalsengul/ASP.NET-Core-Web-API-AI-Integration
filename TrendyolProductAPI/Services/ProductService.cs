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
                _logger.LogInformation("Removing existing product with SKU: {Sku}", existingProduct.Sku);
                _products.Remove(existingProduct);
            }
            _products.Add(product);
            _logger.LogInformation("Product saved successfully. Total products in list: {Count}", _products.Count);
            _logger.LogInformation("Products in list: {Products}", string.Join(", ", _products.Select(p => p.Sku)));
            return await Task.FromResult(product);
        }

        public async Task<IEnumerable<Product>> CrawlProductAsync(string url)
        {
            _logger.LogInformation("Crawling product from URL: {Url}", url);
            
            // Create a mock product with variants
            var mockProduct = new Product
            {
                Sku = "CRAWLED-001",
                Name = "Crawled Product",
                Brand = "Test Brand",
                OriginalPrice = 100.00m,
                DiscountedPrice = 90.00m,
                Images = new List<string> { "https://example.com/image.jpg" },
                Variants = new List<Product>
                {
                    new Product
                    {
                        Sku = "CRAWLED-001-BLACK",
                        Name = "Crawled Product - Black",
                        Color = "Black",
                        OriginalPrice = 100.00m,
                        DiscountedPrice = 90.00m,
                        Images = new List<string> { "https://example.com/image-black.jpg" }
                    },
                    new Product
                    {
                        Sku = "CRAWLED-001-WHITE",
                        Name = "Crawled Product - White",
                        Color = "White",
                        OriginalPrice = 100.00m,
                        DiscountedPrice = 90.00m,
                        Images = new List<string> { "https://example.com/image-white.jpg" }
                    }
                }
            };

            _logger.LogInformation("Created mock product with SKU: {Sku}", mockProduct.Sku);
            _logger.LogInformation("Mock product has {VariantCount} variants", mockProduct.Variants?.Count ?? 0);

            // Save only the main product (which includes variants)
            await SaveProductAsync(mockProduct);

            // Verify the product was saved
            var savedProduct = await GetProductBySkuAsync(mockProduct.Sku);
            if (savedProduct != null)
            {
                _logger.LogInformation("Successfully retrieved saved product with SKU: {Sku}", savedProduct.Sku);
                _logger.LogInformation("Saved product has {VariantCount} variants", savedProduct.Variants?.Count ?? 0);
            }
            else
            {
                _logger.LogError("Failed to retrieve saved product with SKU: {Sku}", mockProduct.Sku);
            }

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
