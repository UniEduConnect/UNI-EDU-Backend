namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class TutorRegister : BaseRegisterDTO
    {
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string Degree { get; set; }
        public string? Experience { get; set; }
        public string? StudentIdNumber { get; set; }
        public float AverageRating { get; set; }
    }
}