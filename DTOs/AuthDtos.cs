namespace WonderpediaAPI.DTOs
{
    public class RegistroDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string NombreOCorreo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool FinalizarIngles { get; set; }
        public bool FinalizarMates { get; set; }
        public bool FinalizarHistoria { get; set; }
    }

    public class AuthResponseDto
    {
        public string Mensaje { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UsuarioResponseDto Usuario { get; set; } = new UsuarioResponseDto();
    }

    public class HistorialLogroDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public DateTime FechaLogro { get; set; }
    }
}