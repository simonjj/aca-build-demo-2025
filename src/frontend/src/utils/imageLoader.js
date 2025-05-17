/**
 * Image loader utility for Azure Container Apps
 * Handles image loading and offers fallbacks
 */
export const loadImage = (src) => {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.src = src;
      img.onload = () => resolve(src);
      img.onerror = () => reject(new Error(`Failed to load image: ${src}`));
    });
  };
  
  /**
   * Preloads all pet images for smoother UX
   * @param {Array} pets - Array of pet objects with image properties
   */
  export const preloadPetImages = (pets) => {
    if (!Array.isArray(pets)) return;
    
    pets.forEach(pet => {
      if (pet.image) {
        // Create image object but don't attach to DOM
        const img = new Image();
        img.src = pet.image;
      }
    });
  };