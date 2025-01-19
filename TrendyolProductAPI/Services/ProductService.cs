using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Chat;
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
                var products = new List<Product>();

                // Extract base SKU from URL
                var skuMatch = Regex.Match(productUrl, @"p-(\d+)");
                var baseSku = skuMatch.Success ? skuMatch.Groups[1].Value : null;

                if (string.IsNullOrEmpty(baseSku))
                {
                    throw new Exception("Could not extract SKU from URL");
                }

                _logger.LogInformation("Processing product with base SKU: {sku}", baseSku);

                // Extract main product details first
                var mainProduct = await ExtractProductDetails(doc, baseSku, null);
                if (mainProduct == null)
                {
                    throw new Exception("Could not extract main product details");
                }

                // Get all variant selectors
                var colorSelector = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'color-selector')]");
                var sizeSelector = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'size-selector')]");
                
                // Get all color variants
                var colorVariants = doc.DocumentNode.SelectNodes("//div[contains(@class, 'slicing-attribute')]//div[contains(@class, 'sp-itm')]");
                
                // Get all size variants
                var sizeVariants = doc.DocumentNode.SelectNodes("//div[contains(@class, 'size-variant-wrapper')]//div[contains(@class, 'variants')]//a");

                _logger.LogInformation("Found variants - Colors: {colorCount}, Sizes: {sizeCount}", 
                    colorVariants?.Count ?? 0, 
                    sizeVariants?.Count ?? 0);

                if (colorVariants != null && colorVariants.Any())
                {
                    foreach (var colorVariant in colorVariants)
                    {
                        try
                        {
                            var variantUrl = colorVariant.GetAttributeValue("href", "");
                            var variantColor = colorVariant.SelectSingleNode(".//span")?.InnerText.Trim();
                            var isSelected = colorVariant.GetAttributeValue("class", "").Contains("selected");

                            if (string.IsNullOrEmpty(variantUrl) && isSelected)
                            {
                                // This is the current color variant
                                var currentProduct = mainProduct.Clone() as Product;
                                if (currentProduct != null)
                                {
                                    AddColorAttribute(currentProduct, variantColor);
                                    await ProcessSizeVariants(currentProduct, sizeVariants, products);
                                }
                            }
                            else if (!string.IsNullOrEmpty(variantUrl))
                            {
                                // Need to fetch the variant page
                                if (!variantUrl.StartsWith("http"))
                                {
                                    variantUrl = "https://www.trendyol.com" + variantUrl;
                                }

                                var variantDoc = await web.LoadFromWebAsync(variantUrl);
                                var variantSkuMatch = Regex.Match(variantUrl, @"p-(\d+)");
                                var variantSku = variantSkuMatch.Success ? variantSkuMatch.Groups[1].Value : null;

                                if (!string.IsNullOrEmpty(variantSku))
                                {
                                    var variantProduct = await ExtractProductDetails(variantDoc, variantSku, baseSku);
                                    if (variantProduct != null)
                                    {
                                        AddColorAttribute(variantProduct, variantColor);
                                        await ProcessSizeVariants(variantProduct, 
                                            variantDoc.DocumentNode.SelectNodes("//div[contains(@class, 'size-variant-wrapper')]//div[contains(@class, 'variants')]//a"), 
                                            products);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing color variant");
                        }
                    }
                }
                else
                {
                    // No color variants, check for size variants only
                    await ProcessSizeVariants(mainProduct, sizeVariants, products);
                }

                // If no variants were processed, add the main product
                if (!products.Any())
                {
                    products.Add(mainProduct);
                }

                // Cache all products
                foreach (var product in products)
                {
                    var cacheKey = $"{CACHE_KEY_PREFIX}{product.Sku}";
                    _cache.Set(cacheKey, product);
                    _cache.AddKey(cacheKey);
                }

                _logger.LogInformation("Successfully crawled {count} products for URL: {url}", products.Count, productUrl);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {Url}", productUrl);
                throw;
            }
        }

        private async Task<Product?> ExtractProductDetails(HtmlDocument doc, string sku, string? parentSku)
        {
            try
            {
                // Extract product information using HTML nodes
                var name = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']")?.InnerText.Trim();
                var brand = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']/a")?.InnerText.Trim();
                var description = doc.DocumentNode.SelectSingleNode("//div[@class='product-description']")?.InnerText.Trim();
                
                // Extract color/variant information
                var selectedVariant = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'selected')]//span")?.InnerText.Trim();
                
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

                // Extract attributes (including color/size variants)
                var attributes = new List<ProductAttribute>();
                
                // Add color/variant as an attribute if available
                if (!string.IsNullOrEmpty(selectedVariant))
                {
                    attributes.Add(new ProductAttribute 
                    { 
                        Key = "color",
                        Name = selectedVariant 
                    });
                }

                // Extract other attributes
                var attributeNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'prop-item')]");
                if (attributeNodes != null)
                {
                    foreach (var attrNode in attributeNodes)
                    {
                        var key = attrNode.SelectSingleNode(".//span[@class='prop-key']")?.InnerText.Trim();
                        var value = attrNode.SelectSingleNode(".//span[@class='prop-value']")?.InnerText.Trim();
                        
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            attributes.Add(new ProductAttribute 
                            { 
                                Key = key.ToLowerInvariant(),
                                Name = value 
                            });
                        }
                    }
                }

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(brand))
                {
                    _logger.LogWarning("Required product information missing for SKU: {sku}", sku);
                    return null;
                }

                return new Product
                {
                    Name = name,
                    Description = description,
                    Sku = sku,
                    ParentSku = parentSku,
                    Brand = brand,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = discountedPrice,
                    Images = images,
                    Attributes = attributes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product details for SKU: {sku}", sku);
                return null;
            }
        }

        private void AddColorAttribute(Product product, string? color)
        {
            if (!string.IsNullOrEmpty(color))
            {
                product.Attributes.Add(new ProductAttribute 
                { 
                    Key = "color",
                    Name = color 
                });
            }
        }

        private async Task ProcessSizeVariants(Product baseProduct, HtmlNodeCollection? sizeVariants, List<Product> products)
        {
            if (sizeVariants == null || !sizeVariants.Any())
            {
                products.Add(baseProduct);
                return;
            }

            foreach (var sizeVariant in sizeVariants)
            {
                try
                {
                    var size = sizeVariant.InnerText.Trim();
                    var isAvailable = !sizeVariant.GetAttributeValue("class", "").Contains("disabled");
                    
                    if (isAvailable)
                    {
                        var sizeVariantProduct = baseProduct.Clone() as Product;
                        if (sizeVariantProduct != null)
                        {
                            // Generate a unique SKU for the size variant
                            sizeVariantProduct.Sku = $"{baseProduct.Sku}-{size.ToLower().Replace(" ", "-")}";
                            sizeVariantProduct.ParentSku = baseProduct.Sku;
                            
                            sizeVariantProduct.Attributes.Add(new ProductAttribute 
                            { 
                                Key = "size",
                                Name = size 
                            });
                            
                            products.Add(sizeVariantProduct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing size variant");
                }
            }
        }

        public async Task<Product?> TransformProductAsync(string sku)
        {
            try
            {
                var product = await GetProductBySkuAsync(sku);
                if (product == null)
                {
                    _logger.LogWarning("Product not found with SKU: {sku}", sku);
                    return null;
                }

                _logger.LogInformation("Starting product transformation for SKU: {sku}", sku);

                _logger.LogInformation("Sending request to OpenAI");
                
                // Create a new conversation
                var chat = _openAI.Chat.CreateConversation();
                
                // Add the messages
                chat.AppendSystemMessage("You are a product translation assistant. Convert product information to English and provide output in JSON format.");
                chat.AppendUserInput($"Translate and enhance the following product information to English and assign a score between 0-100 based on content quality:\n\n" +
                                   $"Name: {product.Name}\n" +
                                   $"Description: {product.Description}\n" +
                                   $"Brand: {product.Brand}\n" +
                                   $"Number of Images: {product.Images.Count}\n\n" +
                                   "Please provide the response in the following JSON format:\n" +
                                   "{\n" +
                                   "  \"name\": \"translated name\",\n" +
                                   "  \"description\": \"translated description\",\n" +
                                   "  \"brand\": \"translated brand\",\n" +
                                   "  \"score\": 85\n" +
                                   "}");

                // Get the response
                var response = await chat.GetResponseFromChatbotAsync();
                _logger.LogInformation("Received OpenAI response: {response}", response);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogError("Empty response received from OpenAI");
                    throw new Exception("Empty response from OpenAI");
                }

                // Parse the response and update the product
                var transformedProduct = new Product
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

                if (string.IsNullOrEmpty(transformedProduct.Name) || 
                    string.IsNullOrEmpty(transformedProduct.Description) || 
                    string.IsNullOrEmpty(transformedProduct.Brand))
                {
                    _logger.LogError("Failed to extract required fields from OpenAI response");
                    throw new Exception("Failed to parse OpenAI response");
                }

                // Store the transformed product in cache
                _cache.Set($"{CACHE_KEY_PREFIX}{sku}", transformedProduct);

                _logger.LogInformation("Successfully transformed product with SKU: {sku}", sku);
                return transformedProduct;
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
            
            try
            {
                _logger.LogInformation("Retrieving all saved products");
                
                // Get all cache keys that start with the product prefix
                var cacheKeys = _cache.GetKeys<string>().Where(k => k.StartsWith(CACHE_KEY_PREFIX));
                _logger.LogInformation("Found {count} cache keys", cacheKeys.Count());
                
                foreach (var key in cacheKeys)
                {
                    if (_cache.TryGetValue(key, out Product? product) && product != null)
                    {
                        _logger.LogInformation("Retrieved product with SKU: {sku}, IsSaved: {isSaved}", product.Sku, product.IsSaved);
                        if (product.IsSaved)
                        {
                            products.Add(product);
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {count} saved products", products.Count);
                return products.OrderByDescending(p => p.Score).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw;
            }
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            await Task.CompletedTask;
            if (_cache.TryGetValue($"{CACHE_KEY_PREFIX}{sku}", out Product? product))
            {
                return product;
            }
            return null;
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

        public async Task<Product> SaveProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation("Saving product with SKU: {sku}", product.Sku);
                
                // Add a flag to indicate this is a saved product
                product.IsSaved = true;
                
                // Store in cache with a longer expiration time for saved products
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromDays(30))
                    .SetPriority(CacheItemPriority.NeverRemove);
                
                var cacheKey = $"{CACHE_KEY_PREFIX}{product.Sku}";
                _cache.Set(cacheKey, product, cacheEntryOptions);
                _cache.AddKey(cacheKey);
                
                // Verify the product was saved
                if (_cache.TryGetValue(cacheKey, out Product? savedProduct) && savedProduct != null)
                {
                    _logger.LogInformation("Successfully saved product with SKU: {sku}, IsSaved: {isSaved}", 
                        savedProduct.Sku, savedProduct.IsSaved);
                    return savedProduct;
                }
                
                throw new Exception($"Failed to verify saved product with SKU: {product.Sku}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product with SKU: {sku}", product.Sku);
                throw;
            }
        }
    }
} 