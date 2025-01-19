using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TrendyolProductAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                _logger.LogWarning("API Key header is missing");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing");
                return;
            }

            var configuredApiKey = _configuration["ApiKey"];
            _logger.LogInformation("Configured API Key exists: {exists}", !string.IsNullOrEmpty(configuredApiKey));

            if (string.IsNullOrEmpty(configuredApiKey))
            {
                _logger.LogError("API Key is not configured in settings");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("API Key is not configured");
                return;
            }

            var extractedKeyString = extractedApiKey.ToString();
            var isValid = configuredApiKey.Equals(extractedKeyString, StringComparison.Ordinal);
            _logger.LogInformation("API Key validation result: {result}", isValid);

            if (!isValid)
            {
                _logger.LogWarning("Invalid API Key provided");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            await _next(context);
        }
    }
} 