import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProductBySku, getProductVariants, transformProduct, saveProduct, Product, getErrorMessage } from '../services/api';
import {
  Container,
  Typography,
  Box,
  Button,
  CircularProgress,
  Alert,
  Stack,
  Rating,
  Grid,
  ImageList,
  ImageListItem,
  List,
  ListItem,
  Card,
  CardMedia,
  Breadcrumbs,
  Link
} from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import FavoriteIcon from '@mui/icons-material/Favorite';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';

interface ProductVariants {
  colors: string[];
  sizes: string[];
  variants: Product[];
}

const ProductDetail: React.FC = () => {
  const { sku } = useParams<{ sku: string }>();
  const navigate = useNavigate();
  const [product, setProduct] = useState<Product | null>(null);
  const [variants, setVariants] = useState<ProductVariants | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [transforming, setTransforming] = useState(false);
  const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);
  const [selectedColor, setSelectedColor] = useState<string | null>(null);
  const [selectedSize, setSelectedSize] = useState<string | null>(null);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);
  const [transformState, setTransformState] = useState<'initial' | 'transformed'>('initial');

  useEffect(() => {
    const fetchData = async () => {
      if (!sku) return;
      
      try {
        setLoading(true);
        setErrorMessage(null);
        const [productData, variantsData] = await Promise.all([
          getProductBySku(sku),
          getProductVariants(sku)
        ]);
        setProduct(productData);
        setVariants(variantsData);
      } catch (error) {
        setErrorMessage(getErrorMessage(error));
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [sku]);

  // Update displayed product when a variant is selected
  useEffect(() => {
    if (selectedVariant) {
      // Keep the original product data but update variant-specific fields
      setProduct(prev => prev ? {
        ...prev,
        color: selectedVariant.color || prev.color,
        size: selectedVariant.size || prev.size,
        sku: selectedVariant.sku,
        discountedPrice: selectedVariant.discountedPrice || prev.discountedPrice,
        originalPrice: selectedVariant.originalPrice || prev.originalPrice,
        images: selectedVariant.images?.length ? selectedVariant.images : prev.images
      } : null);
      setCurrentImageIndex(0); // Reset image index when variant changes
    }
  }, [selectedVariant]);

  const handleColorChange = (color: string) => {
    setSelectedColor(color);
    // Find variant with selected color and current size
    const matchingVariant = variants?.variants.find(
      (v: Product) => v.color === color && (selectedSize ? v.size === selectedSize : true)
    );
    setSelectedVariant(matchingVariant || null);
  };

  const handleSizeChange = (size: string) => {
    setSelectedSize(size);
    // Find variant with current color and selected size
    const matchingVariant = variants?.variants.find(
      (v: Product) => (selectedColor ? v.color === selectedColor : true) && v.size === size
    );
    setSelectedVariant(matchingVariant || null);
  };

  const handleTransform = async () => {
    if (!sku) return;
    
    try {
      setTransforming(true);
      setErrorMessage(null);
      const transformedProduct = await transformProduct(sku);
      setProduct(transformedProduct);
      setTransformState('transformed');
    } catch (error) {
      setErrorMessage(getErrorMessage(error));
    } finally {
      setTransforming(false);
    }
  };

  const handleSave = async () => {
    if (!product) return;
    
    try {
      setSaving(true);
      setErrorMessage(null);
      const savedProduct = await saveProduct(product);
      setProduct(savedProduct);
      setTransformState('initial'); // Reset state after saving
    } catch (error) {
      setErrorMessage(getErrorMessage(error));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Container>
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (errorMessage) {
    return (
      <Container>
        <Alert severity="error" sx={{ mt: 2 }}>
          {errorMessage}
        </Alert>
      </Container>
    );
  }

  if (!product) {
    return (
      <Container>
        <Alert severity="warning" sx={{ mt: 2 }}>
          Product not found
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Breadcrumbs separator={<NavigateNextIcon fontSize="small" />} aria-label="breadcrumb" sx={{ mb: 2 }}>
        <Link color="inherit" href="/" onClick={(e) => { e.preventDefault(); navigate('/'); }}>
          Home
        </Link>
        <Link color="inherit" href="#" onClick={(e) => e.preventDefault()}>
          {product.category || 'Products'}
        </Link>
        <Typography color="text.primary">{product.name}</Typography>
      </Breadcrumbs>

      <Grid container spacing={4}>
        {/* Left Column - Images */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardMedia
              component="img"
              image={product.images[currentImageIndex]}
              alt={product.name}
              sx={{ width: '100%', height: 'auto', objectFit: 'contain' }}
            />
          </Card>
          {product.images.length > 1 && (
            <ImageList sx={{ mt: 2 }} cols={4} rowHeight={100}>
              {product.images.map((image, index) => (
                <ImageListItem 
                  key={index}
                  onClick={() => setCurrentImageIndex(index)}
                  sx={{ 
                    cursor: 'pointer',
                    border: index === currentImageIndex ? '2px solid #1976d2' : 'none',
                    borderRadius: 1
                  }}
                >
                  <img
                    src={image}
                    alt={`${product.name} - ${index + 1}`}
                    loading="lazy"
                    style={{ height: '100%', width: '100%', objectFit: 'contain' }}
                  />
                </ImageListItem>
              ))}
            </ImageList>
          )}
        </Grid>

        {/* Right Column - Product Details */}
        <Grid item xs={12} md={6}>
          <Stack spacing={3}>
            <Box>
              <Typography variant="h4" gutterBottom>
                {product.name}
              </Typography>
              <Typography variant="subtitle1" color="text.secondary" gutterBottom>
                {product.brand}
              </Typography>
            </Box>

            <Box display="flex" alignItems="center" gap={2}>
              <Rating value={product.score || 0} readOnly precision={0.5} />
              <Typography variant="body2" color="text.secondary">
                ({product.ratingCount} reviews)
              </Typography>
              <Box display="flex" alignItems="center" gap={0.5}>
                <FavoriteIcon color="error" fontSize="small" />
                <Typography variant="body2" color="text.secondary">
                  {product.favoriteCount} favorites
                </Typography>
              </Box>
            </Box>

            <Box>
              <Typography variant="h5" color="error" gutterBottom>
                ₺{product.discountedPrice.toFixed(2)}
              </Typography>
              {product.originalPrice > product.discountedPrice && (
                <Typography variant="body1" color="text.secondary" sx={{ textDecoration: 'line-through' }}>
                  ₺{product.originalPrice.toFixed(2)}
                </Typography>
              )}
            </Box>

            {/* Color Selection */}
            {variants?.colors && variants.colors.length > 0 && (
              <Box>
                <Typography variant="subtitle1" gutterBottom>
                  Colors
                </Typography>
                <Stack direction="row" spacing={1}>
                  {variants.colors.map((color) => (
                    <Button
                      key={color}
                      variant={selectedColor === color ? 'contained' : 'outlined'}
                      onClick={() => handleColorChange(color)}
                      sx={{ minWidth: 'auto', textTransform: 'none' }}
                    >
                      {color}
                    </Button>
                  ))}
                </Stack>
              </Box>
            )}

            {/* Size Selection */}
            {variants?.sizes && variants.sizes.length > 0 && (
              <Box>
                <Typography variant="subtitle1" gutterBottom>
                  Sizes
                </Typography>
                <Stack direction="row" spacing={1}>
                  {variants.sizes.map((size) => (
                    <Button
                      key={size}
                      variant={selectedSize === size ? 'contained' : 'outlined'}
                      onClick={() => handleSizeChange(size)}
                      sx={{ minWidth: 'auto', textTransform: 'none' }}
                    >
                      {size}
                    </Button>
                  ))}
                </Stack>
              </Box>
            )}

            <Box>
              <Typography variant="subtitle1" gutterBottom>
                Shipping
              </Typography>
              <Stack direction="row" spacing={1} alignItems="center">
                <LocalShippingIcon color={product.hasFastShipping ? 'success' : 'inherit'} />
                <Typography variant="body2">
                  {product.shippingInfo}
                </Typography>
              </Stack>
            </Box>

            {product.paymentOptions && product.paymentOptions.length > 0 && (
              <Box>
                <Typography variant="subtitle1" gutterBottom>
                  Payment Options
                </Typography>
                <List dense>
                  {product.paymentOptions.map((option, index) => (
                    <ListItem key={index}>
                      <Typography variant="body2">{option}</Typography>
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            <Box>
              <Typography variant="subtitle1" gutterBottom>
                Description
              </Typography>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                {product.description}
              </Typography>
            </Box>

            <Stack direction="row" spacing={2}>
              <Button
                variant="contained"
                startIcon={<AutoFixHighIcon />}
                onClick={handleTransform}
                disabled={transforming || transformState === 'transformed'}
              >
                {transforming ? 'Transforming...' : 'Transform'}
              </Button>
              <Button
                variant="contained"
                color="success"
                startIcon={<SaveIcon />}
                onClick={handleSave}
                disabled={saving || transformState === 'initial'}
              >
                {saving ? 'Saving...' : 'Save Changes'}
              </Button>
            </Stack>
          </Stack>
        </Grid>
      </Grid>
    </Container>
  );
};

export default ProductDetail;