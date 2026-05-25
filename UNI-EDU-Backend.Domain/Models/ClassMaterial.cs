using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class ClassMaterial
    {
        [Key]
        public Guid MaterialID { get; set; }

        [ForeignKey("Class")]
        public Guid ClassID { get; set; }

        public string Name { get; set; } = string.Empty;
        /// <summary>"pdf" | "doc" | "image" | "video" | "link"</summary>
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Size { get; set; }
        public DateTime UploadedAt { get; set; }

        public virtual Class Class { get; set; }
    }
}
