using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class TutorRegister : BaseRegisterDTO
    {
        [Required]
        public string Degree { get; set; }

        public string Experience { get; set; }
    }
}