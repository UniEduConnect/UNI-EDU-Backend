using System.ComponentModel.DataAnnotations;

namespace UNI_EDU_Backend.Domain.Models
{
    // Single-row AI proctoring configuration for the exam-manager portal.
    public class ExamAiConfig
    {
        [Key]
        public Guid ConfigID { get; set; }

        public bool ProctoringEnabled { get; set; } = true;
        public bool FaceDetection { get; set; }
        public bool FullscreenRequired { get; set; } = true;
        public bool CopyPasteBlocked { get; set; } = true;
        public int TabSwitchLimit { get; set; } = 3;

        // Auto-generation defaults.
        public bool AutoGenerateEnabled { get; set; }
        public string DefaultDifficulty { get; set; } = "medium";

        public DateTime UpdatedAt { get; set; }
    }
}
