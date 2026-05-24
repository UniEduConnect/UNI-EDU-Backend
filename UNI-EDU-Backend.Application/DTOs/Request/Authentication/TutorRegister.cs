namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class TutorRegister : BaseRegisterDTO
    {
        public string Gender { get; set; }
        public string? StudentIdNumber { get; set; }
        public string Degree { get; set; }
    }
}