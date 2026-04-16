using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FreakBlog.API.Data;
using System.Linq;

namespace FreakBlog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;

        public AdminController(DataContext context)
        {
            _context = context;
        }

        // Ruta para ver a todos los usuarios
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                u.Name, // <--- ¡Corregido! Ahora coincide con el diagrama
                u.Email,
                u.Role
            }).ToList();

            return Ok(users);
        }
    }
}