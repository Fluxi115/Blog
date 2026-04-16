using Microsoft.AspNetCore.Http;

namespace FreakBlog.API.Dtos
{
    public class PostCreateDto
    {
        public long UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public IFormFile? FeaturedImage { get; set; }
        public IFormFile? VideoFile { get; set; }
    }
}