using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TrendyolProductAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request
            var request = await FormatRequest(context.Request);
            _logger.LogInformation($"Request: {request}");

            // Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            // Create a new memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Continue down the middleware pipeline
            await _next(context);

            // Format and log the response
            var response = await FormatResponse(context.Response);
            _logger.LogInformation($"Response Status: {context.Response.StatusCode}, Body: {response}");

            // Copy the response to the original stream and restore it
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();

            var body = string.Empty;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Path: {request.Path}");
            builder.AppendLine($"QueryString: {request.QueryString}");
            builder.AppendLine($"Request Body: {body}");

            return builder.ToString();
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }
    }
} 