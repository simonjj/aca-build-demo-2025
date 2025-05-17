import React from 'react';
import { Card, CardContent, CardMedia, Typography, Box, Chip } from '@mui/material';
import { styled } from '@mui/material/styles';

const StyledCard = styled(Card)(({ theme, isSelected }) => ({
  cursor: 'pointer',
  transition: 'transform 0.2s',
  '&:hover': {
    transform: 'scale(1.05)',
  },
  border: isSelected ? `2px solid ${theme.palette.primary.main}` : 'none',
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
      <CardMedia
        component="div"
        sx={{
          height: 200,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '5rem',
          backgroundColor: '#f5f5f5',
        }}
      >
        {pet.emoji}
      </CardMedia>
      <CardContent>
        <Typography variant="h5" component="div" gutterBottom>
          {pet.name}
        </Typography>
        {state && (
          <Box sx={{ mt: 2 }}>
            <Chip
             label={`Mood: ${getMoodEmoji(state.mood)} (${state.mood || 'none'})`}
             color="primary"
             variant="outlined"
             sx={{ mr: 1 }}
            />
            <Chip
              label={`Energy: ${state.energy}%`}
              color="secondary"
              variant="outlined"
            />
          </Box>
        )}
      </CardContent>
    </StyledCard>
  );
};

export default PetCard; 