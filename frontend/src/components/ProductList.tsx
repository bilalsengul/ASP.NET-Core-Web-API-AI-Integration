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
  Stack,
  Paper,
  Rating
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import ShoppingBagIcon from '@mui/icons-material/ShoppingBag';
import FavoriteIcon from '@mui/icons-material/Favorite';
import StarIcon from '@mui/icons-material/Star';

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
    <Container maxWidth="lg" sx={{ mt: 2, mb: 4 }}>
      <Paper elevation={0} sx={{ p: 3, backgroundColor: 'transparent' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box>
            <Typography variant="h5" component="h1" gutterBottom fontWeight="bold">
              Saved Products
            </Typography>
            <Typography variant="body2" color="text.secondary">
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
                backgroundColor: '#f27a1a',
                '&:hover': { backgroundColor: '#d65a00' }
              }}
            >
              Add New Product
            </Button>
            <Tooltip title="Refresh products">
              <IconButton 
                onClick={fetchProducts} 
                disabled={refreshing}
                sx={{ 
                  backgroundColor: '#f8f8f8',
                  '&:hover': { backgroundColor: '#e5e5e5' },
                  '&:disabled': { backgroundColor: '#f8f8f8' }
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
              backgroundColor: '#f8f8f8',
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
              sx={{ 
                backgroundColor: '#f27a1a',
                '&:hover': { backgroundColor: '#d65a00' }
              }}
            >
              Crawl New Product
            </Button>
          </Paper>
        ) : (
          <Grid container spacing={3}>
            {products.map((product) => (
              <Grid item xs={12} sm={6} md={4} lg={3} key={product.sku}>
                <Card 
                  sx={{ 
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'transform 0.2s, box-shadow 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: 3
                    },
                    borderRadius: 2,
                    border: '1px solid #e5e5e5'
                  }}
                >
                  <Box sx={{ position: 'relative' }}>
                    <CardMedia
                      component="div"
                      sx={{
                        height: 320,
                        backgroundColor: '#f8f8f8',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center'
                      }}
                    >
                      {product.images && product.images.length > 0 ? (
                        <img
                          src={product.images[0]}
                          alt={product.name}
                          style={{ height: '100%', width: '100%', objectFit: 'contain' }}
                        />
                      ) : (
                        <ShoppingBagIcon sx={{ fontSize: 48, color: 'text.secondary' }} />
                      )}
                    </CardMedia>
                    {product.score !== null && (
                      <Box
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: 'rgba(0, 0, 0, 0.6)',
                          color: 'white',
                          borderRadius: 1,
                          px: 1,
                          py: 0.5,
                          display: 'flex',
                          alignItems: 'center',
                          gap: 0.5
                        }}
                      >
                        <StarIcon sx={{ fontSize: 16 }} />
                        <Typography variant="caption">
                          {product.score?.toFixed(1)}/5
                        </Typography>
                      </Box>
                    )}
                  </Box>
                  <CardContent sx={{ flexGrow: 1, p: 2 }}>
                    <Stack spacing={1}>
                      <Typography 
                        variant="subtitle2" 
                        color="text.secondary"
                        sx={{
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          display: '-webkit-box',
                          WebkitLineClamp: 1,
                          WebkitBoxOrient: 'vertical',
                        }}
                      >
                        {product.brand}
                      </Typography>
                      
                      <Typography 
                        variant="body1"
                        sx={{
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          display: '-webkit-box',
                          WebkitLineClamp: 2,
                          WebkitBoxOrient: 'vertical',
                          minHeight: '3em',
                          lineHeight: 1.5
                        }}
                      >
                        {product.name}
                      </Typography>

                      {product.attributes && (
                        <Stack direction="row" spacing={2} alignItems="center">
                          {product.attributes.find(attr => attr.name === 'review_count') && (
                            <Stack direction="row" alignItems="center" spacing={0.5}>
                              <Rating 
                                value={3.7} 
                                size="small" 
                                readOnly 
                                precision={0.1}
                              />
                              <Typography variant="caption" color="text.secondary">
                                ({product.attributes.find(attr => attr.name === 'review_count')?.value || 0})
                              </Typography>
                            </Stack>
                          )}
                          {product.attributes.find(attr => attr.name === 'favorite_count') && (
                            <Stack direction="row" alignItems="center" spacing={0.5}>
                              <FavoriteIcon sx={{ fontSize: 16, color: '#e81224' }} />
                              <Typography variant="caption" color="text.secondary">
                                {product.attributes.find(attr => attr.name === 'favorite_count')?.value || 0}
                              </Typography>
                            </Stack>
                          )}
                        </Stack>
                      )}

                      <Box>
                        <Typography variant="h6" color="#f27a1a" fontWeight="bold">
                          {(product.discountedPrice / 100).toLocaleString('tr-TR', {
                            style: 'currency',
                            currency: 'TRY'
                          })}
                        </Typography>
                        {product.originalPrice !== product.discountedPrice && (
                          <Typography 
                            variant="body2" 
                            color="text.secondary" 
                            sx={{ textDecoration: 'line-through' }}
                          >
                            {(product.originalPrice / 100).toLocaleString('tr-TR', {
                              style: 'currency',
                              currency: 'TRY'
                            })}
                          </Typography>
                        )}
                      </Box>

                      {product.variants && product.variants.length > 0 && (
                        <Box>
                          <Typography variant="caption" color="text.secondary" display="block" gutterBottom>
                            Available Colors:
                          </Typography>
                          <Stack direction="row" spacing={1} flexWrap="wrap">
                            {product.variants.slice(0, 4).map((variant, index) => (
                              <Box
                                key={variant.sku}
                                sx={{
                                  width: 24,
                                  height: 24,
                                  borderRadius: '50%',
                                  overflow: 'hidden',
                                  border: '1px solid #e5e5e5'
                                }}
                              >
                                {variant.images && variant.images[0] ? (
                                  <img
                                    src={variant.images[0]}
                                    alt={variant.color || `Color ${index + 1}`}
                                    style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                                  />
                                ) : (
                                  <Box sx={{ width: '100%', height: '100%', bgcolor: '#f8f8f8' }} />
                                )}
                              </Box>
                            ))}
                            {product.variants.length > 4 && (
                              <Typography variant="caption" color="text.secondary" sx={{ alignSelf: 'center' }}>
                                +{product.variants.length - 4} more
                              </Typography>
                            )}
                          </Stack>
                        </Box>
                      )}
                    </Stack>
                  </CardContent>
                  <CardActions sx={{ p: 2, pt: 0 }}>
                    <Button 
                      fullWidth
                      variant="contained"
                      component={Link} 
                      to={`/product/${product.sku}`}
                      sx={{ 
                        borderRadius: 1,
                        textTransform: 'none',
                        backgroundColor: '#f27a1a',
                        '&:hover': { backgroundColor: '#d65a00' }
                      }}
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