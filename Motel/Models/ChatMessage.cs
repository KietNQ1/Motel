using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Motel.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        // Khóa ngoại nối sang ApplicationUser (kiểu int)
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public string Role { get; set; } // "user" hoặc "model"
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}