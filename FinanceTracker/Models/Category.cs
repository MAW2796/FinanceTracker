using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceTracker.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama kategori wajib diisi")]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; } 

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }
    }
}