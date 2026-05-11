using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class User
{
    /// <summary>
    /// Auto-increment primary key for user accounts
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Full display name of the owner/user
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Unique login email; used as username
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// BCrypt hashed password — never store plain text
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Optional contact number
    /// </summary>
    public string? PhoneNumber { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 1 = active account, 0 = soft-deleted
    /// </summary>
    public bool IsActive { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}

