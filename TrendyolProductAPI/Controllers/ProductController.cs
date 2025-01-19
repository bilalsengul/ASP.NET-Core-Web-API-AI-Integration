using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TrendyolProductAPI.Services;

namespace TrendyolProductAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpPost("crawl")]
        public async Task<IActionResult> CrawlProduct([FromBody] string productUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(productUrl))
                {
                    return BadRequest("Product URL is required");
                }

                _logger.LogInformation("Crawling product from URL: {url}", productUrl);
                var products = await _productService.CrawlProductAsync(productUrl);
                
                if (products == null || products.Count == 0)
                {
                    return NotFound("No products found at the specified URL");
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {Url}", productUrl);
                return StatusCode(500, "An error occurred while crawling the product. Please check the logs for details.");
            }
        }

        [HttpPost("transform/{sku}")]
        public async Task<IActionResult> TransformProduct(string sku)
        {
            try
            {
                if (string.IsNullOrEmpty(sku))
                {
                    return BadRequest("SKU is required");
                }

                _logger.LogInformation("Transforming product with SKU: {sku}", sku);
                var transformedProduct = await _productService.TransformProductAsync(sku);
                
                if (transformedProduct == null)
                {
                    return NotFound($"Product with SKU {sku} not found");
                }

                return Ok(transformedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU: {Sku}. Error: {Error}", sku, ex.Message);
                return StatusCode(500, $"An error occurred while transforming the product: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                _logger.LogInformation("Retrieving all products");
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        [HttpGet("{sku}")]
        public async Task<IActionResult> GetProduct(string sku)
        {
            try
            {
                if (string.IsNullOrEmpty(sku))
                {
                    return BadRequest("SKU is required");
                }

                _logger.LogInformation("Retrieving product with SKU: {sku}", sku);
                var product = await _productService.GetProductBySkuAsync(sku);
                
                if (product == null)
                {
                    return NotFound($"Product with SKU {sku} not found");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with SKU: {Sku}", sku);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }
    }
} 