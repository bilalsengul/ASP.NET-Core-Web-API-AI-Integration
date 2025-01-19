import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getAllProducts, Product } from '../services/api';
import { 
  Grid, 
  Card, 
  CardContent, 
  Typography, 
  CardActions, 
  Button,
  Container,
  CircularProgress,
  Box,
  Alert,
  IconButton,
  Tooltip,
  CardMedia,
  Chip,
  Stack,
  Paper,
  Rating
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import ShoppingBagIcon from '@mui/icons-material/ShoppingBag';
import LocalOfferIcon from '@mui/icons-material/LocalOffer';

const ProductList: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const fetchProducts = async () => {
    try {
      setRefreshing(true);
      const data = await getAllProducts();
      setProducts(data);
      setError(null);
    } catch (error: unknown) {
      console.error('Error fetching products:', error);
      setError('Failed to fetch products. Please try again later.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper elevation={0} sx={{ p: 3, backgroundColor: 'transparent' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={4}>
          <Box>
            <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
              Saved Products
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              {products.length} {products.length === 1 ? 'product' : 'products'} found
            </Typography>
          </Box>
          <Stack direction="row" spacing={2} alignItems="center">
            <Button
              component={Link}
              to="/crawl"
              variant="contained"
              startIcon={<ShoppingBagIcon />}
              sx={{ 
                backgroundColor: 'primary.dark',
                '&:hover': { backgroundColor: 'primary.main' }
              }}
            >
              Add New Product
            </Button>
            <Tooltip title="Refresh products">
              <IconButton 
                onClick={fetchProducts} 
                disabled={refreshing}
                color="primary"
                sx={{ 
                  backgroundColor: 'primary.light',
                  '&:hover': { backgroundColor: 'primary.main' },
                  '&:disabled': { backgroundColor: 'action.disabledBackground' }
                }}
              >
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        {products.length === 0 ? (
          <Paper 
            elevation={0} 
            sx={{ 
              p: 6, 
              textAlign: 'center',
              backgroundColor: 'grey.50',
              borderRadius: 2
            }}
          >
            <ShoppingBagIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="text.secondary" gutterBottom>
              No products saved yet
            </Typography>
            <Typography variant="body2" color="text.secondary" mb={3}>
              Start by crawling a new product from Trendyol
            </Typography>
            <Button
              component={Link}
              to="/crawl"
              variant="contained"
              startIcon={<ShoppingBagIcon />}
            >
              Crawl New Product
            </Button>
          </Paper>
        ) : (
          <Grid container spacing={3}>
            {products.map((product) => (
              <Grid item xs={12} sm={6} md={4} key={product.sku}>
                <Card 
                  sx={{ 
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'transform 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: 6
                    }
                  }}
                >
                  <CardMedia
                    component="div"
                    sx={{
                      height: 200,
                      backgroundColor: 'grey.100',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center'
                    }}
                  >
                    {product.images && product.images.length > 0 ? (
                      <img
                        src={product.images[0]}
                        alt={product.name}
                        style={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }}
                      />
                    ) : (
                      <ShoppingBagIcon sx={{ fontSize: 48, color: 'text.secondary' }} />
                    )}
                  </CardMedia>
                  <CardContent sx={{ flexGrow: 1 }}>
                    <Typography variant="h6" component="div" gutterBottom noWrap>
                      {product.name}
                    </Typography>
                    <Stack direction="row" spacing={1} mb={1}>
                      <Chip 
                        label={product.brand}
                        size="small"
                        color="primary"
                        variant="outlined"
                      />
                      <Chip
                        label={`SKU: ${product.sku}`}
                        size="small"
                        variant="outlined"
                      />
                    </Stack>
                    <Box display="flex" alignItems="center" gap={1} mb={2}>
                      <LocalOfferIcon color="error" fontSize="small" />
                      <Typography variant="h6" color="error.main">
                        ${(product.discountedPrice / 100).toFixed(2)}
                      </Typography>
                      {product.originalPrice !== product.discountedPrice && (
                        <Typography 
                          variant="body2" 
                          color="text.secondary" 
                          sx={{ textDecoration: 'line-through' }}
                        >
                          ${(product.originalPrice / 100).toFixed(2)}
                        </Typography>
                      )}
                    </Box>
                    {product.score !== null && (
                      <Box display="flex" alignItems="center" gap={1}>
                        <Rating 
                          value={product.score / 20} 
                          precision={0.5} 
                          readOnly 
                        />
                        <Typography variant="body2" color="text.secondary">
                          ({product.score}/100)
                        </Typography>
                      </Box>
                    )}
                  </CardContent>
                  <CardActions>
                    <Button 
                      size="small" 
                      component={Link} 
                      to={`/product/${product.sku}`}
                      sx={{ ml: 'auto' }}
                    >
                      View Details
                    </Button>
                  </CardActions>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </Paper>
    </Container>
  );
};

export default ProductList; 