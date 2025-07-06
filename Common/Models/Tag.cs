namespace NoteApi.Common.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
    }
}
