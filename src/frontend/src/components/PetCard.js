import React from 'react';
import { Card, CardContent, CardActionArea, CardMedia, Typography, Box, Chip } from '@mui/material';
import { styled } from '@mui/material/styles';
import PetImage from './PetImage';

const StyledCard = styled(Card)(({ theme, isSelected }) => ({
  cursor: 'pointer',
  transition: 'transform 0.2s',
  '&:hover': {
    transform: 'scale(1.05)',
  },
  border: isSelected ? `2px solid ${theme.palette.primary.main}` : 'none',
}));

// Update the image container to maximize available space
// Update the image container to maximize available space
const PetImageContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
  margin: 0, // Remove margin to maximize space
  padding: 0, // Remove padding to maximize space
  width: '100%', // Take full width of the card
  height: '300px', // Increase height significantly
  overflow: 'hidden', // Prevent overflow
  [theme.breakpoints.up('md')]: {
    height: '340px', // Even taller on medium screens
  },
  [theme.breakpoints.up('lg')]: {
    height: '380px', // Maximum height on large screens
  },
  [theme.breakpoints.down('sm')]: {
    height: '260px', // Still taller on small screens
  },
}));

const PetCard = ({ pet, isSelected, onClick, state }) => {
  const getMoodEmoji = (mood) => {
    // Azure best practice: Use case-insensitive comparison for mood states
    const normalizedMood = mood?.toLowerCase();
    
    switch (normalizedMood) {
      case 'happy': return '😊';
      case 'content': return '😌';
      case 'furious': return '😡';
      case 'angry': return '😠';
      case 'sad': return '😢';
      case 'sleepy': return '😴';
      case 'energetic': return '⚡';
      case 'hungry': return '🍔';
      case 'tired': return '😩';
      case 'hyperactive': return '😜';
      case 'playful': return '🎉';
      case 'bored': return '😒';
      case 'curious': return '🤔';
      case 'excited': return '🤩';
      case 'calm': return '😌';
      case 'chaotic': return '😵';
      case 'enraged': return '😤';
      case 'lethargic': return '😴';
      case 'content': return '😊';
      case 'relaxed': return '😌';
      case 'unpredictable': return '🤯';
      case 'hiding': return '🙈';
      case 'anxious': return '😰';
      case 'agitated': return '😠';
      case 'sleepy': return '😴';
      case 'nervous': return '😬';
      case 'angry': return '😡';
      default: return '😐';
    }
  };
  return (
    <StyledCard isSelected={isSelected} onClick={onClick}>
      <CardActionArea>
        <CardContent sx={{ textAlign: 'center', padding: '8px', paddingBottom: '8px', display: 'flex', flexDirection: 'column', alignItems: 'center', justicyContent:'space-between', height: '100%', '@media (-webkit-min-device-pixel-ratio: 2), (min-resolution: 192dpi)': {imageRendering: 'crisp-edges',} }}>
          <PetImageContainer>
            <PetImage pet ={pet} size={140} maxHeight="100%"/>
          </PetImageContainer>
          <Typography variant="h6" component="div" gutterBottom>
            {pet.name}
          </Typography>
          
          {state?.mood && (
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mt: 1 }}>
              <Typography variant="body2" color="text.secondary">
                {state.mood} {getMoodEmoji(state.mood)}
              </Typography>
            </Box>
          )}
        </CardContent>
      </CardActionArea>
    </StyledCard>
  );
};

export default PetCard; 