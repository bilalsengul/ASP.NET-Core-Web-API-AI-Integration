using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TrendyolProductAPI.Services;
using TrendyolProductAPI.Models;

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
        public async Task<ActionResult<IEnumerable<Product>>> CrawlProduct([FromBody] string productUrl)
        {
            try
            {
                _logger.LogInformation("Crawling product from URL: {url}", productUrl);
                var products = await _productService.CrawlProductAsync(productUrl);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {url}", productUrl);
                return StatusCode(500, "An error occurred while crawling the product. Please check the logs for details.");
            }
        }

        [HttpPost("transform/{sku}")]
        public async Task<ActionResult<Product>> TransformProduct(string sku)
        {
            try
            {
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
                _logger.LogError(ex, "Error transforming product with SKU: {sku}", sku);
                return StatusCode(500, "An error occurred while transforming the product. Please check the logs for details.");
            }
        }

        [HttpPost("save")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<Product>> SaveProduct([FromBody] Product product)
        {
            try
            {
                if (product == null)
                {
                    return BadRequest("Product data is required");
                }

                if (string.IsNullOrEmpty(product.Sku))
                {
                    return BadRequest("Product SKU is required");
                }

                _logger.LogInformation("Saving product with SKU: {sku}", product.Sku);
                var savedProduct = await _productService.SaveProductAsync(product);
                return Ok(savedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product with SKU: {sku}", product?.Sku);
                return StatusCode(500, "An error occurred while saving the product. Please check the logs for details.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
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
                return StatusCode(500, "An error occurred while retrieving products. Please check the logs for details.");
            }
        }

        [HttpGet("{sku}")]
        public async Task<ActionResult<Product>> GetProductBySku(string sku)
        {
            try
            {
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
                _logger.LogError(ex, "Error retrieving product with SKU: {sku}", sku);
                return StatusCode(500, "An error occurred while retrieving the product. Please check the logs for details.");
            }
        }
    }
} 