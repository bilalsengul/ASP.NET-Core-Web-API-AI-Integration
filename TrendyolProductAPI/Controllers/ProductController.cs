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
                var products = await _productService.CrawlProductAsync(productUrl);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling product from URL: {Url}", productUrl);
                return StatusCode(500, "An error occurred while crawling the product");
            }
        }

        [HttpPost("transform/{sku}")]
        public async Task<IActionResult> TransformProduct(string sku)
        {
            try
            {
                var transformedProduct = await _productService.TransformProductAsync(sku);
                if (transformedProduct == null)
                {
                    return NotFound($"Product with SKU {sku} not found");
                }
                return Ok(transformedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU: {Sku}", sku);
                return StatusCode(500, "An error occurred while transforming the product");
            }
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
                _logger.LogError(ex, "Error retrieving all products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        [HttpGet("{sku}")]
        public async Task<IActionResult> GetProduct(string sku)
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
                _logger.LogError(ex, "Error retrieving product with SKU: {Sku}", sku);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }
    }
} 