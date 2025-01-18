# Trendyol Product API

This ASP.NET Core Web API project provides endpoints for crawling and transforming product information from Trendyol.

## Features

- Product crawling from Trendyol URLs
- AI-powered product information translation and scoring
- API key authentication
- Swagger documentation
- Request/Response logging

## Prerequisites

- .NET 7.0 SDK or later
- OpenAI API key

## Setup

1. Clone the repository
2. Update the `appsettings.json` file with your API keys:
   ```json
   {
     "ApiKey": "your-api-key-here",
     "OpenAI": {
       "ApiKey": "your-openai-key-here"
     }
   }
   ```
3. Run the application:
   ```bash
   dotnet restore
   dotnet run
   ```

## API Endpoints

### 1. Crawl Product
- **POST** `/api/Product/crawl`
- **Body**: Product URL (string)
- **Headers**: X-API-Key

### 2. Transform Product
- **POST** `/api/Product/transform/{sku}`
- **Parameters**: SKU (string)
- **Headers**: X-API-Key

### 3. Get All Products
- **GET** `/api/Product`
- **Headers**: X-API-Key

### 4. Get Product by SKU
- **GET** `/api/Product/{sku}`
- **Parameters**: SKU (string)
- **Headers**: X-API-Key

## Testing

The API can be tested using the built-in Swagger UI at `/swagger` when running in development mode.

## Dependencies

- HtmlAgilityPack (for web scraping)
- OpenAI API (for translation and scoring)
- MemoryCache (for product storage)

## Error Handling

The API includes comprehensive error handling and logging:
- Request/Response logging
- Exception handling with appropriate status codes
- Detailed error messages in development mode
