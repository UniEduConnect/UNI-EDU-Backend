using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UNI_EDU_Backend.Domain.Models
{
    public class Student
    {
        [Key]
        [ForeignKey("User")]
        public Guid StudentID { get; set; }

        [ForeignKey("Parent")]
        public Guid? ParentID { get; set; }

        public string FullName { get; set; }
        public string School { get; set; }
        public int Grade { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Parent Parent { get; set; }
        public virtual ICollection<Class> Classes { get; set; }
    }
}