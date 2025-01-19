import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProductBySku, transformProduct, saveProduct, Product } from '../services/api';
import {
  Container,
  Typography,
  Box,
  Button,
  CircularProgress,
  Alert,
  Paper,
  Stack,
  Chip,
  Rating,
  Divider,
  Grid
} from '@mui/material';
import ShoppingBagIcon from '@mui/icons-material/ShoppingBag';
import LocalOfferIcon from '@mui/icons-material/LocalOffer';
import DescriptionIcon from '@mui/icons-material/Description';
import SaveIcon from '@mui/icons-material/Save';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';

const ProductDetail: React.FC = () => {
  const { sku } = useParams<{ sku: string }>();
  const navigate = useNavigate();
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [transforming, setTransforming] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const fetchProduct = async () => {
      if (!sku) return;
      
      try {
        const data = await getProductBySku(sku);
        setProduct(data);
        setError(null);
      } catch (error: unknown) {
        console.error('Error fetching product:', error);
        setError('Failed to fetch product details. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    fetchProduct();
  }, [sku]);

  const handleTransform = async () => {
    if (!sku) return;
    
    setTransforming(true);
    setError(null);

    try {
      const transformedProduct = await transformProduct(sku);
      setProduct(transformedProduct);
    } catch (error: unknown) {
      console.error('Error transforming product:', error);
      setError('Failed to transform product. Please try again later.');
    } finally {
      setTransforming(false);
    }
  };

  const handleSave = async () => {
    if (!product) return;
    
    setSaving(true);
    try {
      await saveProduct(product);
      navigate('/', { replace: true });
    } catch (error: unknown) {
      console.error('Error saving product:', error);
      setError('Failed to save product. Please try again later.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!product) {
    return (
      <Container maxWidth="md" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error">Product not found</Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper elevation={0} sx={{ p: 4, backgroundColor: 'transparent' }}>
        <Stack spacing={3}>
          <Box>
            <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
              Product Details
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              View and manage product information
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

          <Paper elevation={2} sx={{ p: 4, borderRadius: 2 }}>
            <Grid container spacing={4}>
              <Grid item xs={12} md={4}>
                <Box
                  sx={{
                    height: 300,
                    backgroundColor: 'grey.100',
                    borderRadius: 2,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    mb: { xs: 2, md: 0 }
                  }}
                >
                  {product.images && product.images.length > 0 ? (
                    <img
                      src={product.images[0]}
                      alt={product.name}
                      style={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }}
                    />
                  ) : (
                    <ShoppingBagIcon sx={{ fontSize: 64, color: 'text.secondary' }} />
                  )}
                </Box>
              </Grid>

              <Grid item xs={12} md={8}>
                <Stack spacing={3}>
                  <Box>
                    <Typography variant="h5" gutterBottom>
                      {product.name}
                    </Typography>
                    <Stack direction="row" spacing={1} mb={2}>
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
                  </Box>

                  <Box>
                    <Stack direction="row" alignItems="center" spacing={1} mb={1}>
                      <LocalOfferIcon color="error" />
                      <Typography variant="h5" color="error.main">
                        ${(product.discountedPrice / 100).toFixed(2)}
                      </Typography>
                      {product.originalPrice !== product.discountedPrice && (
                        <Typography 
                          variant="body1" 
                          color="text.secondary" 
                          sx={{ textDecoration: 'line-through' }}
                        >
                          ${(product.originalPrice / 100).toFixed(2)}
                        </Typography>
                      )}
                    </Stack>
                  </Box>

                  {product.description && (
                    <Box>
                      <Stack direction="row" spacing={1} alignItems="center" mb={1}>
                        <DescriptionIcon color="action" />
                        <Typography variant="subtitle2">
                          Description
                        </Typography>
                      </Stack>
                      <Typography variant="body1" color="text.secondary">
                        {product.description}
                      </Typography>
                    </Box>
                  )}

                  {product.score !== null && (
                    <Box>
                      <Typography variant="subtitle2" gutterBottom>
                        AI Score
                      </Typography>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Rating 
                          value={product.score / 20} 
                          precision={0.5} 
                          readOnly 
                        />
                        <Typography variant="body2" color="text.secondary">
                          ({product.score}/100)
                        </Typography>
                      </Stack>
                    </Box>
                  )}

                  <Divider />

                  <Stack direction="row" spacing={2}>
                    {product.score === null ? (
                      <Button
                        variant="contained"
                        size="large"
                        onClick={handleTransform}
                        disabled={transforming}
                        startIcon={transforming ? <CircularProgress size={20} /> : <AutoFixHighIcon />}
                        sx={{
                          py: 1.5,
                          borderRadius: 2,
                          backgroundColor: 'primary.dark',
                          '&:hover': { backgroundColor: 'primary.main' }
                        }}
                      >
                        {transforming ? 'Transforming...' : 'Transform Product'}
                      </Button>
                    ) : (
                      <Button
                        variant="contained"
                        size="large"
                        color="success"
                        onClick={handleSave}
                        disabled={saving}
                        startIcon={saving ? <CircularProgress size={20} /> : <SaveIcon />}
                        sx={{
                          py: 1.5,
                          borderRadius: 2
                        }}
                      >
                        {saving ? 'Saving...' : 'Save Product'}
                      </Button>
                    )}
                  </Stack>
                </Stack>
              </Grid>
            </Grid>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  );
};

export default ProductDetail; 