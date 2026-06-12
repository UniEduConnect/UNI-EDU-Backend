namespace UNI_EDU_Backend.Domain.Enums
{
    public enum UserRole
    {
        Admin,
        Tutor,
        Parent,
        Student,
        // Staff portals (added). Stored as ints 4/5/6 — additive, existing data unaffected.
        Office,
        Finance,
        ExamManager
    }
}