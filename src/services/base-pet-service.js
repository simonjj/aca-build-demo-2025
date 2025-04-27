const express = require('express');
const { createClient } = require('redis');
const cors = require('cors');
require('dotenv').config();

class BasePetService {
  constructor(petId, port) {
    this.petId = petId;
    this.port = port;
    this.app = express();
    this.redisClient = createClient({
      url: process.env.REDIS_URL || 'redis://localhost:6379'
    });

    this.setupMiddleware();
    this.setupRoutes();
    this.setupRedis();
  }

  setupMiddleware() {
    this.app.use(cors({
      origin: process.env.FRONTEND_URL || 'http://localhost:80',
      methods: ['GET', 'POST']
    }));
    this.app.use(express.json());
  }

  setupRoutes() {
    // Health check endpoint
    this.app.get('/health', (req, res) => {
      res.json({ status: 'healthy', pet: this.petId });
    });

    // Get pet state
    this.app.get('/state', async (req, res) => {
      try {
        const state = await this.redisClient.get(`pet:${this.petId}`);
        res.json(JSON.parse(state || '{}'));
      } catch (error) {
        res.status(500).json({ error: 'Failed to get pet state' });
      }
    });

    // Update pet state
    this.app.post('/interact', async (req, res) => {
      try {
        const { action, message } = req.body;
        const newState = await this.handleInteraction(action, message);
        await this.redisClient.set(`pet:${this.petId}`, JSON.stringify(newState));
        res.json(newState);
      } catch (error) {
        res.status(500).json({ error: 'Failed to process interaction' });
      }
    });
  }

  async setupRedis() {
    this.redisClient.on('error', (err) => console.error(`Redis Client Error for ${this.petId}:`, err));
    await this.redisClient.connect();
    
    // Initialize pet state if not exists
    const currentState = await this.redisClient.get(`pet:${this.petId}`);
    if (!currentState) {
      await this.redisClient.set(`pet:${this.petId}`, JSON.stringify({
        mood: 'happy',
        energy: 100,
        position: { x: 0, y: 0 },
        lastMessage: '',
        footballSkills: {
          dribbling: 50,
          shooting: 50,
          passing: 50
        }
      }));
    }
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    switch (action) {
      case 'dribble':
        newState.footballSkills.dribbling += 5;
        newState.energy = Math.max(0, newState.energy - 10);
        break;
      case 'shoot':
        newState.footballSkills.shooting += 5;
        newState.energy = Math.max(0, newState.energy - 15);
        break;
      case 'pass':
        newState.footballSkills.passing += 5;
        newState.energy = Math.max(0, newState.energy - 8);
        break;
      case 'rest':
        newState.energy = Math.min(100, newState.energy + 20);
        break;
      case 'message':
        newState.lastMessage = message;
        break;
    }

    return newState;
  }

  start() {
    this.app.listen(this.port, () => {
      console.log(`${this.petId} service running on port ${this.port}`);
    });
  }
}

module.exports = BasePetService; 