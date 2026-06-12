using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // Physical room inventory for the office portal (scheduling / availability).
    public class Room
    {
        [Key]
        public Guid RoomID { get; set; }

        public string Name { get; set; } = string.Empty;   // e.g. "A101"
        public int Capacity { get; set; }
        public string Type { get; set; } = "classroom";     // classroom | lab | seminar
        public string? Building { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
