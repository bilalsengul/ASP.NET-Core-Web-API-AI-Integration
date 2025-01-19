import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProductBySku, transformProduct, saveProduct, Product } from '../services/api';
import {
  Container,
  Typography,
  Box,
  CircularProgress,
  Paper,
  Grid,
  Button,
  Chip,
  Divider,
  Stack
} from '@mui/material';

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
    try {
      const transformedProduct = await transformProduct(sku);
      setProduct(transformedProduct);
      setError(null);
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
      const savedProduct = await saveProduct(product);
      setProduct(savedProduct);
      setError(null);
      // Navigate to products list after saving
      navigate('/');
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

  if (error) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  if (!product) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <Typography>Product not found</Typography>
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper elevation={3} sx={{ p: 3 }}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="h4" component="h1" gutterBottom>
                {product.name}
              </Typography>
              <Stack direction="row" spacing={2}>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={handleTransform}
                  disabled={transforming || saving}
                >
                  {transforming ? 'Transforming...' : 'Transform Product'}
                </Button>
                <Button
                  variant="contained"
                  color="success"
                  onClick={handleSave}
                  disabled={transforming || saving}
                >
                  {saving ? 'Saving...' : 'Save Product'}
                </Button>
              </Stack>
            </Box>
          </Grid>

          <Grid item xs={12}>
            <Divider />
          </Grid>

          <Grid item xs={12} md={8}>
            <Typography variant="h6" gutterBottom>Description</Typography>
            <Typography paragraph>
              {product.description || 'No description available'}
            </Typography>

            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>Details</Typography>
            <Box sx={{ mb: 2 }}>
              <Typography><strong>Brand:</strong> {product.brand}</Typography>
              <Typography><strong>SKU:</strong> {product.sku}</Typography>
              <Typography><strong>Category:</strong> {product.category || 'Uncategorized'}</Typography>
            </Box>

            {product.attributes.length > 0 && (
              <Box sx={{ mb: 2 }}>
                <Typography variant="h6" gutterBottom>Attributes</Typography>
                <Box display="flex" gap={1} flexWrap="wrap">
                  {product.attributes.map((attr, index) => (
                    <Chip key={index} label={`${attr.name}: ${attr.value}`} />
                  ))}
                </Box>
              </Box>
            )}
          </Grid>

          <Grid item xs={12} md={4}>
            <Paper elevation={2} sx={{ p: 2, mb: 2 }}>
              <Typography variant="h6" gutterBottom>Pricing</Typography>
              <Typography variant="h4" color="primary" gutterBottom>
                ${(product.discountedPrice / 100).toFixed(2)}
              </Typography>
              {product.originalPrice !== product.discountedPrice && (
                <Typography variant="body2" sx={{ textDecoration: 'line-through' }}>
                  Original: ${(product.originalPrice / 100).toFixed(2)}
                </Typography>
              )}
            </Paper>

            {product.score !== null && (
              <Paper elevation={2} sx={{ p: 2 }}>
                <Typography variant="h6" gutterBottom>AI Score</Typography>
                <Box display="flex" alignItems="center" gap={2}>
                  <CircularProgress
                    variant="determinate"
                    value={product.score}
                    size={60}
                  />
                  <Typography variant="h4">{product.score}</Typography>
                </Box>
              </Paper>
            )}
          </Grid>
        </Grid>
      </Paper>
    </Container>
  );
};

export default ProductDetail; 