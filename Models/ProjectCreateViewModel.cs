using System.ComponentModel.DataAnnotations;

namespace BuildWise.Models;

public class ProjectCreateViewModel
{
    [Required]
    [StringLength(150)]
    public string ProjectName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
