using System.ComponentModel.DataAnnotations;

namespace FreakBlog.API.Models
{
    public class PasswordReset
    {
        [Key] // El correo será nuestra llave principal aquí
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}