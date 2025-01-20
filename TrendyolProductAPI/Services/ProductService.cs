using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using TrendyolProductAPI.Models;
using OpenAI_API;
using Microsoft.Extensions.Configuration;

namespace TrendyolProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ILogger<ProductService> _logger;
        private readonly string _productsFilePath;
        private static readonly object _lock = new object();
        private readonly IProductCrawlerService _crawlerService;
        private readonly OpenAIAPI _openAI;

        public ProductService(ILogger<ProductService> logger, IProductCrawlerService crawlerService, IConfiguration configuration)
        {
            _logger = logger;
            _crawlerService = crawlerService;
            
            // Initialize OpenAI
            var openAiKey = configuration["OpenAI:ApiKey"];
            _openAI = new OpenAIAPI(openAiKey);
            
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

                // Basic transformation to ensure essential data is present
                ApplyBasicTransformation(product);

                // Try to generate AI description, with fallback to basic description
                try
                {
                    var aiDescription = await GenerateAIDescriptionAsync(product);
                    if (!string.IsNullOrWhiteSpace(aiDescription))
                    {
                        product.Description = aiDescription;
                    }
                    else
                    {
                        _logger.LogWarning("AI description generation returned empty result for SKU {Sku}, using fallback description", sku);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating AI description for SKU {Sku}, using fallback description", sku);
                }

                // Ensure description is not empty by using fallback if needed
                if (string.IsNullOrWhiteSpace(product.Description))
                {
                    GenerateFallbackDescription(product);
                }

                // Common updates
                ApplyCommonUpdates(product);

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

        private void ApplyBasicTransformation(Product product)
        {
            // Ensure basic product information is present
            product.Name = string.IsNullOrWhiteSpace(product.Name) 
                ? $"{product.Brand ?? "Premium Brand"} {product.Category ?? "Product"}"
                : product.Name;

            product.Brand ??= "Premium Brand";
            product.Category ??= "General";
            product.Color ??= "Standard";

            // Add or update standard attributes
            var standardAttributes = new List<(string Name, string Value)>
            {
                ("Material", "Premium Material"),
                ("Style", "Modern Design"),
                ("Gender", "Unisex"),
                ("Dimensions", "Standard Size"),
                ("Features", "Multiple Features"),
                ("Care Instructions", "Standard Care")
            };

            foreach (var (name, defaultValue) in standardAttributes)
            {
                var existingAttr = product.Attributes.FirstOrDefault(a => a.Name == name);
                if (existingAttr != null)
                {
                    if (string.IsNullOrWhiteSpace(existingAttr.Value))
                    {
                        existingAttr.Value = defaultValue;
                    }
                }
                else
                {
                    product.Attributes.Add(new ProductAttribute { Name = name, Value = defaultValue });
                }
            }
        }

        private async Task<string> GenerateAIDescriptionAsync(Product product)
        {
            var prompt = $"Write a detailed, engaging product description for an e-commerce website. Product details:\n" +
                        $"Name: {product.Name}\n" +
                        $"Brand: {product.Brand}\n" +
                        $"Category: {product.Category}\n" +
                        $"Color: {product.Color}\n" +
                        $"Price: ${product.DiscountedPrice}\n" +
                        $"Features: {string.Join(", ", product.Attributes.Select(a => $"{a.Name}: {a.Value}"))}\n\n" +
                        "The description should be professional, highlight key features, and be around 200 words. Focus on benefits and unique selling points.";

            var completionRequest = _openAI.Chat.CreateConversation();
            completionRequest.AppendSystemMessage("You are a professional e-commerce product description writer. Create engaging, detailed, and accurate product descriptions that highlight key features and benefits.");
            completionRequest.AppendUserInput(prompt);

            var response = await completionRequest.GetResponseFromChatbotAsync();
            return response?.Trim();
        }

        private void GenerateFallbackDescription(Product product)
        {
            var features = string.Join(", ", product.Attributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Value))
                .Select(a => $"{a.Name}: {a.Value}"));

            var priceText = product.DiscountedPrice > 0 
                ? $" available at ${product.DiscountedPrice}"
                : "";

            product.Description = $"Discover the exceptional quality of this {product.Color?.ToLower() ?? "premium"} " +
                                $"{product.Category?.ToLower() ?? "product"} from {product.Brand ?? "our premium collection"}" +
                                $"{priceText}. This product offers {features}. " +
                                "Crafted with attention to detail and designed for optimal performance, " +
                                "this item combines style with functionality to meet your needs.";
        }

        private void ApplyCommonUpdates(Product product)
        {
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
        }
    }
} 
