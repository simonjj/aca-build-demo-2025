const BasePetService = require('./base-pet-service');

class DragonService extends BasePetService {
  constructor() {
    super('dragon', 3003);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Dragon-specific behaviors
    switch (action) {
      case 'fireball':
        newState.footballSkills.shooting += 20; // Dragons have powerful shots
        newState.energy = Math.max(0, newState.energy - 15);
        newState.lastMessage = "Feel the heat of my shot!";
        break;
      case 'fly':
        newState.footballSkills.aerial += 15;
        newState.energy = Math.max(0, newState.energy - 10);
        newState.lastMessage = "I'm unstoppable in the air!";
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
  const dragonService = new DragonService();
  dragonService.start();
}

module.exports = DragonService; 