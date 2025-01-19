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
  Rating,
  Divider,
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
import ShoppingBagIcon from '@mui/icons-material/ShoppingBag';
import SaveIcon from '@mui/icons-material/Save';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import FavoriteIcon from '@mui/icons-material/Favorite';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';

const ProductDetail: React.FC = () => {
  const { sku } = useParams<{ sku: string }>();
  const navigate = useNavigate();
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [transforming, setTransforming] = useState(false);
  const [saving, setSaving] = useState(false);
  const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  useEffect(() => {
    const fetchProduct = async () => {
      if (!sku) return;
      
      try {
        const data = await getProductBySku(sku);
        setProduct(data);
        if (data.variants && data.variants.length > 0) {
          setSelectedVariant(data.variants[0]);
        }
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

  const getAttributeValue = (name: string): string | null => {
    return product?.attributes.find(attr => attr.name === name)?.value || null;
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

  const displayedProduct = selectedVariant || product;
  const allImages = displayedProduct.images || [];
  const reviewCount = getAttributeValue('review_count');
  const favoriteCount = getAttributeValue('favorite_count');
  const deliveryInfo = getAttributeValue('delivery_info');
  const specifications = product.attributes.filter(attr => 
    !['review_count', 'favorite_count', 'delivery_info', 'features'].includes(attr.name)
  );
  const features = getAttributeValue('features')?.split('|').filter(f => f.trim()) || [];

  return (
    <Container maxWidth="lg" sx={{ mt: 2, mb: 4 }}>
      <Breadcrumbs 
        separator={<NavigateNextIcon fontSize="small" />} 
        sx={{ mb: 2 }}
      >
        <Link color="inherit" href="/">Home</Link>
        <Link color="inherit" href="#">{product?.category || 'Category'}</Link>
        <Typography color="text.primary">{product?.name}</Typography>
      </Breadcrumbs>

      <Paper elevation={0} sx={{ p: 3, backgroundColor: 'transparent' }}>
        <Grid container spacing={4}>
          {/* Left Column - Images */}
          <Grid item xs={12} md={6}>
            <Box sx={{ position: 'relative', mb: 2 }}>
              {allImages.length > 0 ? (
                <img
                  src={allImages[currentImageIndex]}
                  alt={displayedProduct.name}
                  style={{ 
                    width: '100%', 
                    height: 'auto', 
                    maxHeight: '600px',
                    objectFit: 'contain',
                    backgroundColor: '#f8f8f8',
                    borderRadius: '8px'
                  }}
                />
              ) : (
                <Box
                  sx={{
                    height: 600,
                    backgroundColor: '#f8f8f8',
                    borderRadius: '8px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                  }}
                >
                  <ShoppingBagIcon sx={{ fontSize: 64, color: 'text.secondary' }} />
                </Box>
              )}
            </Box>
            
            {allImages.length > 1 && (
              <ImageList sx={{ width: '100%', height: 100 }} cols={6} rowHeight={100}>
                {allImages.map((img, index) => (
                  <ImageListItem 
                    key={index}
                    sx={{ 
                      cursor: 'pointer',
                      border: index === currentImageIndex ? '2px solid' : '1px solid',
                      borderColor: index === currentImageIndex ? '#f27a1a' : '#e5e5e5',
                      borderRadius: '4px',
                      overflow: 'hidden'
                    }}
                    onClick={() => setCurrentImageIndex(index)}
                  >
                    <img
                      src={img}
                      alt={`View ${index + 1}`}
                      style={{ height: '100%', width: '100%', objectFit: 'cover' }}
                    />
                  </ImageListItem>
                ))}
              </ImageList>
            )}
          </Grid>

          {/* Right Column - Product Info */}
          <Grid item xs={12} md={6}>
            <Stack spacing={3}>
              <Box>
                <Typography variant="h5" component="h1" gutterBottom>
                  {displayedProduct.brand}
                </Typography>
                <Typography variant="body1" color="text.secondary" gutterBottom>
                  {displayedProduct.name}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  SKU: {displayedProduct.sku}
                </Typography>
              </Box>

              <Stack direction="row" spacing={2} alignItems="center">
                {reviewCount && (
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <Rating 
                      value={3.7} 
                      precision={0.1} 
                      readOnly 
                      size="small"
                    />
                    <Typography variant="body2" color="text.secondary">
                      ({reviewCount} Reviews)
                    </Typography>
                  </Stack>
                )}
                
                {favoriteCount && (
                  <Stack direction="row" alignItems="center" spacing={0.5}>
                    <FavoriteIcon sx={{ fontSize: 16, color: '#e81224' }} />
                    <Typography variant="body2" color="text.secondary">
                      {favoriteCount}
                    </Typography>
                  </Stack>
                )}
              </Stack>

              <Box>
                <Typography variant="h4" color="#f27a1a" fontWeight="bold">
                  {(displayedProduct.discountedPrice / 100).toLocaleString('tr-TR', {
                    style: 'currency',
                    currency: 'TRY'
                  })}
                </Typography>
                {displayedProduct.originalPrice !== displayedProduct.discountedPrice && (
                  <Typography 
                    variant="h6" 
                    color="text.secondary" 
                    sx={{ textDecoration: 'line-through' }}
                  >
                    {(displayedProduct.originalPrice / 100).toLocaleString('tr-TR', {
                      style: 'currency',
                      currency: 'TRY'
                    })}
                  </Typography>
                )}
              </Box>

              {deliveryInfo && (
                <Paper 
                  variant="outlined" 
                  sx={{ 
                    p: 2, 
                    borderRadius: 2,
                    borderColor: '#e5e5e5',
                    backgroundColor: '#f8f8f8'
                  }}
                >
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <LocalShippingIcon sx={{ color: '#f27a1a' }} />
                    <Typography variant="body2">
                      {deliveryInfo}
                    </Typography>
                  </Stack>
                </Paper>
              )}

              {product.variants && product.variants.length > 0 && (
                <Box>
                  <Typography variant="subtitle1" gutterBottom fontWeight="medium">
                    Available Colors
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap" gap={1}>
                    {product.variants.map((variant) => (
                      <Card
                        key={variant.sku}
                        sx={{
                          width: 64,
                          height: 64,
                          cursor: 'pointer',
                          border: selectedVariant?.sku === variant.sku ? '2px solid #f27a1a' : '1px solid #e5e5e5',
                          borderRadius: 1,
                          overflow: 'hidden'
                        }}
                        onClick={() => setSelectedVariant(variant)}
                      >
                        <CardMedia
                          component="img"
                          height="64"
                          image={variant.images[0] || ''}
                          alt={variant.color || ''}
                        />
                      </Card>
                    ))}
                  </Stack>
                </Box>
              )}

              <Stack direction="row" spacing={2}>
                {displayedProduct.score === null ? (
                  <Button
                    fullWidth
                    variant="contained"
                    size="large"
                    onClick={handleTransform}
                    disabled={transforming}
                    startIcon={transforming ? <CircularProgress size={20} /> : <AutoFixHighIcon />}
                    sx={{
                      py: 1.5,
                      backgroundColor: '#f27a1a',
                      '&:hover': { backgroundColor: '#d65a00' }
                    }}
                  >
                    {transforming ? 'Transforming...' : 'Transform Product'}
                  </Button>
                ) : (
                  <Button
                    fullWidth
                    variant="contained"
                    size="large"
                    color="success"
                    onClick={handleSave}
                    disabled={saving}
                    startIcon={saving ? <CircularProgress size={20} /> : <SaveIcon />}
                    sx={{
                      py: 1.5,
                      backgroundColor: '#f27a1a',
                      '&:hover': { backgroundColor: '#d65a00' }
                    }}
                  >
                    {saving ? 'Saving...' : 'Save Product'}
                  </Button>
                )}
              </Stack>

              <Divider />

              {features.length > 0 && (
                <Box sx={{ mt: 4 }}>
                  <Typography variant="h6" gutterBottom fontWeight="medium" color="#333">
                    Ürün Özellikleri
                  </Typography>
                  <Paper 
                    variant="outlined" 
                    sx={{ 
                      p: 3,
                      borderRadius: 2,
                      borderColor: '#e5e5e5',
                      backgroundColor: '#fff'
                    }}
                  >
                    <Grid container spacing={2}>
                      {features.map((feature, index) => (
                        <Grid item xs={12} sm={6} key={index}>
                          <Stack 
                            direction="row" 
                            spacing={2} 
                            alignItems="flex-start"
                            sx={{
                              p: 1,
                              borderRadius: 1,
                              '&:hover': {
                                backgroundColor: '#f8f8f8'
                              }
                            }}
                          >
                            <Box
                              sx={{
                                width: 6,
                                height: 6,
                                borderRadius: '50%',
                                backgroundColor: '#f27a1a',
                                mt: 1
                              }}
                            />
                            <Typography 
                              variant="body2" 
                              color="text.primary"
                              sx={{ 
                                flex: 1,
                                lineHeight: 1.6
                              }}
                            >
                              {feature.trim()}
                            </Typography>
                          </Stack>
                        </Grid>
                      ))}
                    </Grid>
                  </Paper>
                </Box>
              )}

              {specifications.length > 0 && (
                <Box sx={{ mt: 4 }}>
                  <Typography variant="h6" gutterBottom fontWeight="medium" color="#333">
                    Ürün Bilgileri
                  </Typography>
                  <Paper 
                    variant="outlined" 
                    sx={{ 
                      borderRadius: 2,
                      borderColor: '#e5e5e5',
                      backgroundColor: '#fff'
                    }}
                  >
                    <List disablePadding>
                      {specifications.map((spec, index) => (
                        <ListItem 
                          key={index} 
                          sx={{
                            py: 2,
                            px: 3,
                            borderBottom: index < specifications.length - 1 ? '1px solid #e5e5e5' : 'none',
                            '&:hover': {
                              backgroundColor: '#f8f8f8'
                            }
                          }}
                        >
                          <Grid container spacing={2}>
                            <Grid item xs={12} sm={4}>
                              <Typography 
                                variant="body2" 
                                color="text.secondary"
                                sx={{ fontWeight: 500 }}
                              >
                                {spec.name}
                              </Typography>
                            </Grid>
                            <Grid item xs={12} sm={8}>
                              <Typography variant="body2" color="text.primary">
                                {spec.value}
                              </Typography>
                            </Grid>
                          </Grid>
                        </ListItem>
                      ))}
                    </List>
                  </Paper>
                </Box>
              )}

              {displayedProduct.description && (
                <Box>
                  <Typography variant="subtitle1" gutterBottom fontWeight="medium">
                    Description
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                    {displayedProduct.description}
                  </Typography>
                </Box>
              )}
            </Stack>
          </Grid>
        </Grid>
      </Paper>
    </Container>
  );
};

export default ProductDetail; 