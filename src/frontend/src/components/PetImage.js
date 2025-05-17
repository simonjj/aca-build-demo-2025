import React, { useState, useEffect } from 'react';
import { Box, Typography } from '@mui/material';

/**
 * PetImage component for displaying pet images with fallback to emoji
 * Following Azure Container Apps best practices for responsive image display
 * @param {Object} props - Component props
 * @param {Object} props.pet - Pet object containing image and emoji
 * @param {number} [props.size=140] - Base size for the image in pixels
 * @param {string} [props.maxHeight] - Optional maximum height constraint
 */
const PetImage = ({ pet, size = 240, maxHeight }) => {
  const [hasError, setHasError] = useState(false);

   // Add image preloading for performance
   useEffect(() => {
    if (pet.image) {
      const img = new Image();
      img.src = pet.image;
    }
  }, [pet.image]);
  
  
  // If no image or error loading, show emoji
  if (!pet.image || hasError) {
    return (
      <Typography 
        variant="h1" 
        component="div" 
        sx={{ 
          fontSize: size / 16 + 'rem', // Adjusted for better proportions
          height: '100%',
          width: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}
        aria-label={`${pet.name} emoji`}
      >
        {pet.emoji}
      </Typography>
    );
  }
  
  return (
    <Box
      component="img"
      src={pet.image}
      alt={pet.name}
      loading="lazy"
      sx={{
        width: '80%', // Auto width for proper aspect ratio
        height: '80%', // Auto height for proper aspect ratio
        maxWidth: 'unset',
        maxHeight: 'unset',
        objectFit: 'contain',
        borderRadius: '8px',
        transition: 'transform 0.3s ease',
        '@media (-webkit-min-device-pixel-ratio: 2), (min-resolution: 192dpi)': {
          imageRendering: 'crisp-edges',
        },
        '&:hover': {
          transform: 'scale(1.05)',
        }
      }}
      onError={() => setHasError(true)}
    />
  );
};

export default PetImage;