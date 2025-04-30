namespace BabyDinoApi.Models
{
    /// <summary>
    /// Represents a request to interact with the Baby Dino
    /// </summary>
    public class InteractionRequest
    {
        /// <summary>
        /// The type of action to perform (pet, feed, poke, sing, message)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Optional message to send to the dino
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Optional identifier for the user performing the interaction
        /// </summary>
        public string? UserId { get; set; }
    }
}