namespace UNI_EDU_Backend.Application.DTOs.Request.Authentication
{
    public class BaseRegisterDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Fullname { get; set; }
    }
}