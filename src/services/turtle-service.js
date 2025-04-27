const BasePetService = require('./base-pet-service');

class TurtleService extends BasePetService {
  constructor() {
    super('turtle', 3001);
  }

  async handleInteraction(action, message) {
    const currentState = JSON.parse(await this.redisClient.get(`pet:${this.petId}`));
    let newState = { ...currentState };

    // Turtle-specific behaviors
    switch (action) {
      case 'defend':
        newState.footballSkills.defense += 15; // Turtles are great at defense
        newState.energy = Math.max(0, newState.energy - 8);
        newState.lastMessage = "I'm a wall! Nothing gets past me!";
        break;
      case 'shell':
        newState.footballSkills.defense += 20;
        newState.energy = Math.max(0, newState.energy - 5);
        newState.lastMessage = "Safe in my shell!";
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
  const turtleService = new TurtleService();
  turtleService.start();
}

module.exports = TurtleService; 