const BasePetService = require('./base-pet-service');

class DinoService extends BasePetService {
  constructor() {
    super('dino', 3004);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Dinosaur-specific behaviors
    switch (action) {
      case 'stomp':
        newState.footballSkills.strength += 15; // Dinosaurs are strong
        newState.energy = Math.max(0, newState.energy - 10);
        newState.lastMessage = "ROAR! Feel my power!";
        break;
      case 'charge':
        newState.footballSkills.speed += 12;
        newState.energy = Math.max(0, newState.energy - 8);
        newState.lastMessage = "I'm charging through!";
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
  const dinoService = new DinoService();
  dinoService.start();
}

module.exports = DinoService; 