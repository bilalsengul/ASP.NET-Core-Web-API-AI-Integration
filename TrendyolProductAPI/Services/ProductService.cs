using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using TrendyolProductAPI.Models;
using TrendyolProductAPI.Extensions;
using HtmlAgilityPack;
using System.Linq;
using System.Text.RegularExpressions;

namespace TrendyolProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductService> _logger;
        private readonly HttpClient _httpClient;
        private readonly OpenAIAPI _openAI;
        private const string CACHE_KEY_PREFIX = "product_";

        public ProductService(
            IMemoryCache cache,
            ILogger<ProductService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _cache = cache;
            _logger = logger;
            _httpClient = httpClient;
            _openAI = new OpenAIAPI(new APIAuthentication(configuration["OpenAI:ApiKey"]));
        }

        public async Task<List<Product>> CrawlProductAsync(string productUrl)
        {
            try
            {
                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync(productUrl);

                // Extract product information using HTML nodes
                var name = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']")?.InnerText.Trim();
                var brand = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']/a")?.InnerText.Trim();
                var description = doc.DocumentNode.SelectSingleNode("//div[@class='product-description']")?.InnerText.Trim();
                
                // Extract SKU from URL
                var skuMatch = Regex.Match(productUrl, @"p-(\d+)");
                var sku = skuMatch.Success ? skuMatch.Groups[1].Value : null;

                // Extract prices
                var priceNode = doc.DocumentNode.SelectSingleNode("//span[@class='prc-dsc']");
                var originalPriceNode = doc.DocumentNode.SelectSingleNode("//span[@class='prc-org']");

                decimal discountedPrice = 0;
                decimal originalPrice = 0;

                if (priceNode != null)
                {
                    decimal.TryParse(priceNode.InnerText.Replace("TL", "").Trim(), out discountedPrice);
                }

                if (originalPriceNode != null)
                {
                    decimal.TryParse(originalPriceNode.InnerText.Replace("TL", "").Trim(), out originalPrice);
                }
                else
                {
                    originalPrice = discountedPrice;
                }

                // Extract images
                var images = doc.DocumentNode
                    .SelectNodes("//img[@class='product-image']")?
                    .Select(img => img.GetAttributeValue("src", ""))
                    .Where(src => !string.IsNullOrEmpty(src))
                    .ToList() ?? new List<string>();

                var product = new Product
                {
                    Name = name,
                    Description = description,
                    Sku = sku,
                    Brand = brand,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = discountedPrice,
                    Images = images,
                    Attributes = new List<ProductAttribute>() // You might want to extract attributes as well
                };

                // Cache the product
                _cache.Set($"{CACHE_KEY_PREFIX}{sku}", product);

                return new List<Product> { product };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {Url}", productUrl);
                throw;
            }
        }

        public async Task<Product?> TransformProductAsync(string sku)
        {
            var product = await GetProductBySkuAsync(sku);
            if (product == null)
                return null;

            try
            {
                // Prepare the prompt for OpenAI
                var prompt = $"Translate and enhance the following product information to English and assign a score between 0-100 based on content quality:\n\n" +
                           $"Name: {product.Name}\n" +
                           $"Description: {product.Description}\n" +
                           $"Brand: {product.Brand}\n" +
                           $"Number of Images: {product.Images.Count}\n\n" +
                           "Please provide:\n" +
                           "1. Translated name (make it SEO-friendly)\n" +
                           "2. Translated description (make it detailed and SEO-friendly)\n" +
                           "3. Translated brand\n" +
                           "4. Score (0-100) based on name clarity, description detail, image count, and overall content quality\n" +
                           "Format: JSON";

                var completionRequest = new OpenAI_API.Completions.CompletionRequest
                {
                    Prompt = prompt,
                    Model = "gpt-4-turbo-preview",
                    MaxTokens = 1000
                };

                var result = await _openAI.Completions.CreateCompletionAsync(completionRequest);
                if (result?.Completions == null || result.Completions.Count == 0)
                    return null;

                var response = result.Completions[0].Text;

                // Parse the response and update the product
                return new Product
                {
                    Name = ExtractValue(response, "name"),
                    Description = ExtractValue(response, "description"),
                    Brand = ExtractValue(response, "brand"),
                    Sku = product.Sku,
                    ParentSku = product.ParentSku,
                    Attributes = product.Attributes,
                    Category = product.Category,
                    OriginalPrice = product.OriginalPrice,
                    DiscountedPrice = product.DiscountedPrice,
                    Images = product.Images,
                    Score = ExtractScore(response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU: {Sku}", sku);
                throw;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            await Task.CompletedTask;
            var products = new List<Product>();
            var cacheKeys = _cache.GetKeys<string>().Where(k => k.StartsWith(CACHE_KEY_PREFIX));
            
            foreach (var key in cacheKeys)
            {
                if (_cache.TryGetValue(key, out Product? product) && product != null)
                {
                    products.Add(product);
                }
            }
            
            return products;
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            await Task.CompletedTask;
            _cache.TryGetValue($"{CACHE_KEY_PREFIX}{sku}", out Product? product);
            return product;
        }

        private string ExtractValue(string response, string key)
        {
            if (string.IsNullOrEmpty(response) || string.IsNullOrEmpty(key))
                return string.Empty;

            var match = Regex.Match(response, $@"""{key}""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private decimal ExtractScore(string response)
        {
            var match = Regex.Match(response, @"""score""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            return match.Success && decimal.TryParse(match.Groups[1].Value, out decimal score) ? score : 0;
        }
    }
} 