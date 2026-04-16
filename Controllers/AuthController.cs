using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreakBlog.API.Data;
using FreakBlog.API.Models;
using FreakBlog.API.Dtos;
using MailKit.Net.Smtp;
using MimeKit;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FreakBlog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(UserRegisterDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("El correo ya existe.");

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "lector"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login(UserLoginDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null) return BadRequest("Usuario no encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return BadRequest("Contraseña incorrecta.");

            string token = CreateToken(user);

            //Envia todos los datos que el Dashboard ocupa para validar
            return Ok(new
            {
                token = token,
                userName = user.Name,
                userRole = user.Role,
                userId = user.Id
            });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return BadRequest("Usuario no encontrado.");

            // Limpieza de códigos viejos
            var existingReset = _context.PasswordResets.FirstOrDefault(r => r.Email == email);
            if (existingReset != null)
            {
                _context.PasswordResets.Remove(existingReset);
            }

            // Generamos un PIN aleatorio de 6 cifras
            var pinCode = new Random().Next(100000, 999999).ToString();

            var reset = new PasswordReset
            {
                Email = email,
                Token = pinCode,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordResets.Add(reset);
            _context.SaveChanges();

            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("BiblioTech Admin", _configuration["EmailConfiguration:SmtpUsername"]!));
                emailMessage.To.Add(new MailboxAddress(user.Name, user.Email));
                emailMessage.Subject = "Código de Verificación - BiblioTech";

                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 400px; padding: 20px; border: 1px solid #eee;'>
                            <h2 style='color: #E6391E;'>Recuperación de acceso</h2>
                            <p>Hola <strong>{user.Name}</strong>,</p>
                            <p>Tu código de verificación para restablecer tu contraseña es:</p>
                            <div style='background: #f4f4f4; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #E6391E;'>
                                {pinCode}
                            </div>
                            <p style='font-size: 12px; color: #777; margin-top: 20px;'>Este código expirará en 1 hora.</p>
                        </div>"
                };

                using (var client = new SmtpClient())
                {
                    client.CheckCertificateRevocation = false;
                    client.Connect(
                        _configuration["EmailConfiguration:SmtpServer"] ?? "smtp.gmail.com",
                        int.Parse(_configuration["EmailConfiguration:SmtpPort"] ?? "587"),
                        MailKit.Security.SecureSocketOptions.StartTls
                    );

                    client.Authenticate(
                        _configuration["EmailConfiguration:SmtpUsername"]!,
                        _configuration["EmailConfiguration:SmtpPassword"]!
                    );

                    client.Send(emailMessage);
                    client.Disconnect(true);
                }
                return Ok("¡Código enviado con éxito!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error SMTP: {ex.Message}");
            }
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(UserResetPasswordDto request)
        {
            // Validamos el PIN (Token) y el Email
            var resetRequest = _context.PasswordResets.FirstOrDefault(r => r.Email == request.Email && r.Token == request.Token);

            if (resetRequest == null)
                return BadRequest("Código de verificación inválido o expirado.");

            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null) return BadRequest("Usuario no encontrado.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.PasswordResets.Remove(resetRequest);
            _context.SaveChanges();

            return Ok("¡Contraseña actualizada con éxito!");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    PostsCount = _context.Posts.Count(p => p.UserId == u.Id)
                })
                .ToListAsync();

            return Ok(users);
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddDays(1), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}