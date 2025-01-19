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
  CircularProgress,
  Stack,
  InputAdornment,
  IconButton,
  Tooltip
} from '@mui/material';
import ShoppingBagIcon from '@mui/icons-material/ShoppingBag';
import LinkIcon from '@mui/icons-material/Link';
import ClearIcon from '@mui/icons-material/Clear';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';

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

  const handleClear = () => {
    setUrl('');
    setError(null);
  };

  return (
    <Container maxWidth="md" sx={{ mt: 4, mb: 4 }}>
      <Paper 
        elevation={0} 
        sx={{ 
          p: 4,
          backgroundColor: 'transparent'
        }}
      >
        <Stack spacing={3}>
          <Box>
            <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
              Crawl New Product
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Enter a Trendyol product URL to analyze and transform the product information
            </Typography>
          </Box>

          {error && (
            <Alert 
              severity="error" 
              sx={{ 
                borderRadius: 2,
                '& .MuiAlert-icon': { alignItems: 'center' }
              }}
            >
              {error}
            </Alert>
          )}

          <Paper 
            elevation={2} 
            sx={{ 
              p: 4,
              borderRadius: 2,
              backgroundColor: 'background.paper'
            }}
          >
            <Box component="form" onSubmit={handleSubmit}>
              <Stack spacing={3}>
                <Box>
                  <Stack direction="row" spacing={1} alignItems="center" mb={1}>
                    <Typography variant="subtitle2" color="text.primary">
                      Product URL
                    </Typography>
                    <Tooltip title="Enter the URL of a Trendyol product page">
                      <HelpOutlineIcon 
                        fontSize="small" 
                        sx={{ color: 'text.secondary' }}
                      />
                    </Tooltip>
                  </Stack>
                  <TextField
                    fullWidth
                    variant="outlined"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    placeholder="https://www.trendyol.com/..."
                    disabled={loading}
                    error={!!error}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <LinkIcon color="action" />
                        </InputAdornment>
                      ),
                      endAdornment: url && (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={handleClear}
                            edge="end"
                            disabled={loading}
                          >
                            <ClearIcon />
                          </IconButton>
                        </InputAdornment>
                      )
                    }}
                    sx={{
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 2
                      }
                    }}
                  />
                </Box>

                <Button
                  type="submit"
                  variant="contained"
                  size="large"
                  disabled={loading || !url.trim()}
                  startIcon={loading ? <CircularProgress size={20} /> : <ShoppingBagIcon />}
                  sx={{
                    py: 1.5,
                    borderRadius: 2,
                    backgroundColor: 'primary.dark',
                    '&:hover': { backgroundColor: 'primary.main' }
                  }}
                >
                  {loading ? 'Crawling...' : 'Crawl Product'}
                </Button>
              </Stack>
            </Box>
          </Paper>

          <Paper 
            elevation={1}
            sx={{ 
              p: 3, 
              borderRadius: 2,
              backgroundColor: 'grey.50'
            }}
          >
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              How it works
            </Typography>
            <Typography variant="body2" color="text.secondary">
              1. Enter the URL of a Trendyol product<br />
              2. Click "Crawl Product" to fetch the product information<br />
              3. Transform the product details using AI<br />
              4. Save the transformed product to your collection
            </Typography>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  );
};

export default CrawlProduct; 