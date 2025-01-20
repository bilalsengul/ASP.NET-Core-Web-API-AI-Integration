using Microsoft.OpenApi.Models;
using TrendyolProductAPI.Middleware;
using TrendyolProductAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging first
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

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

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductCrawlerService, ProductCrawlerService>();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("X-API-Key");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before other middleware
app.UseCors();

// Add request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Add API key middleware
app.UseMiddleware<ApiKeyMiddleware>();

// Use routing and endpoints
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Log all registered endpoints
var endpointLogger = app.Services.GetRequiredService<ILogger<Program>>();
var endpoints = app.Services
    .GetRequiredService<IEnumerable<EndpointDataSource>>()
    .SelectMany(source => source.Endpoints);

foreach (var endpoint in endpoints)
{
    if (endpoint is RouteEndpoint routeEndpoint)
    {
        var httpMethods = routeEndpoint.Metadata
            .OfType<HttpMethodMetadata>()
            .FirstOrDefault()
            ?.HttpMethods ?? new[] { "Unknown" };

        endpointLogger.LogInformation(
            "Endpoint: {DisplayName}, Route: {RoutePattern}, HTTP Methods: {HttpMethods}",
            routeEndpoint.DisplayName,
            routeEndpoint.RoutePattern.RawText,
            string.Join(", ", httpMethods)
        );
    }
}

app.Run();
