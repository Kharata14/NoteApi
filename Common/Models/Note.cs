namespace NoteApi.Common.Models
{
    public class Note
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
        public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
    }
}
