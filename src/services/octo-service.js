const BasePetService = require('./base-pet-service');

class OctoService extends BasePetService {
  constructor() {
    super('octo', 3002);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Octopus-specific behaviors
    switch (action) {
      case 'multitask':
        newState.footballSkills.passing += 10; // Octopuses can pass with multiple arms
        newState.footballSkills.dribbling += 10;
        newState.energy = Math.max(0, newState.energy - 12);
        newState.lastMessage = "I can do everything at once!";
        break;
      case 'ink':
        newState.footballSkills.stealth += 15;
        newState.energy = Math.max(0, newState.energy - 8);
        newState.lastMessage = "You can't see me now!";
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
  const octoService = new OctoService();
  octoService.start();
}

module.exports = OctoService; 