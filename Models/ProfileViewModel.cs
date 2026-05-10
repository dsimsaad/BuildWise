using System.ComponentModel.DataAnnotations;

namespace BuildWise.Models;

public class ProfileViewModel
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Profession { get; set; }

    public int ProjectCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
