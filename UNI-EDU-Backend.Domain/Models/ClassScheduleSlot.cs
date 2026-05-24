namespace UNI_EDU_Backend.Domain.Models
{
    public class ClassScheduleSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
