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
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("X-API-Key")
               .SetIsOriginAllowed(origin => 
               {
                   var allowedOrigins = new[] { "http://localhost:5173" };
                   return allowedOrigins.Contains(origin);
               });
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

// Enable CORS before any other middleware
app.UseCors();

// The rest of your middleware
app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
