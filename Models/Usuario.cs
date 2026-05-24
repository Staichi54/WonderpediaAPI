using System.ComponentModel.DataAnnotations;

namespace WonderpediaAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool FinalizarIngles { get; set; } = false;

        public bool FinalizarMates { get; set; } = false;

        public bool FinalizarHistoria { get; set; } = false;

        public ICollection<HistorialLogro> HistorialLogros { get; set; } = new List<HistorialLogro>();
    }
}