const BasePetService = require('./base-pet-service');

class DogService extends BasePetService {
  constructor() {
    super('dog', 3001);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Dog-specific behaviors
    switch (action) {
      case 'fetch':
        newState.footballSkills.passing += 8; // Dogs are good at fetching
        newState.energy = Math.max(0, newState.energy - 12);
        newState.lastMessage = 'Woof! I love fetching the ball!';
        break;
      case 'bark':
        newState.mood = 'excited';
        newState.lastMessage = 'Woof woof!';
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
  const dogService = new DogService();
  dogService.start();
}

module.exports = DogService; 