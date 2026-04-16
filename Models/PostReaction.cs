using FreakBlog.API.Models;

public class PostReaction
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Post? Post { get; set; }
    public User? User { get; set; }
}