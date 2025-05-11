import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Grid,
  TextField,
  LinearProgress,
  Snackbar,
  Alert,
} from '@mui/material';
import PetsIcon from '@mui/icons-material/Pets';
import RestaurantIcon from '@mui/icons-material/Restaurant';
import TouchAppIcon from '@mui/icons-material/TouchApp';
import MusicNoteIcon from '@mui/icons-material/MusicNote';
import ChatIcon from '@mui/icons-material/Chat';
import { getApiUrl } from '../utils/apiConfig.js';
import { tracer } from '../telemetry';    // ← add this

// create a counter for interactions
const interactionCounter = meter.createCounter('pet_interactions', {
  description: 'Counts pet/ poke / feed / sing events'
});


const PetInteraction = ({ pet, socket, state, onStateUpdate }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleInteraction = async (action, message = null) => {
    const span = tracer.startSpan(`ui.pet.${action}`, {
      attributes: { pet: pet.name }
    });
    interactionCounter.add(1, { action, pet: pet.name });
    try {
      setLoading(true);
      
      // Get the appropriate API URL based on pet type
      const apiUrl = getApiUrl(pet.type);
      
      // Create the request payload
      const payload = {
        action,
        ...(message && { message })
      };
      
      // Make the API call to the appropriate backend service
      const response = await fetch(`${apiUrl}/pet/interact`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });
      
      if (!response.ok) {
        throw new Error(`Failed to interact with ${pet.name}: ${response.statusText}`);
      }
      
      const updatedState = await response.json();
      
      // Update the pet state in the parent component
      if (onStateUpdate) {
        onStateUpdate(updatedState);
      }
      
      // Also emit the socket event for real-time updates if socket is available
      if (socket && socket.connected) {
        socket.emit('petInteraction', {
          petId: pet.id,
          action,
          ...(message && { message })
        });
      }
      
      setLoading(false);
    } catch (err) {
      console.error('Pet interaction failed:', err);
      setError(`Failed to interact with ${pet.name}. Please try again.`);
      setLoading(false);
    } finally{
      span.end();  // End the span after the interaction is complete
    }
  };

  const handleMessage = (event) => {
    if (event.key === 'Enter') {
      handleInteraction('message', event.target.value);
      event.target.value = '';
    }
  };

  const closeError = () => {
    setError(null);
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
          
          <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
            Happiness
          </Typography>
          <LinearProgress
            variant="determinate"
            value={state.happiness}
            color="success"
            sx={{ height: 10, borderRadius: 5 }}
          />
          
          <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
            Chaos
          </Typography>
          <LinearProgress
            variant="determinate"
            value={state.chaos}
            color="error"
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
            disabled={loading}
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
            disabled={loading}
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
            disabled={loading}
            color="warning"
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
            disabled={loading}
            color="info"
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
          disabled={loading}
          InputProps={{
            startAdornment: <ChatIcon sx={{ mr: 1, color: 'action.active' }} />,
          }}
        />
      </Box>

      {state?.lastMessage && (
        <Box sx={{ mt: 2, p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {pet.name} says: "{state.lastMessage}"
          </Typography>
        </Box>
      )}
      
      <Snackbar open={!!error} autoHideDuration={6000} onClose={closeError}>
        <Alert onClose={closeError} severity="error">
          {error}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default PetInteraction;