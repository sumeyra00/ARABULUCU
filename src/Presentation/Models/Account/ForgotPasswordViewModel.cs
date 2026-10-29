using System.ComponentModel.DataAnnotations;

namespace ARABULUCU.Web.Models.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;
}
