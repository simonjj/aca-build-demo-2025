const BasePetService = require('./base-pet-service');

class RabbitService extends BasePetService {
  constructor() {
    super('rabbit', 3003);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Rabbit-specific behaviors
    switch (action) {
      case 'hop':
        newState.footballSkills.speed += 12; // Rabbits are fast
        newState.energy = Math.max(0, newState.energy - 10);
        newState.lastMessage = "Hop hop! I'm quick on my feet!";
        break;
      case 'nibble':
        newState.mood = 'playful';
        newState.energy = Math.min(100, newState.energy + 3);
        newState.lastMessage = 'Nibble nibble...';
        break;
      default:
        // Handle base interactions
        newState = await super.handleInteraction(action, message);
    }

    return newState;
  }
}

// Start the service if this file is run directly
if (require.main === module) {
  const rabbitService = new RabbitService();
  rabbitService.start();
}

module.exports = RabbitService; 