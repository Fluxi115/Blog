namespace FreakBlog.API.Models
{
    public class Post
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; 
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = "DRAFT";
        
        public string? FeaturedImage { get; set; }
        public string? VideoUrl { get; set; }

        public int Views { get; set; } = 0;
        
        // Los contadores
        public int ReadTime { get; set; } = 0;
        public int EpicCount { get; set; } = 0;
        public int AnalysisCount { get; set; } = 0;
        public int DebateCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;

        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int FireCount { get; set; } = 0;
        public int HeartCount { get; set; } = 0;

        // Relaciones
        public List<Category> Categories { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }
}