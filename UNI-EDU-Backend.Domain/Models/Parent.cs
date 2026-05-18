using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Parent
    {
        [Key]
        [ForeignKey("User")]
        public Guid ParentID { get; set; }

        public string FullName { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }

        public virtual ICollection<Student> Children { get; set; }
    }
}