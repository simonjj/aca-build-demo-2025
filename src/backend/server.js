const express = require('express');
const http = require('http');
const socketIo = require('socket.io');
const cors = require('cors');
const Redis = require('redis');
require('dotenv').config();

const app = express();
const server = http.createServer(app);
const io = socketIo(server, {
  cors: {
    origin: process.env.FRONTEND_URL || 'http://localhost:80',
    methods: ['GET', 'POST']
  }
});

app.use(cors());
app.use(express.json());

// Initialize Redis client
const redisClient = Redis.createClient({
  url: process.env.REDIS_URL || 'redis://localhost:6379'
});

redisClient.on('error', (err) => console.log('Redis Client Error', err));
redisClient.connect();

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
  for (const [petId, state] of Object.entries(PETS)) {
    await redisClient.set(`pet:${petId}`, JSON.stringify(state));
  }
}

// Socket.io connection handling
io.on('connection', (socket) => {
  console.log('New client connected');

  // Send initial pet states to the new client
  Object.entries(PETS).forEach(async ([petId, state]) => {
    const storedState = await redisClient.get(`pet:${petId}`);
    if (storedState) {
      socket.emit('petUpdate', {
        petId,
        state: JSON.parse(storedState)
      });
    }
  });

  // Handle pet interactions
  socket.on('petInteraction', async (data) => {
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

    // Broadcast the update to all connected clients
    io.emit('petUpdate', {
      petId,
      state: newState
    });
  });

  socket.on('disconnect', () => {
    console.log('Client disconnected');
  });
});

// Initialize pet states and start server
initializePetStates().then(() => {
  const PORT = process.env.PORT || 3001;
  server.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
  });
}); 