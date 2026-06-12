namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class ExamAiConfigResponse
{
    public bool ProctoringEnabled { get; set; }
    public bool FaceDetection { get; set; }
    public bool FullscreenRequired { get; set; }
    public bool CopyPasteBlocked { get; set; }
    public int TabSwitchLimit { get; set; }
    public bool AutoGenerateEnabled { get; set; }
    public string DefaultDifficulty { get; set; } = "medium";
    public DateTime UpdatedAt { get; set; }
}

public class UpdateExamAiConfigRequest
{
    public bool ProctoringEnabled { get; set; } = true;
    public bool FaceDetection { get; set; }
    public bool FullscreenRequired { get; set; } = true;
    public bool CopyPasteBlocked { get; set; } = true;
    public int TabSwitchLimit { get; set; } = 3;
    public bool AutoGenerateEnabled { get; set; }
    public string DefaultDifficulty { get; set; } = "medium";
}
