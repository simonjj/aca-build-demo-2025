const BasePetService = require('./base-pet-service');

class BunnyService extends BasePetService {
  constructor() {
    super('bunny', 3005);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Bunny-specific behaviors
    switch (action) {
      case 'hop':
        newState.footballSkills.agility += 15; // Bunnies are agile
        newState.energy = Math.max(0, newState.energy - 8);
        newState.lastMessage = "Hop hop! I'm too quick for you!";
        break;
      case 'bounce':
        newState.footballSkills.jumping += 12;
        newState.energy = Math.max(0, newState.energy - 6);
        newState.lastMessage = "I can jump higher than anyone!";
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
  const bunnyService = new BunnyService();
  bunnyService.start();
}

module.exports = BunnyService; 