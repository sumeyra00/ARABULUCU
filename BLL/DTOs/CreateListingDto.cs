using System.ComponentModel.DataAnnotations;

namespace ARABULUCU.BLL.DTOs;

public class CreateListingDto
{
    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Geçerli bir fiyat girin.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Kilometre sıfır veya daha büyük olmalıdır.")]
    public int Mileage { get; set; }

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    public List<int> SelectedFeatureIds { get; set; } = [];
}
