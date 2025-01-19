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
  Tooltip
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';

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
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Saved Products
        </Typography>
        <Tooltip title="Refresh products">
          <IconButton 
            onClick={fetchProducts} 
            disabled={refreshing}
            color="primary"
          >
            <RefreshIcon />
          </IconButton>
        </Tooltip>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {products.length === 0 ? (
        <Alert severity="info">
          No products saved yet. Go to "Crawl New" to add products.
        </Alert>
      ) : (
        <Grid container spacing={3}>
          {products.map((product) => (
            <Grid item xs={12} sm={6} md={4} key={product.sku}>
              <Card>
                <CardContent>
                  <Typography variant="h6" component="div" gutterBottom>
                    {product.name}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Brand: {product.brand}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    SKU: {product.sku}
                  </Typography>
                  <Typography variant="body1">
                    Price: ${(product.discountedPrice / 100).toFixed(2)}
                  </Typography>
                  {product.score !== null && (
                    <Typography variant="body2" color="primary">
                      AI Score: {product.score}
                    </Typography>
                  )}
                </CardContent>
                <CardActions>
                  <Button 
                    size="small" 
                    component={Link} 
                    to={`/product/${product.sku}`}
                  >
                    View Details
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
};

export default ProductList; 