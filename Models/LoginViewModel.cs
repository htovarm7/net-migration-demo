using System.ComponentModel.DataAnnotations;

namespace LegacyWeb.Models
{
    public class LoginViewModel
    {
        [Required]
        public string User { get; set; }

        [Required]
        public string Password { get; set; }

        public string ReturnPath { get; set; }
    }
}
