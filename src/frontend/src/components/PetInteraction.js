import React from 'react';
import {
  Box,
  Typography,
  Button,
  Grid,
  TextField,
  LinearProgress,
} from '@mui/material';
import PetsIcon from '@mui/icons-material/Pets';
import RestaurantIcon from '@mui/icons-material/Restaurant';
import TouchAppIcon from '@mui/icons-material/TouchApp';
import MusicNoteIcon from '@mui/icons-material/MusicNote';
import ChatIcon from '@mui/icons-material/Chat';

const PetInteraction = ({ pet, socket, state }) => {
  const handleInteraction = (action) => {
    socket.emit('petInteraction', {
      petId: pet.id,
      action,
    });
  };

  const handleMessage = (event) => {
    if (event.key === 'Enter') {
      socket.emit('petInteraction', {
        petId: pet.id,
        action: 'message',
        message: event.target.value,
      });
      event.target.value = '';
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {pet.emoji} {pet.name}
      </Typography>

      {state && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle1" gutterBottom>
            Energy Level
          </Typography>
          <LinearProgress
            variant="determinate"
            value={state.energy}
            sx={{ height: 10, borderRadius: 5 }}
          />
        </Box>
      )}

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} sm={4}>
          <Button
            fullWidth
            variant="contained"
            startIcon={<PetsIcon />}
            onClick={() => handleInteraction('pet')}
          >
            Pet
          </Button>
        </Grid>
        <Grid item xs={6} sm={4}>
          <Button
            fullWidth
            variant="contained"
            startIcon={<RestaurantIcon />}
            onClick={() => handleInteraction('feed')}
          >
            Feed
          </Button>
        </Grid>
        <Grid item xs={6} sm={4}>
          <Button
            fullWidth
            variant="contained"
            startIcon={<TouchAppIcon />}
            onClick={() => handleInteraction('poke')}
          >
            Poke
          </Button>
        </Grid>
        <Grid item xs={6} sm={4}>
          <Button
            fullWidth
            variant="contained"
            startIcon={<MusicNoteIcon />}
            onClick={() => handleInteraction('sing')}
          >
            Sing To
          </Button>
        </Grid>
      </Grid>

      <Box>
        <Typography variant="subtitle1" gutterBottom>
          Send a message to {pet.name}:
        </Typography>
        <TextField
          fullWidth
          variant="outlined"
          placeholder={`Type a message for ${pet.name}...`}
          onKeyPress={handleMessage}
          InputProps={{
            startAdornment: <ChatIcon sx={{ mr: 1, color: 'action.active' }} />,
          }}
        />
      </Box>

      {state?.lastMessage && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" color="text.secondary">
            {pet.name} says: "{state.lastMessage}"
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default PetInteraction; 