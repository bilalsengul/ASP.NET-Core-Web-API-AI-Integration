import axios, { AxiosError } from 'axios';

const API_KEY = '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';
const BASE_URL = 'http://localhost:5121/api';

const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
  },
    'X-API-Key': API_KEY
  }
});

interface ProductAttribute {
  name: string;
  value: string;
}

export interface Product {
  name: string;
  description: string | null;
  sku: string;
  parentSku: string | null;
  attributes: ProductAttribute[];
  category: string | null;
  brand: string;
  originalPrice: number;
  discountedPrice: number;
  images: string[];
  score: number | null;
  isSaved: boolean;
  color: string | null;
  size: string | null;
  variantId: string | null;
  isMainVariant: boolean;
  variants: Product[];
}

interface ProductVariants {
  colors: string[];
  sizes: string[];
  variants: Product[];
}

const handleApiError = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ message: string }>;
    console.error('API Error:', {
      status: axiosError.response?.status,
      statusText: axiosError.response?.statusText,
      data: axiosError.response?.data,
      headers: axiosError.response?.headers
    });

    if (axiosError.response) {
      if (axiosError.response.status === 401) {
        throw new Error('Invalid API key. Please check your configuration.');
      } else if (axiosError.response.status === 403) {
        throw new Error('Access forbidden. Please check your API key.');
      } else if (axiosError.response.status === 404) {
        throw new Error('Resource not found.');
      } else {
        throw new Error(axiosError.response.data?.message || `Server error: ${axiosError.response.status}`);
      }
    } else if (axiosError.request) {
      console.error('No response received:', axiosError.request);
      throw new Error('No response received from server. Please check if the backend is running.');
    }
  }
  console.error('Unexpected error:', error);
  throw new Error('An unexpected error occurred');
};

export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ message: string }>;
    if (axiosError.response?.data?.message) {
      return axiosError.response.data.message;
    }
    return axiosError.message;
  }
  return error instanceof Error ? error.message : 'An unknown error occurred';
};

export const crawlProduct = async (url: string): Promise<Product[]> => {
  try {
    console.log('Sending crawl request with URL:', url);
    const response = await api.post('/products/crawl', { url });
    console.log('Crawl response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Crawl error details:', error);
    handleApiError(error);
    throw error;
  }
};

export const transformProduct = async (sku: string): Promise<Product> => {
  try {
    const response = await api.post(`/products/transform/${sku}`);
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const getAllProducts = async (): Promise<Product[]> => {
  try {
    const response = await api.get('/products');
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const getProductBySku = async (sku: string): Promise<Product> => {
  try {
    const response = await api.get(`/products/${sku}`);
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const saveProduct = async (product: Product): Promise<Product> => {
  try {
    const response = await api.post('/products', product, {
      headers: {
        'Content-Type': 'application/json',
      },
    });
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const getProductVariants = async (sku: string): Promise<ProductVariants> => {
  try {
    const response = await api.get<ProductVariants>(`/products/${sku}/variants`);
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

// Add request interceptor to ensure API key is set
api.interceptors.request.use((config) => {
  if (!config.headers['X-API-Key']) {
    config.headers['X-API-Key'] = API_KEY;
  }
  return config;
});

export default api;