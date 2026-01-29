using System.ComponentModel.DataAnnotations;

namespace MyMvcApp.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        public Product Product { get; set; } = null!;
    }
}
