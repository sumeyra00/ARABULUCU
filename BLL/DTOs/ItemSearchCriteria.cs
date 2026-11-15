namespace ARABULUCU.BLL.DTOs;

public sealed class ItemSearchCriteria
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public bool? IsFound { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public DateTime? EventDateFrom { get; set; }
    public DateTime? EventDateTo { get; set; }
    public bool? IsItemFound { get; set; }
    public bool? IsDelivered { get; set; }
}
