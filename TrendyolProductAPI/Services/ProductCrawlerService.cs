using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Microsoft.Extensions.Logging;
using TrendyolProductAPI.Models;
using System.Text.RegularExpressions;
using System.Linq;
using OpenQA.Selenium.Interactions;

namespace TrendyolProductAPI.Services
{
    public interface IProductCrawlerService
    {
        Task<Product> CrawlProductAsync(string url);
    }

    public class ProductCrawlerService : IProductCrawlerService, IDisposable
    {
        private readonly ILogger<ProductCrawlerService> _logger;
        private readonly IWebDriver _driver;
        private bool _disposed;

        public ProductCrawlerService(ILogger<ProductCrawlerService> logger)
        {
            _logger = logger;
            
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            
            _driver = new ChromeDriver(options);
        }

        public async Task<Product> CrawlProductAsync(string url)
        {
            try
            {
                _logger.LogInformation("Starting to crawl product from URL: {Url}", url);

                _driver.Navigate().GoToUrl(url);
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                
                // Wait for critical elements
                wait.Until(d => d.FindElement(By.CssSelector("h1.pr-new-br")));
                wait.Until(d => d.FindElement(By.CssSelector("div.product-price-container")));
                
                // Scroll to load all content
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(1000); // Wait for dynamic content

                var pageSource = _driver.PageSource;
                var doc = new HtmlDocument();
                doc.LoadHtml(pageSource);

                var sku = ExtractSkuFromUrl(url);
                var productName = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']")?.InnerText.Trim();
                var brand = doc.DocumentNode.SelectSingleNode("//h1[@class='pr-new-br']/a")?.InnerText.Trim();
                var priceText = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-price-container')]//span[contains(@class, 'prc-dsc')]")?.InnerText.Trim();
                var originalPriceText = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-price-container')]//span[contains(@class, 'prc-org')]")?.InnerText.Trim();
                
                var description = GetFullDescription(doc);
                var images = GetAllProductImages(doc);
                var attributes = GetDetailedAttributes(doc);

                var categoryPath = doc.DocumentNode.SelectNodes("//div[contains(@class,'product-navigation')]//a")?
                    .Select(node => node.InnerText.Trim())
                    .Where(text => !string.IsNullOrEmpty(text))
                    .ToList();
                var category = categoryPath != null ? string.Join(" > ", categoryPath) : null;

                decimal.TryParse(priceText?.Replace("TL", "").Replace(".", "").Replace(",", ".").Trim(), out decimal discountedPrice);
                decimal.TryParse(originalPriceText?.Replace("TL", "").Replace(".", "").Replace(",", ".").Trim(), out decimal originalPrice);

                var product = new Product
                {
                    Sku = sku,
                    Name = productName,
                    Brand = brand,
                    Description = description,
                    Category = category,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = discountedPrice,
                    Images = images,
                    Attributes = attributes,
                    Score = await GetProductRatingAsync(doc),
                    ShippingInfo = GetShippingInfo(doc),
                    PaymentOptions = GetPaymentOptions(doc),
                    StockStatus = GetStockStatus(doc),
                    SellerName = GetSellerName(doc)
                };

                try
                {
                    // Wait for variants to load
                    var variantContainer = _driver.FindElements(By.CssSelector("div.slicing-attributes"));
                    if (variantContainer.Any())
                    {
                        product.Variants = await GetProductVariantsAsync(variantContainer);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting product variants");
                }

                product.IsMainVariant = true;
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {Url}", url);
                throw;
            }
        }

        private string GetFullDescription(HtmlDocument doc)
        {
            var description = new List<string>();
            
            // Get main description from featured information
            var mainDesc = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'featured-information')]//ul[@id='content-descriptions-list']")?.InnerText.Trim();
            if (!string.IsNullOrEmpty(mainDesc))
            {
                description.Add(mainDesc);
            }

            // Get additional description sections
            var descSections = doc.DocumentNode.SelectNodes("//div[contains(@class, 'content-descriptions')]//li");
            if (descSections != null)
            {
                foreach (var section in descSections)
                {
                    var text = section.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        description.Add(text);
                    }
                }
            }

            return string.Join("\n\n", description);
        }

        private List<string> GetAllProductImages(HtmlDocument doc)
        {
            var images = new List<string>();
            
            // Get all image elements from the gallery
            var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'styles-module_slider__o0fqa')]//img") ??
                            doc.DocumentNode.SelectNodes("//div[contains(@class, 'gallery-modal')]//img");
            
            if (imageNodes != null)
            {
                foreach (var img in imageNodes)
                {
                    var src = img.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        // Use the original image URL without transformation
                        if (!images.Contains(src))
                        {
                            images.Add(src);
                        }
                    }
                }
            }

            return images.Distinct().ToList();
        }

        private List<ProductAttribute> GetDetailedAttributes(HtmlDocument doc)
        {
            var attributes = new List<ProductAttribute>();
            
            // Get attributes from the slicing attributes section
            var slicingAttributes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'slicing-attributes')]//section");
            if (slicingAttributes != null)
            {
                foreach (var section in slicingAttributes)
                {
                    var attributeName = section.SelectSingleNode(".//div[contains(@class, 'slc-title')]//h2")?.InnerText.Trim();
                    var attributeValue = section.SelectSingleNode(".//div[contains(@class, 'selected')]")?.InnerText.Trim();
                    
                    if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(attributeValue))
                    {
                        attributeName = attributeName.Replace(":", "").Trim();
                        attributes.Add(new ProductAttribute { Name = attributeName, Value = attributeValue });
                    }
                }
            }

            return attributes;
        }

        private string ExtractSkuFromUrl(string url)
        {
            var match = Regex.Match(url, @"p-(\d+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private async Task<decimal> GetProductRatingAsync(HtmlDocument doc)
        {
            try
            {
                var ratingText = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product-rating-score')]//div[@class='value']")?.InnerText.Trim();
                if (decimal.TryParse(ratingText?.Replace(",", "."), out decimal rating))
                {
                    return rating;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product rating");
            }
            return 0;
        }

        private string GetShippingInfo(HtmlDocument doc)
        {
            try
            {
                return doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'same-day-shipping')]//div")?.InnerText.Trim() ??
                       doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'delivery-info')]//span[@class='info-text']")?.InnerText.Trim() ??
                       "24 saatte kargoda";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping info");
                return "24 saatte kargoda";
            }
        }

        private List<string> GetPaymentOptions(HtmlDocument doc)
        {
            var options = new List<string> { "Kredi Kartı", "Havale/EFT" };
            try
            {
                var paymentNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'payment-options-content')]//span[@class='banner-content']");
                if (paymentNodes != null)
                {
                    options.Clear();
                    foreach (var node in paymentNodes)
                    {
                        var option = node.InnerText.Trim();
                        if (!string.IsNullOrEmpty(option))
                        {
                            options.Add(option);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment options");
            }
            return options;
        }

        private string GetStockStatus(HtmlDocument doc)
        {
            try
            {
                var stockStatus = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'pr-in-stock')]//span")?.InnerText.Trim();
                return !string.IsNullOrEmpty(stockStatus) ? stockStatus : "Stokta";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock status");
                return "Stokta";
            }
        }

        private string GetSellerName(HtmlDocument doc)
        {
            try
            {
                return doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'seller-name-text')]")?.InnerText.Trim() ??
                       doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'seller-name-text')]")?.InnerText.Trim() ??
                       "TrendyolExpress";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller name");
                return "TrendyolExpress";
            }
        }

        private async Task<List<Product>> GetProductVariantsAsync(IReadOnlyCollection<IWebElement> variantContainer)
        {
            var variants = new List<Product>();
            try
            {
                foreach (var container in variantContainer)
                {
                    var variantLinks = container.FindElements(By.CssSelector("a.slc-img"));
                    foreach (var link in variantLinks)
                    {
                        try
                        {
                            var href = link.GetDomAttribute("href");
                            if (!string.IsNullOrEmpty(href))
                            {
                                var variant = await CrawlProductAsync($"https://www.trendyol.com{href}");
                                variant.IsMainVariant = false;
                                variants.Add(variant);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error crawling variant");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product variants");
            }
            return variants;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _driver?.Quit();
                _driver?.Dispose();
                _disposed = true;
            }
        }
    }
} 