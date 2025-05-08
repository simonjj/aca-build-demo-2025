/**
 * API configuration for Azure Container Apps backend services
 * Following Azure best practices for frontend-backend integration
 */

// Map pet types to their corresponding environment variables
const petApiEnvMap = {
    'turtle': 'REACT_APP_API_TURTLE',
    'octo': 'REACT_APP_API_OCTO', 
    'dragon': 'REACT_APP_API_DRAGON',
    'dino': 'REACT_APP_API_DINO',
    'bunny': 'REACT_APP_API_BUNNY'
  };
  
  /**
   * Gets the API URL for a specific pet type using environment variables
   * @param {string} petType - The type of pet (turtle, octo, dragon, dino, bunny)
   * @returns {string} The complete API URL for the pet service
   */
  export const getApiUrl = (petType) => {
    // First normalize the pet type to lowercase for consistent lookup
    const normalizedPetType = petType?.toLowerCase();
    
    // Get the corresponding environment variable key for this pet type
    const envKey = petApiEnvMap[normalizedPetType];
    
    // If we don't have a mapping or the env var isn't defined, fall back to the default API URL
    if (!envKey || !process.env[envKey]) {
      console.warn(`No API URL configured for pet type: ${petType}, falling back to default API`);
      return process.env.REACT_APP_API_URL || 'http://localhost:3001';
    }
    
    // Return the pet-specific API URL from environment variables
    return process.env[envKey];
  };
  
  /**
   * Get the pet state from the appropriate backend service
   * @param {string} petType - The type of pet
   * @param {string} petId - Optional pet ID for multi-instance pets
   * @returns {Promise<Object>} The pet state object
   */
  export const getPetState = async (petType, petId = null) => {
    try {
      const apiUrl = getApiUrl(petType);
      const response = await fetch(`${apiUrl}/pet/state${petId ? `?id=${petId}` : ''}`, {
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });
      
      if (!response.ok) {
        throw new Error(`Failed to fetch pet state: ${response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error fetching pet state:', error);
      throw error;
    }
  };
  
  /**
   * Interact with a pet via the backend API
   * @param {string} petType - The type of pet
   * @param {string} action - The interaction action (pet, feed, poke, sing, message)
   * @param {string} message - Optional message for the pet
   * @param {string} petId - Optional pet ID for multi-instance pets
   * @returns {Promise<Object>} The updated pet state
   */
  export const interactWithPet = async (petType, action, message = null, petId = null) => {
    try {
      const apiUrl = getApiUrl(petType);
      
      // Create the payload based on the API requirements
      const payload = {
        action,
        ...(message && { message }),
        ...(petId && { id: petId })
      };
      
      // Make the API call to the pet-specific endpoint
      const response = await fetch(`${apiUrl}/pet/interact`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      
      if (!response.ok) {
        throw new Error(`Failed to interact with ${petType}: ${response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error(`Error during ${action} interaction with ${petType}:`, error);
      throw error;
    }
  };