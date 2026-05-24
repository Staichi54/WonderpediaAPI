using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WonderpediaAPI.Models
{
    public class HistorialLogro
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Modulo { get; set; } = string.Empty;

        public DateTime FechaLogro { get; set; } = DateTime.Now;

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}