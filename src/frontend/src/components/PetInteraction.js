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

const PetInteraction = ({ pet, socket, state, onStateUpdate }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleInteraction = async (action, message = null) => {
    const span = tracer.startSpan(`ui.pet.${action}`, {
      attributes: { pet: pet.name }
    });
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
    {/* Energy Level with unbounded visualization */}
    <Typography variant="subtitle1" gutterBottom>
      Energy Level: {state.energy} {state.energy > 100 ? "⚡" : ""}
    </Typography>
    <Box sx={{ display: 'flex', alignItems: 'center' }}>
      <Box sx={{ width: '100%', mr: 1 }}>
        <LinearProgress
          variant="determinate"
          value={Math.min(100, state.energy)}
          color={state.energy > 100 ? "secondary" : "primary"}
          sx={{ 
            height: 10, 
            borderRadius: 5,
            '& .MuiLinearProgress-bar': {
              backgroundColor: state.energy > 100 ? '#aa00ff' : undefined,
            }
          }}
        />
      </Box>
      <Box sx={{ minWidth: 45 }}>
        <Typography 
          variant="body2" 
          color={state.energy > 100 ? "secondary" : "text.secondary"} 
          fontWeight={state.energy > 100 ? "bold" : "normal"}
        >
          {state.energy}%
        </Typography>
      </Box>
    </Box>
    {state.energy > 100 && (
      <Typography variant="caption" color="secondary">
        Energy overflowing! Your pet is supercharged.
      </Typography>
    )}
    
    {/* Happiness Level with unbounded visualization */}
    <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
      Happiness Level: {state.happiness} {state.happiness > 100 ? "🎉" : ""}
    </Typography>
    <Box sx={{ display: 'flex', alignItems: 'center' }}>
      <Box sx={{ width: '100%', mr: 1 }}>
        <LinearProgress
          variant="determinate"
          value={Math.min(100, state.happiness)}
          color={state.happiness > 100 ? "success" : "success"}
          sx={{ 
            height: 10, 
            borderRadius: 5,
            '& .MuiLinearProgress-bar': {
              backgroundColor: state.happiness > 100 ? '#00c853' : undefined,
            }
          }}
        />
      </Box>
      <Box sx={{ minWidth: 45 }}>
        <Typography 
          variant="body2" 
          color={state.happiness > 100 ? "success.main" : "text.secondary"} 
          fontWeight={state.happiness > 100 ? "bold" : "normal"}
        >
          {state.happiness}%
        </Typography>
      </Box>
    </Box>
    {state.happiness > 100 && (
      <Typography variant="caption" color="success.main">
        Extraordinary happiness! Your pet is in bliss.
      </Typography>
    )}
    
    {/* Chaos Level with unbounded visualization */}
    <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
      Chaos Level: {state.chaos} {state.chaos > 100 ? "🔥" : ""}
    </Typography>
    <Box sx={{ display: 'flex', alignItems: 'center' }}>
      <Box sx={{ width: '100%', mr: 1 }}>
        <LinearProgress
          variant="determinate"
          value={Math.min(100, state.chaos)}
          color={state.chaos > 100 ? "error" : "warning"}
          sx={{ 
            height: 10, 
            borderRadius: 5,
            '& .MuiLinearProgress-bar': {
              backgroundColor: state.chaos > 100 ? '#ff1744' : undefined,
            }
          }}
        />
      </Box>
      <Box sx={{ minWidth: 45 }}>
        <Typography 
          variant="body2" 
          color={state.chaos > 100 ? "error" : "text.secondary"} 
          fontWeight={state.chaos > 100 ? "bold" : "normal"}
        >
          {state.chaos}%
        </Typography>
      </Box>
    </Box>
    {state.chaos > 100 && (
      <Typography variant="caption" color="error">
        Warning: Chaos exceeding safe levels! This may trigger throttling.
      </Typography>
    )}
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
      </Grid>

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