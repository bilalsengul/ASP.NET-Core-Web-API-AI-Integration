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
using System.Globalization;

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
                var colorVariants = new List<Product>();
                var variantSlider = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'styles-module_slider')]");
                
                if (variantSlider != null)
                {
                    var variantLinks = variantSlider.SelectNodes(".//a[contains(@class, 'styles-module_item')]");
                    if (variantLinks != null)
                    {
                        foreach (var link in variantLinks)
                        {
                            try
                            {
                                var variantUrl = link.GetAttributeValue("href", "");
                                var variantColor = link.SelectSingleNode(".//div[contains(@class, 'styles-module_title')]")?.InnerText.Trim();
                                var variantImage = link.SelectSingleNode(".//img")?.GetAttributeValue("src", "");
                                var isSelected = link.GetAttributeValue("class", "").Contains("selected");

                                if (!string.IsNullOrEmpty(variantUrl))
                                {
                                    // Convert relative URL to absolute
                                    if (!variantUrl.StartsWith("http"))
                                    {
                                        variantUrl = "https://www.trendyol.com" + variantUrl;
                                    }

                                    // Extract SKU from URL
                                    var variantSkuMatch = Regex.Match(variantUrl, @"p-(\d+)");
                                    var variantSku = variantSkuMatch.Success ? variantSkuMatch.Groups[1].Value : null;

                                    if (!string.IsNullOrEmpty(variantSku))
                                    {
                                        // Convert thumbnail URL to full-size image URL
                                        if (!string.IsNullOrEmpty(variantImage))
                                        {
                                            variantImage = variantImage.Replace("/mnresize/128/192/", "/mnresize/1200/1800/");
                                        }

                                        var variant = new Product
                                        {
                                            Sku = variantSku,
                                            ParentSku = baseSku,
                                            Color = variantColor,
                                            Images = new List<string> { variantImage },
                                            IsMainVariant = isSelected,
                                            Attributes = new List<ProductAttribute>()
                                        };

                                        if (!string.IsNullOrEmpty(variantColor))
                                        {
                                            variant.Attributes.Add(new ProductAttribute
                                            {
                                                Name = "color",
                                                Value = variantColor
                                            });
                                        }

                                        colorVariants.Add(variant);
                                        _logger.LogInformation("Added color variant: {color} with SKU: {sku}", variantColor, variantSku);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing color variant link");
                            }
                        }
                    }
                }

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
                // Extract basic product info
                var name = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']")?.InnerText.Trim();
                var brand = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']/a")?.InnerText.Trim();
                var category = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'breadcrumb')]/div[last()]")?.InnerText.Trim();

                // Extract all product images with improved selectors
                var images = new List<string>();
                var sliderImages = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-slide')]//img") ??
                                  doc.DocumentNode.SelectNodes("//div[contains(@class, 'gallery-modal-content')]//img") ??
                                  doc.DocumentNode.SelectNodes("//div[contains(@class, 'base-product-image')]//img");

                if (sliderImages != null)
                {
                    foreach (var img in sliderImages)
                    {
                        var src = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(src))
                        {
                            // Convert thumbnail URL to full-size image URL
                            src = src.Replace("/mnresize/128/192/", "/mnresize/1200/1800/")
                                     .Replace("/mnresize/50/75/", "/mnresize/1200/1800/");
                            if (!images.Contains(src))
                            {
                                images.Add(src);
                            }
                        }
                    }
                }

                // Extract color variants with improved selectors
                var colorVariants = new List<Product>();
                var variantSlider = doc.DocumentNode.SelectNodes("//div[contains(@class, 'slicing-attributes')]//div[contains(@class, 'sp-itm')]");
                
                if (variantSlider != null)
                {
                    foreach (var variant in variantSlider)
                    {
                        try
                        {
                            var variantUrl = variant.SelectSingleNode(".//a")?.GetAttributeValue("href", "");
                            var variantColor = variant.SelectSingleNode(".//div[contains(@class, 'variants')]//span")?.InnerText.Trim();
                            var variantImage = variant.SelectSingleNode(".//img")?.GetAttributeValue("src", "");
                            var isSelected = variant.GetAttributeValue("class", "").Contains("selected");

                            if (!string.IsNullOrEmpty(variantUrl))
                            {
                                // Convert relative URL to absolute
                                if (!variantUrl.StartsWith("http"))
                                {
                                    variantUrl = "https://www.trendyol.com" + variantUrl;
                                }

                                // Extract SKU from URL
                                var variantSkuMatch = Regex.Match(variantUrl, @"p-(\d+)");
                                var variantSku = variantSkuMatch.Success ? variantSkuMatch.Groups[1].Value : null;

                                if (!string.IsNullOrEmpty(variantSku))
                                {
                                    // Convert thumbnail URL to full-size image URL
                                    if (!string.IsNullOrEmpty(variantImage))
                                    {
                                        variantImage = variantImage.Replace("/mnresize/128/192/", "/mnresize/1200/1800/")
                                                                 .Replace("/mnresize/50/75/", "/mnresize/1200/1800/");
                                    }

                                    var variant = new Product
                                    {
                                        Sku = variantSku,
                                        ParentSku = parentSku ?? sku,
                                        Color = variantColor,
                                        Images = new List<string> { variantImage },
                                        IsMainVariant = isSelected,
                                        Attributes = new List<ProductAttribute>()
                                    };

                                    if (!string.IsNullOrEmpty(variantColor))
                                    {
                                        variant.Attributes.Add(new ProductAttribute
                                        {
                                            Name = "color",
                                            Value = variantColor
                                        });
                                    }

                                    colorVariants.Add(variant);
                                    _logger.LogInformation("Added color variant: {color} with SKU: {sku}", variantColor, variantSku);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing color variant");
                        }
                    }
                }

                // Extract size variants
                var sizeVariants = new List<Product>();
                var sizeNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'size-variant-wrapper')]//div[contains(@class, 'sp-itm')]");
                if (sizeNodes != null)
                {
                    foreach (var sizeNode in sizeNodes)
                    {
                        var size = sizeNode.InnerText.Trim();
                        var isAvailable = !sizeNode.GetAttributeValue("class", "").Contains("disabled");
                        var isSelected = sizeNode.GetAttributeValue("class", "").Contains("selected");
                        
                        if (isAvailable)
                        {
                            var sizeVariant = new Product
                            {
                                Sku = $"{sku}-{size.ToLower().Replace(" ", "-")}",
                                ParentSku = sku,
                                Size = size,
                                IsMainVariant = isSelected,
                                Attributes = new List<ProductAttribute>
                                {
                                    new ProductAttribute 
                                    { 
                                        Name = "size",
                                        Value = size 
                                    }
                                }
                            };
                            sizeVariants.Add(sizeVariant);
                        }
                    }
                }

                // Extract prices
                var priceNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'featured-prices')]//span[@class='prc-dsc']") ??
                               doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-price-container')]//span[@class='prc-dsc']");
                var originalPriceNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'featured-prices')]//span[@class='prc-org']") ??
                                       doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-price-container')]//span[@class='prc-org']");

                decimal discountedPrice = 0;
                decimal originalPrice = 0;

                if (priceNode != null)
                {
                    var priceText = priceNode.InnerText.Trim()
                        .Replace("TL", "")
                        .Replace(",", ".")
                        .Trim();
                    decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out discountedPrice);
                    discountedPrice *= 100; // Convert to cents
                }

                if (originalPriceNode != null)
                {
                    var priceText = originalPriceNode.InnerText.Trim()
                        .Replace("TL", "")
                        .Replace(",", ".")
                        .Trim();
                    decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out originalPrice);
                    originalPrice *= 100; // Convert to cents
                }
                else
                {
                    originalPrice = discountedPrice;
                }

                // Extract attributes and specifications
                var attributes = new List<ProductAttribute>();
                
                // Extract specifications from the product details table
                var specRows = doc.DocumentNode.SelectNodes("//table[contains(@class, 'detail-attr-table')]//tr") ??
                              doc.DocumentNode.SelectNodes("//div[contains(@class, 'detail-attr-container')]//tr");
                
                if (specRows != null)
                {
                    foreach (var row in specRows)
                    {
                        var label = row.SelectSingleNode(".//th")?.InnerText.Trim() ?? 
                                   row.SelectSingleNode(".//td[1]")?.InnerText.Trim();
                        var value = row.SelectSingleNode(".//td[last()]")?.InnerText.Trim();
                        
                        if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(value))
                        {
                            label = label.ToLower()
                                .Replace(" ", "_")
                                .Replace("ı", "i")
                                .Replace("ğ", "g")
                                .Replace("ü", "u")
                                .Replace("ş", "s")
                                .Replace("ö", "o")
                                .Replace("ç", "c");

                            attributes.Add(new ProductAttribute 
                            { 
                                Name = label,
                                Value = value 
                            });
                        }
                    }
                }

                // Extract features
                var features = new List<string>();
                var featureNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'detail-desc-list')]//li") ??
                                  doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-information-content')]//li") ??
                                  doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-feature-content')]//li");
                
                if (featureNodes != null)
                {
                    foreach (var node in featureNodes)
                    {
                        var featureText = node.InnerText.Trim()
                            .Replace("Detaylı bilgi için tıklayın.", "")
                            .Replace("\n", " ")
                            .Trim();
                        
                        if (!string.IsNullOrEmpty(featureText) && 
                            !featureText.Contains("tarafından gönderilecektir") &&
                            !featureText.Contains("TRENDYOL PAZARYERİ"))
                        {
                            features.Add(featureText);
                        }
                    }
                }

                // Get product description
                var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-information-content')]//p") ??
                                     doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'detail-desc-text')]");
                
                if (descriptionNode != null)
                {
                    var description = descriptionNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(description))
                    {
                        attributes.Add(new ProductAttribute
                        {
                            Name = "original_description",
                            Value = description
                        });
                    }
                }

                if (features.Any())
                {
                    attributes.Add(new ProductAttribute 
                    { 
                        Name = "features",
                        Value = string.Join("|", features)
                    });
                }

                // Extract review and favorite counts
                var reviewCountNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'pr-rnr-cn')]//span");
                if (reviewCountNode != null)
                {
                    var reviewCount = reviewCountNode.InnerText.Trim().Replace("(", "").Replace(")", "");
                    attributes.Add(new ProductAttribute
                    {
                        Name = "review_count",
                        Value = reviewCount
                    });
                }

                var favoriteCountNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'fv-dt')]//span");
                if (favoriteCountNode != null)
                {
                    var favoriteCount = favoriteCountNode.InnerText.Trim();
                    attributes.Add(new ProductAttribute
                    {
                        Name = "favorite_count",
                        Value = favoriteCount
                    });
                }

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(brand))
                {
                    _logger.LogWarning("Required product information missing for SKU: {sku}", sku);
                    return null;
                }

                var mainProduct = new Product
                {
                    Name = name,
                    Description = null, // Will be added by AI transformation
                    Sku = sku,
                    ParentSku = parentSku,
                    Brand = brand,
                    Category = category,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = discountedPrice,
                    Images = images,
                    Attributes = attributes,
                    Variants = colorVariants.Concat(sizeVariants).ToList(),
                    IsMainVariant = true
                };

                // Add current color if available
                var selectedColor = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'slc-title')]//h2/span")?.InnerText.Trim() ??
                                  doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'selected-variant-text')]")?.InnerText.Trim();
                if (!string.IsNullOrEmpty(selectedColor))
                {
                    mainProduct.Color = selectedColor;
                    AddColorAttribute(mainProduct, selectedColor);
                }

                // Add current size if available
                var selectedSize = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'size-variant-wrapper')]//div[contains(@class, 'sp-itm selected')]")?.InnerText.Trim();
                if (!string.IsNullOrEmpty(selectedSize))
                {
                    mainProduct.Size = selectedSize;
                    mainProduct.Attributes.Add(new ProductAttribute 
                    { 
                        Name = "size",
                        Value = selectedSize 
                    });
                }

                return mainProduct;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product details for SKU: {Sku}", sku);
                return null;
            }
        }

        private void AddColorAttribute(Product product, string? color)
        {
            if (!string.IsNullOrEmpty(color))
            {
                product.Attributes.Add(new ProductAttribute 
                { 
                    Name = "color",
                    Value = color 
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
                                Name = "size",
                                Value = size 
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
                chat.AppendSystemMessage(@"You are a professional product description writer specializing in fashion and accessories. 
Your task is to create detailed, engaging product descriptions that highlight:
1. Key features and materials
2. Design elements and style
3. Functionality and use cases
4. Target audience
5. Unique selling points
6. Color and aesthetic appeal
7. Quality and craftsmanship

Ensure descriptions are informative, engaging, and help customers visualize the product.");

                // Prepare a detailed context for the AI
                var productContext = $@"Product Details:
Name: {product.Name}
Brand: {product.Brand}
Category: {product.Category ?? "Fashion Accessory"}
Color: {product.Color ?? "Not specified"}
Features: {(product.Attributes.FirstOrDefault(a => a.Name == "features")?.Value ?? "Not specified")}
Images Count: {product.Images.Count}
Price Range: {(product.DiscountedPrice != product.OriginalPrice ? "Discounted" : "Regular")}

Please transform this into English and create a detailed, marketing-friendly description that covers all aspects of the product.";

                chat.AppendUserInput($@"Transform the following product information into a comprehensive English description:

{productContext}

Please provide the response in the following JSON format:
{{
    ""name"": ""professional product name"",
    ""description"": ""detailed, multi-paragraph description that covers materials, design, features, and benefits"",
    ""brand"": ""brand name in English"",
    ""score"": rating between 0-100 based on content quality and completeness
}}");

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
                    Score = ExtractScore(response),
                    Color = product.Color,
                    Size = product.Size,
                    VariantId = product.VariantId,
                    IsMainVariant = product.IsMainVariant,
                    Variants = product.Variants
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