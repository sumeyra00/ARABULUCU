using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ARABULUCU.Web.Models.Listing;

public class FoundItemViewModel
{
    [Required(ErrorMessage = "Eşya başlığı zorunludur.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 4, ErrorMessage = "Geçerli bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Bulunduğu tarih ve saat zorunludur.")]
    public DateTime FoundDate { get; set; }

    [Required(ErrorMessage = "Kampüs bölgesi zorunludur.")]
    public string CampusArea { get; set; } = string.Empty;

    [StringLength(300)]
    public string? LocationDetail { get; set; }

    [Required(ErrorMessage = "Eşyanın mevcut durumunu seçin.")]
    public string ItemStatus { get; set; } = "with_me";

    [StringLength(300)]
    public string? DeliveredTo { get; set; }

    public IFormFile? ItemPhoto { get; set; }

    [Required(ErrorMessage = "Doğrulama sorusu zorunludur.")]
    [StringLength(300)]
    public string SecurityQuestion { get; set; } = string.Empty;
}
