using System.ComponentModel.DataAnnotations;

namespace App.Models;

public abstract class BaseModel
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}