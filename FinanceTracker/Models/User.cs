using System;
using System.ComponentModel.DataAnnotations;
namespace FinanceTracker.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string Role { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
