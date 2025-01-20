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

            try
            {
                // Initialize lists if they are null
                product.Attributes ??= new List<ProductAttribute>();
                product.PaymentOptions ??= new List<string>();
                product.Images ??= new List<string>();
                product.Variants ??= new List<Product>();

                // Check if this is the first transformation or AI description enhancement
                bool isFirstTransform = string.IsNullOrEmpty(product.Description) || !product.Description.Contains("masterfully crafted");

                if (isFirstTransform)
                {
                    // Basic transformation
                    var colorText = product.Color?.ToLower() ?? "elegant";
                    var categoryText = product.Category?.ToLower() ?? "product";
                    var brandText = product.Brand ?? "Premium Brand";
                    var priceText = product.DiscountedPrice > 0 ? $" at an attractive price of {product.DiscountedPrice:C}" : "";

                    // Generate a detailed product name
                    product.Name = $"{brandText} {colorText} Collection - Premium {categoryText}";

                    // Basic description
                    product.Description = $"Experience luxury and style with this {colorText} {categoryText} from {brandText}'s latest collection{priceText}. " +
                                        "This piece combines fashion with functionality.";

                    // Add standard attributes
                    var standardAttributes = new List<(string Name, string Value)>
                    {
                        ("Material", "Premium Synthetic Leather"),
                        ("Style", "Modern Crossbody"),
                        ("Gender", "Women"),
                        ("Dimensions", "27x27x14 cm"),
                        ("Features", "Adjustable Strap, Multiple Compartments"),
                        ("Care Instructions", "Wipe with damp cloth")
                    };

                    foreach (var (name, value) in standardAttributes)
                    {
                        var existingAttr = product.Attributes.FirstOrDefault(a => a.Name == name);
                        if (existingAttr != null)
                        {
                            existingAttr.Value = value;
                        }
                        else
                        {
                            product.Attributes.Add(new ProductAttribute { Name = name, Value = value });
                        }
                    }
                }
                else
                {
                    // Enhanced AI description
                    var descriptionBuilder = new System.Text.StringBuilder();
                    var material = product.Attributes.FirstOrDefault(a => a.Name == "Material")?.Value ?? "Premium Synthetic Leather";
                    var features = product.Attributes.FirstOrDefault(a => a.Name == "Features")?.Value ?? "multiple features";
                    var dimensions = product.Attributes.FirstOrDefault(a => a.Name == "Dimensions")?.Value ?? "spacious dimensions";
                    var style = product.Attributes.FirstOrDefault(a => a.Name == "Style")?.Value ?? "Modern";
                    var care = product.Attributes.FirstOrDefault(a => a.Name == "Care Instructions")?.Value ?? "gentle care";

                    descriptionBuilder.AppendLine($"Discover the epitome of style with this exquisite {product.Name}. ");
                    descriptionBuilder.AppendLine($"This masterfully crafted piece showcases the perfect blend of contemporary design and practical functionality. ");
                    descriptionBuilder.AppendLine($"Expertly constructed using {material}, this product exemplifies durability and sophistication. ");
                    descriptionBuilder.AppendLine($"Featuring {features}, this versatile accessory adapts seamlessly to your daily needs. ");
                    descriptionBuilder.AppendLine($"With its {dimensions}, it offers ample space while maintaining a sleek profile. ");
                    descriptionBuilder.AppendLine($"The {style} design makes it a perfect companion for both casual outings and formal occasions. ");
                    descriptionBuilder.AppendLine($"\nCare & Maintenance: {care} to maintain its pristine condition. ");
                    descriptionBuilder.AppendLine($"\nEnjoy the convenience of fast shipping and experience the luxury of {product.Brand} at your doorstep. ");

                    product.Description = descriptionBuilder.ToString();

                    // Update features with more details
                    var existingFeatures = product.Attributes.FirstOrDefault(a => a.Name == "Features");
                    if (existingFeatures != null)
                    {
                        existingFeatures.Value += ", Premium Hardware, Interior Pockets";
                    }

                    // Update care instructions with more details
                    var existingCare = product.Attributes.FirstOrDefault(a => a.Name == "Care Instructions");
                    if (existingCare != null)
                    {
                        existingCare.Value += ", Store in dust bag, Avoid direct sunlight";
                    }
                }

                // Common updates for both stages
                product.ShippingInfo = "Fast Shipping Available - Delivery in 2-3 Business Days";
                product.HasFastShipping = true;
                product.PaymentOptions = new List<string>
                {
                    "Credit Card - Up to 12 installments",
                    "Bank Transfer",
                    "Mobile Payment",
                    "Digital Wallet"
                };

                product.StockStatus = "In Stock";
                product.RatingCount = Math.Max(product.RatingCount, 10);
                product.FavoriteCount = Math.Max(product.FavoriteCount, 50);
                product.Score = product.Score ?? 4.5m;

                // Save the transformed product
                await SaveProductAsync(product);
                _logger.LogInformation("Product transformed successfully: {Sku}", sku);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU {Sku}", sku);
                throw;
            }
        }
    }
} 
