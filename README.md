# Trendyol Product AI Integration

A modern web application that crawls product information from Trendyol, transforms it using AI, and allows you to save and manage your product collection.

## Features

-  **Product Crawling**: Easily fetch product details from Trendyol URLs
-  **AI Transformation**: Transform product information using OpenAI's GPT-4
-  **Product Management**: Save and view your transformed products
-  **Modern UI**: Clean and responsive interface built with Material-UI
-  **Multi-language Support**: Product details are translated to English

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (version 16.0 or later)
- OpenAI API Key
- API Key for authentication

## Project Structure

```
TrendyolProductAPI/          # Backend API project
├── Controllers/            # API endpoints
├── Models/                # Data models
├── Services/              # Business logic
├── Middleware/           # Custom middleware
└── Extensions/           # Extension methods

frontend/                  # React frontend
├── src/
│   ├── components/      # React components
│   ├── services/       # API integration
│   └── App.tsx        # Main application
```

## Installation

### 1. Clone the Repository

```bash
git clone <repository-url>
cd <repository-name>
```

### 2. Backend Setup

1. Navigate to the API project:
```bash
cd TrendyolProductAPI
```

2. Verify .NET version:
```bash
dotnet --version
# Should show 9.0.x
```

3. Install dependencies:
```bash
dotnet restore
```

4. Configure API Keys:
   Create or update `appsettings.json`:
```json
{
  "ApiKey": "your-api-key",
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4-1106-preview"
  }
}
```

### 3. Frontend Setup

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Install dependencies:
```bash
npm install
```

3. Update API configuration:
   In `src/services/api.ts`, update the API key and base URL if needed.

## Running the Application

### Start the Backend

```bash
cd TrendyolProductAPI
dotnet run
```
The API will be available at `http://localhost:5121`

### Start the Frontend

```bash
cd frontend
npm run dev
```
The application will be available at `http://localhost:5173`

## Usage Guide

### 1. Adding Products

1. Click "Add New Product" or navigate to the Crawl page
2. Enter a Trendyol product URL (e.g., https://www.trendyol.com/brand/product-p-123456)
3. Click "Crawl Product"

### 2. Transforming Products

1. After crawling, you'll see the product details
2. Click "Transform Product"
3. The AI will:
   - Translate to English
   - Generate a description
   - Assign a quality score

### 3. Managing Products

- Save transformed products
- View all products in the list
- Sort by AI score
- View detailed information

## API Documentation

### Endpoints

```
POST /api/Product/crawl
- Body: Product URL
- Header: X-API-Key

POST /api/Product/transform/{sku}
- Path: SKU
- Header: X-API-Key

POST /api/Product/save
- Body: Product object
- Header: X-API-Key

GET /api/Product
- Header: X-API-Key

GET /api/Product/{sku}
- Path: SKU
- Header: X-API-Key
```

## Dependencies

### Backend (.NET 9.0)
- HtmlAgilityPack (1.11.54)
- OpenAI (1.7.2)
- Microsoft.AspNetCore.OpenApi (9.0.0)
- Swashbuckle.AspNetCore (6.5.0)

### Frontend
- React 18
- Material-UI
- React Router
- Axios

## Development

### Backend Development
```bash
cd TrendyolProductAPI
dotnet watch run
```

### Frontend Development
```bash
cd frontend
npm run dev
```

## Error Handling

The application includes comprehensive error handling for:
- Invalid URLs
- Network issues
- API rate limits
- Missing products
- Authentication errors

## Security

- API Key authentication required for all endpoints
- OpenAI API key stored securely in backend
- CORS configured for frontend access
- Request/Response logging
- Input validation

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.
