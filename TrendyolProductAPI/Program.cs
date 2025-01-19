using Microsoft.OpenApi.Models;
using TrendyolProductAPI.Middleware;
using TrendyolProductAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trendyol Product API", Version = "v1" });
    
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication using the 'X-API-Key' header",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = ParameterLocation.Header
    };

    var requirement = new OpenApiSecurityRequirement
    {
        { scheme, new List<string>() }
    };

    c.AddSecurityRequirement(requirement);
});

// Add memory cache for storing crawled products
builder.Services.AddMemoryCache();

// Register HttpClient
builder.Services.AddHttpClient();

// Register ProductService
builder.Services.AddScoped<IProductService, ProductService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Set to Debug for more detailed logs

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithMethods("GET", "POST", "PUT", "DELETE")
               .WithExposedHeaders("X-API-Key");
    });
});

var app = builder.Build();

// Log configuration values
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("API Key configured: {exists}", !string.IsNullOrEmpty(app.Configuration["ApiKey"]));
logger.LogInformation("OpenAI Key configured: {exists}", !string.IsNullOrEmpty(app.Configuration["OpenAI:ApiKey"]));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before other middleware
app.UseCors();

// Add request logging middleware first
app.UseMiddleware<RequestLoggingMiddleware>();

// Add API key middleware before routing
app.UseMiddleware<ApiKeyMiddleware>();

// The rest of your middleware
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

// Map controllers after all middleware
app.MapControllers();

// Log all registered endpoints
var endpoints = app.Services
    .GetRequiredService<IEnumerable<EndpointDataSource>>()
    .SelectMany(source => source.Endpoints);

logger = app.Services.GetRequiredService<ILogger<Program>>();
foreach (var endpoint in endpoints)
{
    if (endpoint is RouteEndpoint routeEndpoint)
    {
        logger.LogInformation(
            "Endpoint: {DisplayName}, Route: {RoutePattern}, HTTP Methods: {HttpMethods}",
            routeEndpoint.DisplayName,
            routeEndpoint.RoutePattern.RawText,
            string.Join(", ", routeEndpoint.Metadata.GetOrderedMetadata<HttpMethodMetadata>().SelectMany(m => m.HttpMethods))
        );
    }
}

app.Run();
