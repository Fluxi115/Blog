using FreakBlog.API.Data;
using FreakBlog.API.Dtos;
using FreakBlog.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; //Importante para leer el ID del usuario

namespace FreakBlog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        public PostsController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        //LEER TODOS LOS POSTS (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(posts);
        }

        //LEER UN POST POR SLUG (GET) slug hace que el url sea mas entendible por ejemplo we: no saldra http://localhost:5173/8542 si no que va a salir http://localhost:5173/admin
        [HttpGet("{slug}")]
        public async Task<ActionResult<Post>> GetPostBySlug(string slug)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (post == null)
                return NotFound("El post que buscas no existe.");

            post.FireCount = await _context.PostReactions.CountAsync(r => r.PostId == post.Id && r.Type == "fire");
            post.HeartCount = await _context.PostReactions.CountAsync(r => r.PostId == post.Id && r.Type == "heart");

            return Ok(post);
        }

        //OBTENER POST POR ID (Para cargar el Editor)
        [HttpGet("edit/{id}")]
        [Authorize]
        public async Task<ActionResult<Post>> GetPostById(long id)
        {
            // Busca el post por ID e incluimos al usuario por si acaso
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound("El post no existe.");

            // OPCIONAL WE: se podría validar aquí también que solo el dueño lo cargue, 
            // pero como el [HttpPut] ya protege el guardado, esto es seguro. ya tu decides si lo dejas we
            return Ok(post);
        }

        //CREAR UN POST (POST)
        [HttpPost]
        [Authorize(Roles = "admin,user")]
        public async Task<IActionResult> CreatePost([FromForm] PostCreateDto request)
        {
            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return BadRequest("Autor no encontrado.");

                string? imagePath = null;
                if (request.FeaturedImage != null)
                    imagePath = await SaveFile(request.FeaturedImage, "images");

                var newPost = new Post
                {
                    Title = request.Title,
                    Slug = request.Title.ToLower().Replace(" ", "-").Replace("?", ""),
                    Content = request.Content,
                    Summary = request.Summary,
                    FeaturedImage = imagePath,
                    UserId = request.UserId,
                    Status = "PENDIENTE",
                    PublishedAt = DateTime.UtcNow,
                    FireCount = 0,
                    HeartCount = 0
                };

                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync();

                return Ok(newPost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        //ACTUALIZAR UN POST (PUT)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(long id, [FromForm] PostCreateDto request)
        {
            // 1. Obtener datos del usuario actual desde el Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var currentUserId = long.Parse(userIdClaim);

            // 2. Buscar el post
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            //Solo el dueño o el admin pueden editar
            if (post.UserId != currentUserId && userRole != "admin")
            {
                return Forbid("No tienes permiso para editar el contenido de otro autor.");
            }

            //cambios
            post.Title = request.Title;
            post.Content = request.Content;
            post.Summary = request.Summary;
            post.UpdatedAt = DateTime.UtcNow;

            // Solo actualizamos el slug si cambió el título (opcional pero recomendado)
            post.Slug = request.Title.ToLower().Replace(" ", "-").Replace("?", "");

            if (request.FeaturedImage != null)
                post.FeaturedImage = await SaveFile(request.FeaturedImage, "images");

            await _context.SaveChangesAsync();
            return Ok(post);
        }

        //CAMBIAR ESTADO (ADMIN)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] string newStatus)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.Status = newStatus.ToUpper();
            await _context.SaveChangesAsync();
            return Ok();
        }

        //REACCIONES ÚNICAS (TOGGLE SYSTEM)
        [HttpPost("{id}/react")]
        [Authorize] // 👈 Ahora es obligatorio estar logueado
        public async Task<IActionResult> React(long id, [FromBody] string reactionType)
        {
            // 1. Obtener ID del usuario del Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = long.Parse(userIdClaim);

            // 2. Buscar si ya existe la reacción
            var existingReaction = await _context.PostReactions
                .FirstOrDefaultAsync(r => r.PostId == id && r.UserId == userId && r.Type == reactionType);

            if (existingReaction != null)
            {
                // Si existe, la quitamos (Toggle OFF)
                _context.PostReactions.Remove(existingReaction);
            }
            else
            {
                // Si no existe, la creamos (Toggle ON)
                _context.PostReactions.Add(new PostReaction
                {
                    PostId = id,
                    UserId = userId,
                    Type = reactionType
                });
            }

            await _context.SaveChangesAsync();

            // 3. Contar el total actualizado de ese tipo para ese post
            var totalCount = await _context.PostReactions.CountAsync(r => r.PostId == id && r.Type == reactionType);

            return Ok(new { count = totalCount });
        }

        //BORRAR UN POST (DELETE)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(long id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var currentUserId = long.Parse(userIdClaim);
            var post = await _context.Posts.FindAsync(id);

            if (post == null) return NotFound();

            if (post.UserId != currentUserId && userRole != "admin")
            {
                return Forbid("No tienes permiso para borrar el contenido de otro autor.");
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok("Post eliminado.");
        }

        //AYUDA A GUARDAR ARCHIVOS
        private async Task<string> SaveFile(IFormFile file, string folderName)
        {
            var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(wwwroot, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{fileName}";
        }
    }
}