# Trendyol Product AI Integration

A modern web application that crawls product information from Trendyol, transforms it using AI, and allows you to save and manage your product collection.

## Features

-  **Product Crawling**: Easily fetch product details from Trendyol URLs
-  **AI Transformation**: Transform product information using OpenAI's GPT-4
-  **Product Management**: Save and view your transformed products
-  **Modern UI**: Clean and responsive interface built with Material-UI
-  **Multi-language Support**: Product details are translated to English

## Getting Started

### Prerequisites

- .NET 7.0 or later
- Node.js 16.0 or later
- OpenAI API Key
- API Key for authentication

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/trendyol-product-ai.git
cd trendyol-product-ai
```

2. Backend Setup:
```bash
cd TrendyolProductAPI
dotnet restore
```

3. Frontend Setup:
```bash
cd frontend
npm install
```

4. Configure API Keys:
- Create `appsettings.json` in the `TrendyolProductAPI` directory:
```json
{
  "ApiKey": "your-api-key",
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4-1106-preview"
  }
}
```

### Running the Application

1. Start the Backend:
```bash
cd TrendyolProductAPI
dotnet run
```

2. Start the Frontend:
```bash
cd frontend
npm run dev
```

3. Open your browser and navigate to `http://localhost:5173`

## Usage Guide

### 1. Adding a New Product

1. Click "Add New Product" on the products list page
2. Enter a Trendyol product URL (e.g., https://www.trendyol.com/brand/product-p-123456)
3. Click "Crawl Product" to fetch the product information

### 2. Transforming Products

1. After crawling, you'll be redirected to the product details page
2. Click "Transform Product" to process the product with AI
3. The AI will:
   - Translate product details to English
   - Generate a comprehensive description
   - Assign a quality score

### 3. Saving Products

1. After transformation, a "Save Product" button will appear
2. Click it to add the product to your collection
3. You'll be redirected to the products list

### 4. Managing Products

- View all saved products on the main page
- Click "View Details" on any product card to see full information
- Use the refresh button to update the product list
- Products are sorted by their AI score

## API Endpoints

- `POST /api/Product/crawl` - Crawl a product from Trendyol
- `POST /api/Product/transform/{sku}` - Transform product using AI
- `POST /api/Product/save` - Save a transformed product
- `GET /api/Product` - Get all saved products
- `GET /api/Product/{sku}` - Get a specific product

## Technical Details

### Backend
- ASP.NET Core Web API
- Memory Cache for product storage
- OpenAI integration for AI transformations
- Custom middleware for API key validation

### Frontend
- React with TypeScript
- Material-UI for components
- React Router for navigation
- Axios for API communication

## Security

- API Key authentication required for all endpoints
- OpenAI API key stored securely in backend
- CORS configured for frontend access

## Error Handling

The application includes comprehensive error handling:
- Invalid URLs
- Network issues
- API rate limits
- Transformation failures
- Missing products

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
