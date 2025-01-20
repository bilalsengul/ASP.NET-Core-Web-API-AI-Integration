using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using TrendyolProductAPI.Models;

namespace TrendyolProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ILogger<ProductService> _logger;
        private readonly string _productsFilePath;
        private static readonly object _lock = new object();
        private readonly IProductCrawlerService _crawlerService;

        public ProductService(ILogger<ProductService> logger, IProductCrawlerService crawlerService)
        {
            _logger = logger;
            _crawlerService = crawlerService;
            
            // Get the project root directory
            var rootDirectory = Directory.GetCurrentDirectory();
            _productsFilePath = Path.Combine(rootDirectory, "products.json");

            // Create the file if it doesn't exist
            if (!File.Exists(_productsFilePath))
            {
                File.WriteAllText(_productsFilePath, "[]");
            }

            _logger.LogInformation("Using products file at: {FilePath}", _productsFilePath);
        }

        private List<Product> ReadProducts()
        {
            lock (_lock)
            {
                try
                {
                    var json = File.ReadAllText(_productsFilePath);
                    return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading products file");
                    return new List<Product>();
                }
            }
        }

        private void WriteProducts(List<Product> products)
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_productsFilePath, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing products file");
                }
            }
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            _logger.LogInformation("Getting all products");
            return await Task.FromResult(ReadProducts());
        }

        public async Task<Product> GetProductBySkuAsync(string sku)
        {
            _logger.LogInformation("Getting product with SKU: {Sku}", sku);
            var products = ReadProducts();
            var product = products.FirstOrDefault(p => p.Sku == sku);
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
            var products = ReadProducts();
            var existingProduct = products.FirstOrDefault(p => p.Sku == product.Sku);
            if (existingProduct != null)
            {
                _logger.LogInformation("Removing existing product with SKU: {Sku}", existingProduct.Sku);
                products.Remove(existingProduct);
            }
            products.Add(product);
            WriteProducts(products);
            _logger.LogInformation("Product saved successfully. Total products in list: {Count}", products.Count);
            return await Task.FromResult(product);
        }

        public async Task<IEnumerable<Product>> CrawlProductAsync(string url)
        {
            _logger.LogInformation("Crawling product from URL: {Url}", url);
            
            var product = await _crawlerService.CrawlProductAsync(url);
            await SaveProductAsync(product);

            return new List<Product> { product };
        }

        public async Task<Product> TransformProductAsync(string sku)
        {
            _logger.LogInformation("Transforming product with SKU: {Sku}", sku);
            var product = await GetProductBySkuAsync(sku);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with SKU {sku} not found");
            }

            // Transform the product
            product.Name = $"{product.Brand} {product.Color} Collection - {product.Category}";
            product.Description = $"Experience luxury and style with this {product.Color.ToLower()} {product.Category.ToLower()} from {product.Brand}'s latest collection. " +
                                 $"Crafted with premium materials and attention to detail, this piece combines fashion with functionality.";
            
            // Update attributes with English translations
            product.Attributes = product.Attributes.Select(attr => new ProductAttribute
            {
                Name = attr.Name switch
                {
                    "Material" => "Material",
                    "Style" => "Style",
                    "Gender" => "Gender",
                    _ => attr.Name
                },
                Value = attr.Value switch
                {
                    "Suni Deri" => "Synthetic Leather",
                    "Çapraz Askılı" => "Crossbody",
                    "Kadın" => "Women",
                    _ => attr.Value
                }
            }).ToList();

            // Update other fields
            product.Category = product.Category switch
            {
                "Çanta" => "Bag",
                _ => product.Category
            };
            
            product.ShippingInfo = "Fast Shipping Available";
            product.PaymentOptions = new List<string> { "Credit Card", "Bank Transfer" };
            product.StockStatus = "In Stock";

            // Save the transformed product
            await SaveProductAsync(product);

            return product;
        }
    }
}
