using UserAuthorizationandAuthentication.TourGuide.Models;
namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
{
    public class UpdateLicenseDto
    {
        public string? LicenseId { get; set; }
        public string? LicenseCard { get; set; }
        public string LicenseIdFrontPhoto { get; set; }
        public string LicenseIdBackPhoto { get; set; }
    }
}



