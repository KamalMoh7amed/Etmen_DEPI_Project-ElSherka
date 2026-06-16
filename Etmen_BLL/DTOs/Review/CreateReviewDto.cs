using System.ComponentModel.DataAnnotations;

namespace Etmen_BLL.DTOs.Review
{
    public class CreateReviewDto
    {
        public int? DoctorProfileId { get; set; }
        public int? HealthcareProviderId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string Comment { get; set; } = string.Empty;
    }
}
