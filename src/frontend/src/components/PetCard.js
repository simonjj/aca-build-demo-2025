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
    switch (mood) {
      case 'happy': return '😊';
      case 'sad': return '😢';
      case 'angry': return '😠';
      case 'sleepy': return '😴';
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
              label={`Mood: ${getMoodEmoji(state.mood)}`}
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