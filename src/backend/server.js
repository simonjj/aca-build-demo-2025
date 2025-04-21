const express = require('express');
const http = require('http');
const socketIo = require('socket.io');
const cors = require('cors');
const { createClient } = require('redis');
require('dotenv').config();

const app = express();
const server = http.createServer(app);

// Add more detailed CORS configuration
const io = socketIo(server, {
  cors: {
    origin: "*", // Allow all origins for testing
    methods: ["GET", "POST"],
    credentials: true
  }
});

// Add more detailed CORS middleware
app.use(cors({
  origin: "*",
  methods: ["GET", "POST"],
  credentials: true
}));
app.use(express.json());

// Test endpoint
app.get('/test', (req, res) => {
  console.log('Test endpoint hit');
  res.status(200).json({ message: 'Backend is working!' });
});

// Health check endpoint
app.get('/health', (req, res) => {
  console.log('Health check hit');
  res.status(200).json({ status: 'ok' });
});

// Initialize Redis client
const redisClient = createClient({
  url: process.env.REDIS_URL || 'redis://localhost:6379'
});

// Handle Redis connection errors
redisClient.on('error', (err) => {
  console.error('Redis Client Error:', err);
});

// Handle Redis connection
redisClient.on('connect', () => {
  console.log('Connected to Redis');
});

// Pet state management
const PETS = {
  chillturtle: { mood: 'happy', energy: 100, lastMessage: '' },
  emoocto: { mood: 'sad', energy: 100, lastMessage: '' },
  chaosdragon: { mood: 'angry', energy: 100, lastMessage: '' },
  babydino: { mood: 'happy', energy: 100, lastMessage: '' },
  bouncybun: { mood: 'happy', energy: 100, lastMessage: '' }
};

// Initialize pet states in Redis
async function initializePetStates() {
  try {
    await redisClient.connect();
    for (const [petId, state] of Object.entries(PETS)) {
      await redisClient.set(`pet:${petId}`, JSON.stringify(state));
    }
    console.log('Pet states initialized in Redis');
  } catch (error) {
    console.error('Error initializing pet states:', error);
    process.exit(1);
  }
}

// Socket.io connection handling
io.on('connection', (socket) => {
  console.log('New client connected', socket.id);

  // Send initial pet states to the new client
  Object.entries(PETS).forEach(async ([petId, state]) => {
    try {
      const storedState = await redisClient.get(`pet:${petId}`);
      if (storedState) {
        console.log(`Sending initial state for ${petId}`);
        socket.emit('petUpdate', {
          petId,
          state: JSON.parse(storedState)
        });
      }
    } catch (error) {
      console.error(`Error sending initial state for ${petId}:`, error);
    }
  });

  // Handle pet interactions
  socket.on('petInteraction', async (data) => {
    console.log('Received pet interaction:', data);
    try {
      const { petId, action, message } = data;
      const currentState = JSON.parse(await redisClient.get(`pet:${petId}`) || '{}');

      let newState = { ...currentState };

      switch (action) {
        case 'pet':
          newState.mood = 'happy';
          newState.energy = Math.min(100, newState.energy + 10);
          break;
        case 'feed':
          newState.energy = Math.min(100, newState.energy + 20);
          break;
        case 'poke':
          newState.mood = 'angry';
          newState.energy = Math.max(0, newState.energy - 10);
          break;
        case 'sing':
          newState.mood = 'sleepy';
          newState.energy = Math.min(100, newState.energy + 5);
          break;
        case 'message':
          newState.lastMessage = message;
          break;
      }

      // Save new state to Redis
      await redisClient.set(`pet:${petId}`, JSON.stringify(newState));
      console.log(`Updated state for ${petId}:`, newState);

      // Broadcast the update to all connected clients
      io.emit('petUpdate', {
        petId,
        state: newState
      });
    } catch (error) {
      console.error('Error handling pet interaction:', error);
      socket.emit('error', { message: 'Failed to process interaction' });
    }
  });

  socket.on('disconnect', () => {
    console.log('Client disconnected', socket.id);
  });
});

// Initialize pet states and start server
initializePetStates().then(() => {
  const PORT = process.env.PORT || 3001;
  server.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
    console.log(`Test endpoint available at http://localhost:${PORT}/test`);
    console.log(`Health check available at http://localhost:${PORT}/health`);
  });
}).catch((error) => {
  console.error('Failed to start server:', error);
  process.exit(1);
}); 