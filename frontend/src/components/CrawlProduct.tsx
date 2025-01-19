import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { crawlProduct } from '../services/api';
import {
  Container,
  Typography,
  Box,
  TextField,
  Button,
  Paper,
  Alert,
  CircularProgress
} from '@mui/material';

const CrawlProduct: React.FC = () => {
  const navigate = useNavigate();
  const [url, setUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) {
      setError('Please enter a valid Trendyol product URL');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const products = await crawlProduct(url);
      if (products && products.length > 0) {
        navigate(`/product/${products[0].sku}`);
      } else {
        setError('No product found at the provided URL');
      }
    } catch (error: unknown) {
      console.error('Error crawling product:', error);
      setError('Failed to crawl product. Please check the URL and try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="md" sx={{ mt: 4, mb: 4 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Crawl New Product
        </Typography>
        
        <Typography variant="body1" sx={{ mb: 3 }}>
          Enter a Trendyol product URL to crawl and analyze the product information.
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Product URL"
            variant="outlined"
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://www.trendyol.com/..."
            disabled={loading}
            sx={{ mb: 2 }}
          />

          <Button
            type="submit"
            variant="contained"
            color="primary"
            size="large"
            disabled={loading}
            sx={{ mt: 2 }}
          >
            {loading ? (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CircularProgress size={20} color="inherit" />
                Crawling...
              </Box>
            ) : (
              'Crawl Product'
            )}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default CrawlProduct; 