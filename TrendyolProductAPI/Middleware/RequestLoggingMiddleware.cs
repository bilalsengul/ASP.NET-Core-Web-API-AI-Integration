using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
            // Log request details
            var request = context.Request;
            var requestBody = string.Empty;

            // Enable request body buffering
            request.EnableBuffering();

            if (request.ContentLength > 0)
            {
                using (var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0;  // Reset the position to allow reading again
                }
            }

            var requestHeaders = request.Headers
                .Select(h => $"{h.Key}: {string.Join(",", h.Value.Select(v => v?.ToString() ?? ""))}")
                .ToList();

            _logger.LogInformation(
                "Request: {Method} {Path}{Query}\nHeaders: {Headers}\nBody: {Body}",
                request.Method,
                request.Path,
                request.QueryString,
                string.Join(", ", requestHeaders),
                requestBody
            );

            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                responseBody.Seek(0, SeekOrigin.Begin);
                var response = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                var responseHeaders = context.Response.Headers
                    .Select(h => $"{h.Key}: {string.Join(",", h.Value.Select(v => v?.ToString() ?? ""))}")
                    .ToList();

                _logger.LogInformation(
                    "Response: Status: {StatusCode}\nHeaders: {Headers}\nBody: {Body}",
                    context.Response.StatusCode,
                    string.Join(", ", responseHeaders),
                    response
                );

                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}