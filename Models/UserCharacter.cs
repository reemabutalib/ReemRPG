using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ReemRPG.Models
{
    public class UserCharacter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int CharacterId { get; set; }

        public int Level { get; set; } = 1;

        public int Experience { get; set; } = 0;

        public int Gold { get; set; } = 0;

        public bool IsSelected { get; set; } = false;

        // Navigation properties
        [ForeignKey("CharacterId")]
        public virtual Character Character { get; set; }

        // Add User navigation property if your views expect it
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

    }
}