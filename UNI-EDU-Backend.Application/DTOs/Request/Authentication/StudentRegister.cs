using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class StudentRegister : BaseRegisterDTO
    {
        [Required]
        public string School { get; set; }

        [Range(1, 12)]
        public int Grade { get; set; }
    }
}