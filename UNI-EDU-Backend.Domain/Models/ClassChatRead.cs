namespace UNI_EDU_Backend.Domain.Models
{
    // Per-(class, user) read marker for the class chat. Composite key (ClassID, UserID) is
    // configured in OnModelCreating. Unread = messages with SentAt after LastReadAt.
    public class ClassChatRead
    {
        public Guid ClassID { get; set; }
        public Guid UserID { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}
