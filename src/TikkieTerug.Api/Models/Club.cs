namespace TikkieTerug.Api.Models;

public class Club
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
    public int? AfdelingId { get; set; }
    public string? Speeldag { get; set; }
    public int? CompetitionId { get; set; }
    public int? ParentClubId { get; set; }
    public int? TeamNumber { get; set; }
}
