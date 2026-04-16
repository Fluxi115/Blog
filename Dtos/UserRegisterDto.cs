namespace FreakBlog.API.Dtos
{
    // Esta clase sirve para recibir exactamente lo que el usuario envía desde React
    public class UserRegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}