using Homelab.Domain;
using Homelab.Domain.Entities.Enums;

namespace Homelab.Domain.Entities.Locations;

public class LocationDirection : Audit
{
    public Guid ExternalId { get; set; }
    public Guid AcademicLocationId { get; set; }
    public AcademicLocation? AcademicLocation { get; set; }

    public TravelMode TravelMode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string? PublicTransportStop { get; set; }
    public string? ParkingInstructions { get; set; }
    public string? AccessibilityNotes { get; set; }
    public string? ExternalNavigationUrl { get; set; }
    public int SortOrder { get; set; }
}

