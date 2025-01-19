import axios, { AxiosError } from 'axios';

const API_KEY = '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';
const BASE_URL = 'http://localhost:5121/api';

const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
  },
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
}

const handleApiError = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ message: string }>;
    if (axiosError.response) {
      // The request was made and the server responded with a status code
      // that falls out of the range of 2xx
      throw new Error(axiosError.response.data?.message || 'Server error occurred');
    } else if (axiosError.request) {
      // The request was made but no response was received
      throw new Error('No response received from server. Please check your connection.');
    }
  }
  // Something happened in setting up the request that triggered an Error
  throw new Error('An unexpected error occurred');
};

export const crawlProduct = async (url: string): Promise<Product[]> => {
  try {
    const response = await api.post('/Product/crawl', JSON.stringify(url));
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const transformProduct = async (sku: string): Promise<Product> => {
  try {
    const response = await api.post(`/Product/transform/${sku}`);
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const getAllProducts = async (): Promise<Product[]> => {
  try {
    const response = await api.get('/Product');
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export const getProductBySku = async (sku: string): Promise<Product> => {
  try {
    const response = await api.get(`/Product/${sku}`);
    return response.data;
  } catch (error) {
    handleApiError(error);
    throw error;
  }
};

export default api; 