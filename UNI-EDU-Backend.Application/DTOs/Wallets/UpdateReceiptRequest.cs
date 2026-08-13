using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Application.DTOs.Wallets;

public class UpdateReceiptRequest
{
    [Required]
    public string ReceiptUrl { get; set; } = string.Empty;
}
