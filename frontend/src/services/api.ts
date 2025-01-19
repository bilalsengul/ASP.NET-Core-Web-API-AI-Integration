import axios from 'axios';

const API_KEY = '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';
const BASE_URL = 'http://localhost:5121/api';

const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY,
  },
});

export interface Product {
  name: string;
  description: string | null;
  sku: string;
  parentSku: string | null;
  attributes: Array<any>;
  category: string | null;
  brand: string;
  originalPrice: number;
  discountedPrice: number;
  images: string[];
  score: number | null;
}

export const crawlProduct = async (url: string): Promise<Product[]> => {
  const response = await api.post('/Product/crawl', JSON.stringify(url));
  return response.data;
};

export const transformProduct = async (sku: string): Promise<Product> => {
  const response = await api.post(`/Product/transform/${sku}`);
  return response.data;
};

export const getAllProducts = async (): Promise<Product[]> => {
  const response = await api.get('/Product');
  return response.data;
};

export const getProductBySku = async (sku: string): Promise<Product> => {
  const response = await api.get(`/Product/${sku}`);
  return response.data;
};

export default api; 