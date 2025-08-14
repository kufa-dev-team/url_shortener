using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs.UrlMapping
{
    public class UpdateUrlMappingRequest
    {
    [Required][Range(1, int.MaxValue)]
    public int Id { get; set; }

    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Url]
    public required string OriginalUrl { get; set; }

    public DateTime? ExpiresAt { get; set; }

    }
}