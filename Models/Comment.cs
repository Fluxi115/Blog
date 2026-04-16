namespace FreakBlog.API.Models
{
    public class Comment
    {
        public long Id { get; set; }
        public string Content { get; set; } = string.Empty;

        // Para el iconito en los comentarios 
        public int LikesCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long UserId { get; set; }
        public User User { get; set; } = null!;
    }
}