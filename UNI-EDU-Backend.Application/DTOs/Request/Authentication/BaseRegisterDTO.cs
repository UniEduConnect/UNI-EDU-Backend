using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class BaseRegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public string Fullname { get; set; }
    }
}