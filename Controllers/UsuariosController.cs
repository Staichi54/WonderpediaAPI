using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WonderpediaAPI.Data;
using WonderpediaAPI.DTOs;
using WonderpediaAPI.Models;
using WonderpediaAPI.Services;

namespace WonderpediaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public UsuariosController(
            AppDbContext context,
            IConfiguration configuration,
            EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        private string GenerarToken(Usuario usuario)
{
    string jwtKey = _configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

    string jwtIssuer = _configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");

    string jwtAudience = _configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

    string expireMinutesValue = _configuration["Jwt:ExpireMinutes"]
        ?? throw new InvalidOperationException("Jwt:ExpireMinutes no está configurado.");

    if (!double.TryParse(expireMinutesValue, out double expireMinutes))
    {
        throw new InvalidOperationException("Jwt:ExpireMinutes debe ser un número válido.");
    }

    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Name, usuario.Nombre),
        new Claim(ClaimTypes.Email, usuario.Correo)
    };

    byte[] jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

    var securityKey = new SymmetricSecurityKey(jwtKeyBytes);

    var credentials = new SigningCredentials(
        securityKey,
        SecurityAlgorithms.HmacSha256
    );

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expireMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

        private int ObtenerUsuarioIdDesdeToken()
        {
            string? idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(idClaim, out int usuarioId))
            {
                return usuarioId;
            }

            return 0;
        }

        private UsuarioResponseDto CrearUsuarioResponseDto(Usuario usuario)
        {
            return new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                FinalizarIngles = usuario.FinalizarIngles,
                FinalizarMates = usuario.FinalizarMates,
                FinalizarHistoria = usuario.FinalizarHistoria
            };
        }

        // GET: api/usuarios
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(usuario => new UsuarioResponseDto
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Correo = usuario.Correo,
                    FinalizarIngles = usuario.FinalizarIngles,
                    FinalizarMates = usuario.FinalizarMates,
                    FinalizarHistoria = usuario.FinalizarHistoria
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET: api/usuarios/1
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            return Ok(CrearUsuarioResponseDto(usuario));
        }

        // POST: api/usuarios/registrar
        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<ActionResult> RegistrarUsuario(RegistroDto registro)
        {
            if (string.IsNullOrWhiteSpace(registro.Nombre) ||
                string.IsNullOrWhiteSpace(registro.Correo) ||
                string.IsNullOrWhiteSpace(registro.Password))
            {
                return BadRequest(new { mensaje = "Debes llenar todos los campos" });
            }

            bool nombreExiste = await _context.Usuarios
                .AnyAsync(u => u.Nombre == registro.Nombre);

            if (nombreExiste)
            {
                return BadRequest(new { mensaje = "El nombre de usuario ya existe" });
            }

            bool correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo == registro.Correo);

            if (correoExiste)
            {
                return BadRequest(new { mensaje = "El correo ya está registrado" });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registro.Password);

            Usuario usuario = new Usuario
            {
                Nombre = registro.Nombre,
                Correo = registro.Correo,
                PasswordHash = passwordHash,
                FechaCreacion = DateTime.Now,
                FinalizarIngles = false,
                FinalizarMates = false,
                FinalizarHistoria = false
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            string token = GenerarToken(usuario);

            return Ok(new AuthResponseDto
            {
                Mensaje = "Usuario registrado correctamente",
                Token = token,
                Usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // POST: api/usuarios/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto login)
        {
            if (string.IsNullOrWhiteSpace(login.NombreOCorreo) ||
                string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest(new { mensaje = "Debes llenar usuario/correo y contraseña" });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Nombre == login.NombreOCorreo ||
                    u.Correo == login.NombreOCorreo);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            }

            bool passwordCorrecta;

            try
            {
                passwordCorrecta = BCrypt.Net.BCrypt.Verify(login.Password, usuario.PasswordHash);
            }
            catch
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            }

            if (!passwordCorrecta)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            }

            string token = GenerarToken(usuario);

            return Ok(new AuthResponseDto
            {
                Mensaje = "Inicio de sesión correcto",
                Token = token,
                Usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // PUT: api/usuarios/1/logro/ingles
        [Authorize]
        [HttpPut("{id:int}/logro/ingles")]
        public async Task<IActionResult> FinalizarIngles(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            if (usuario.FinalizarIngles)
            {
                return Ok(new
                {
                    mensaje = "El logro de Inglés ya estaba completado",
                    usuario = CrearUsuarioResponseDto(usuario)
                });
            }

            usuario.FinalizarIngles = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Logro de Inglés actualizado correctamente",
                usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // PUT: api/usuarios/1/logro/mates
        [Authorize]
        [HttpPut("{id:int}/logro/mates")]
        public async Task<IActionResult> FinalizarMates(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            if (usuario.FinalizarMates)
            {
                return Ok(new
                {
                    mensaje = "El logro de Matemáticas ya estaba completado",
                    usuario = CrearUsuarioResponseDto(usuario)
                });
            }

            usuario.FinalizarMates = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Logro de Matemáticas actualizado correctamente",
                usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // PUT: api/usuarios/1/logro/historia
        [Authorize]
        [HttpPut("{id:int}/logro/historia")]
        public async Task<IActionResult> FinalizarHistoria(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            if (usuario.FinalizarHistoria)
            {
                return Ok(new
                {
                    mensaje = "El logro de Historia ya estaba completado",
                    usuario = CrearUsuarioResponseDto(usuario)
                });
            }

            usuario.FinalizarHistoria = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Logro de Historia actualizado correctamente",
                usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // PUT: api/usuarios/1/progreso/reset
        [Authorize]
        [HttpPut("{id:int}/progreso/reset")]
        public async Task<IActionResult> ReiniciarProgreso(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            usuario.FinalizarIngles = false;
            usuario.FinalizarMates = false;
            usuario.FinalizarHistoria = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Progreso reiniciado correctamente",
                usuario = CrearUsuarioResponseDto(usuario)
            });
        }

        // POST: api/usuarios/1/enviar-progreso
        [Authorize]
        [HttpPost("{id:int}/enviar-progreso")]
        public async Task<IActionResult> EnviarProgresoCorreo(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            string estadoIngles = usuario.FinalizarIngles ? "Completado" : "Pendiente";
            string estadoMates = usuario.FinalizarMates ? "Completado" : "Pendiente";
            string estadoHistoria = usuario.FinalizarHistoria ? "Completado" : "Pendiente";

            string materiasFaltantes = "";

            if (!usuario.FinalizarIngles)
            {
                materiasFaltantes += "- Inglés\n";
            }

            if (!usuario.FinalizarMates)
            {
                materiasFaltantes += "- Matemáticas\n";
            }

            if (!usuario.FinalizarHistoria)
            {
                materiasFaltantes += "- Historia\n";
            }

            if (string.IsNullOrEmpty(materiasFaltantes))
            {
                materiasFaltantes = "No te falta ninguna materia. Has completado todo el progreso disponible.";
            }
            else
            {
                materiasFaltantes = "Te falta completar:\n" + materiasFaltantes;
            }

            string asunto = "Progreso en Wonderpedia";

            string cuerpo =
                $"Hola {usuario.Nombre},\n\n" +
                "Este es tu progreso actual en Wonderpedia:\n\n" +
                $"Jugador: {usuario.Nombre}\n" +
                $"Correo: {usuario.Correo}\n\n" +
                "Estado de materias:\n" +
                $"- Inglés: {estadoIngles}\n" +
                $"- Matemáticas: {estadoMates}\n" +
                $"- Historia: {estadoHistoria}\n\n" +
                $"{materiasFaltantes}\n\n" +
                "Gracias por jugar Wonderpedia.";

            try
            {
                await _emailService.EnviarCorreoAsync(usuario.Correo, asunto, cuerpo);

                return Ok(new
                {
                    mensaje = "Correo de progreso enviado correctamente",
                    correo = usuario.Correo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo enviar el correo",
                    error = ex.Message
                });
            }
        }

        // GET: api/usuarios/1/historial-logros
        [Authorize]
        [HttpGet("{id:int}/historial-logros")]
        public async Task<ActionResult<IEnumerable<HistorialLogroDto>>> GetHistorialLogrosUsuario(int id)
        {
            int usuarioIdToken = ObtenerUsuarioIdDesdeToken();

            if (usuarioIdToken != id)
            {
                return Forbid();
            }

            bool usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == id);

            if (!usuarioExiste)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            var historial = await _context.HistorialLogros
                .Where(h => h.UsuarioId == id)
                .OrderByDescending(h => h.FechaLogro)
                .Select(h => new HistorialLogroDto
                {
                    Id = h.Id,
                    UsuarioId = h.UsuarioId,
                    Modulo = h.Modulo,
                    FechaLogro = h.FechaLogro
                })
                .ToListAsync();

            return Ok(historial);
        }

        // GET: api/usuarios/historial-logros
        [Authorize]
        [HttpGet("historial-logros")]
        public async Task<ActionResult<IEnumerable<HistorialLogroDto>>> GetTodosLosHistoriales()
        {
            var historial = await _context.HistorialLogros
                .OrderByDescending(h => h.FechaLogro)
                .Select(h => new HistorialLogroDto
                {
                    Id = h.Id,
                    UsuarioId = h.UsuarioId,
                    Modulo = h.Modulo,
                    FechaLogro = h.FechaLogro
                })
                .ToListAsync();

            return Ok(historial);
        }
    }
}
