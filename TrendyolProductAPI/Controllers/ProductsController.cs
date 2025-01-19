using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TrendyolProductAPI.Models;
using TrendyolProductAPI.Services;

namespace TrendyolProductAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{sku}")]
        public async Task<ActionResult<Product>> GetProduct(string sku)
        {
            try
            {
                var product = await _productService.GetProductBySkuAsync(sku);
                if (product == null)
                {
                    return NotFound($"Product with SKU {sku} not found");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with SKU {Sku}", sku);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{sku}/variants")]
        public async Task<ActionResult<ProductVariants>> GetProductVariants(string sku)
        {
            try
            {
                var variants = await _productService.GetProductVariantsAsync(sku);
                return Ok(variants);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variants for product with SKU {Sku}", sku);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct([FromBody] Product product)
        {
            try
            {
                var savedProduct = await _productService.SaveProductAsync(product);
                return Ok(savedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product");
                return StatusCode(500, "Internal server error");
            }
        }

        public class CrawlRequest
        {
            public string Url { get; set; }
        }

        [HttpPost("crawl")]
        public async Task<IActionResult> CrawlProduct([FromBody] CrawlRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Url))
                {
                    return BadRequest("URL is required");
                }

                var products = await _productService.CrawlProductAsync(request.Url);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL {Url}", request?.Url);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("transform/{sku}")]
        public async Task<IActionResult> TransformProduct(string sku)
        {
            try
            {
                var transformedProduct = await _productService.TransformProductAsync(sku);
                return Ok(transformedProduct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU {Sku}", sku);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
