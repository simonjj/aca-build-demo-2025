import React, { useState, useEffect } from 'react';
import { Container, Grid, Typography, Box, Paper } from '@mui/material';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import PetCard from './components/PetCard';
import PetInteraction from './components/PetInteraction';
import io from 'socket.io-client';

const theme = createTheme({
  palette: {
    primary: {
      main: '#4a148c',
    },
    secondary: {
      main: '#ff4081',
    },
  },
});

const PETS = [
  { id: 'chillturtle', name: 'ChillTurtle', emoji: '🐢' },
  { id: 'emoocto', name: 'EmoOcto', emoji: '🐙' },
  { id: 'chaosdragon', name: 'ChaosDragon', emoji: '🐉' },
  { id: 'babydino', name: 'BabyDino', emoji: '🦖' },
  { id: 'bouncybun', name: 'BouncyBun', emoji: '🐇' },
];

function App() {
  const [selectedPet, setSelectedPet] = useState(null);
  const [socket, setSocket] = useState(null);
  const [petStates, setPetStates] = useState({});

  useEffect(() => {
    const newSocket = io(process.env.REACT_APP_API_URL);
    setSocket(newSocket);

    newSocket.on('petUpdate', (data) => {
      setPetStates(prev => ({
        ...prev,
        [data.petId]: data.state
      }));
    });

    return () => newSocket.close();
  }, []);

  return (
    <ThemeProvider theme={theme}>
      <Container maxWidth="lg">
        <Box sx={{ my: 4 }}>
          <Typography variant="h2" component="h1" gutterBottom align="center">
            🦴 Cloud Petting Zoo
          </Typography>
          <Typography variant="h5" component="h2" gutterBottom align="center">
            Choose a creature to interact with!
          </Typography>

          <Grid container spacing={3} sx={{ mt: 4 }}>
            {PETS.map((pet) => (
              <Grid item xs={12} sm={6} md={4} key={pet.id}>
                <PetCard
                  pet={pet}
                  isSelected={selectedPet?.id === pet.id}
                  onClick={() => setSelectedPet(pet)}
                  state={petStates[pet.id]}
                />
              </Grid>
            ))}
          </Grid>

          {selectedPet && (
            <Box sx={{ mt: 4 }}>
              <Paper elevation={3} sx={{ p: 3 }}>
                <PetInteraction
                  pet={selectedPet}
                  socket={socket}
                  state={petStates[selectedPet.id]}
                />
              </Paper>
            </Box>
          )}
        </Box>
      </Container>
    </ThemeProvider>
  );
}

export default App; 