using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public abstract class BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] [Required(ErrorMessage = "Original URL is required")]
    [Url(ErrorMessage = "Please provide a valid URL")]
    [MaxLength(2048, ErrorMessage = "URL cannot exceed 2048 characters")]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}