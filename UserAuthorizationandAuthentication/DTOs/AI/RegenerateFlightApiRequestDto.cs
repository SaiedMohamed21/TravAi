using System.ComponentModel.DataAnnotations;

namespace TravAi.DTOs.AI
{
    public class RegenerateFlightApiRequestDto
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [Range(1, 20)]
        public int Adults { get; set; }

        [Range(0, 20)]
        public int Children { get; set; }

        [Required]
        public string Direction { get; set; } = "Outbound"; // Outbound or Return
    }
}
