import { useState, useEffect } from 'react';
import { Container, Grid, Typography, Box, Paper, Alert, Snackbar } from '@mui/material';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import PetCard from './components/PetCard';
import PetInteraction from './components/PetInteraction';
import { getPetState, interactWithPet } from './utils/apiConfig';
//import { trace, metrics } from '@opentelemetry/api';

// 2️⃣ Create tracer + meter
import { tracer, meter } from './telemetry';

// create a counter
const clickCounter = meter.createCounter('app.button_clicks', {
  description: 'Counts how often buttons are clicked'
});

const loadPetStateCounter   = meter.createCounter('app.loadpetstate.counter', {
    description: 'Counts loadPetState calls'
  });
const selectPetCounter       = meter.createCounter('app.selectpet.counter', {
    description: 'Counts pet selections'
  });
const petInteractionCounter  = meter.createCounter('app.petinteraction.counter', {
    description: 'Counts pet interaction calls'
  });

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

// Pet configuration following Azure Container Apps microservices pattern
const PETS = [
  { id: 'turtle', name: 'ChillTurtle', emoji: '🐢', type: 'turtle' },
  { id: 'octo', name: 'EmoOcto', emoji: '🐙', type: 'octo' },
  { id: 'dragon', name: 'ChaosDragon', emoji: '🐉', type: 'dragon' },
  { id: 'dino', name: 'BabyDino', emoji: '🦖', type: 'dino' },
  { id: 'bunny', name: 'BouncyBun', emoji: '🐇', type: 'bunny' },
];

function App() {
  const [selectedPet, setSelectedPet] = useState(null);
  const [petStates, setPetStates] = useState({});
  const [loading, setLoading] = useState({});
  const [error, setError] = useState(null);

  // Initialize pet states from their respective microservices
  useEffect(() => {
    let isMounted = true;
  
    const span = tracer.startSpan('sanity.check');
    console.log('span started, id =', span.spanContext().spanId);
    span.end();
    // Shared loader fn
    const loadPetStates = async () => {
      // mark all as loading
      setLoading(prev => {
        const m = { ...prev };
        PETS.forEach(p => (m[p.id] = true));
        return m;
      });
  
      for (const pet of PETS) {
        try {
          loadPetStateCounter.add(1, { op: 'loadPetState', pet: pet.id });
          const state = await getPetState(pet.type);
          if (!isMounted) return;
          setPetStates(prev => ({ ...prev, [pet.id]: state }));
        } catch (err) {
          console.error(`Failed to load state for ${pet.name}:`, err);
          if (!isMounted) return;
          setError(`Failed to connect to ${pet.name} API. Please try again later.`);
        } finally {
          if (!isMounted) return;
          setLoading(prev => ({ ...prev, [pet.id]: false }));
        }
      }
    };
  
    // initial fetch
    loadPetStates();
  
    // poll every 2s
    const intervalId = setInterval(loadPetStates, 2000);
  
    // cleanup on unmount
    return () => {
      isMounted = false;
      clearInterval(intervalId);
    };
  }, []);

  const selectPet = (pet) => {
    selectPetCounter.add(1, { op: 'selectPet', pet: pet.id });
    setSelectedPet(pet);
    // Track pet selection for analytics
    
  };

  // Handle pet interaction from child component
  const handlePetInteraction = async (petType, action, message = null) => {
    if (!selectedPet) return;
    petInteractionCounter.add(1, { op: 'petInteraction', pet: petType, action });
    try {
      setLoading(prev => ({...prev, [selectedPet.id]: true}));
      
      // Call the appropriate backend service based on pet type
      const updatedState = await interactWithPet(petType, action, message);
      
      // Update local state with response from backend
      setPetStates(prev => ({
        ...prev,
        [selectedPet.id]: updatedState
      }));

      
      return updatedState;
    } catch (err) {
      console.error(`Interaction failed for ${petType}:`, err);
      setError(`Failed to ${action} ${selectedPet.name}. Please try again.`);
      
      
      return null;
    } finally {
      setLoading(prev => ({...prev, [selectedPet.id]: false}));
    }
  };

  const closeError = () => {
    setError(null);
  };

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
                  onClick={() => selectPet(pet)}
                  state={petStates[pet.id]}
                  loading={loading[pet.id]}
                />
              </Grid>
            ))}
          </Grid>

          {selectedPet && (
            <Box sx={{ mt: 4 }}>
              <Paper elevation={3} sx={{ p: 3 }}>
                <PetInteraction
                  pet={selectedPet}
                  state={petStates[selectedPet.id]}
                  loading={loading[selectedPet.id]}
                  onInteract={handlePetInteraction}
                />
              </Paper>
            </Box>
          )}
          
          <Snackbar open={!!error} autoHideDuration={6000} onClose={closeError}>
            <Alert onClose={closeError} severity="error" sx={{ width: '100%' }}>
              {error}
            </Alert>
          </Snackbar>
        </Box>
      </Container>
    </ThemeProvider>
  );
}

export default App;