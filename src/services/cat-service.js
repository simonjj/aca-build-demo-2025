const BasePetService = require('./base-pet-service');

class CatService extends BasePetService {
  constructor() {
    super('cat', 3002);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Cat-specific behaviors
    switch (action) {
      case 'pounce':
        newState.footballSkills.dribbling += 10; // Cats are agile
        newState.energy = Math.max(0, newState.energy - 15);
        newState.lastMessage = 'Meow! Watch my fancy footwork!';
        break;
      case 'purr':
        newState.mood = 'content';
        newState.energy = Math.min(100, newState.energy + 5);
        newState.lastMessage = 'Purrrrr...';
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
  const catService = new CatService();
  catService.start();
}

module.exports = CatService; 