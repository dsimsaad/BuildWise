using FirebaseAdmin.Auth;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;
using BuildWise.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    public class AccountController : BaseController
    {
        private readonly BuildWiseDbContext _context;

        public AccountController(BuildWiseDbContext context)
        {
            _context = context;
        }

        private async Task<Project> EnsureDefaultProjectAsync(User user)
        {
            // Every signed in user needs one active project so dashboard pages have a project context.
            var existingProject = await _context.Projects
                .Where(p => p.UserId == user.UserId)
                .OrderBy(p => p.ProjectId)
                .FirstOrDefaultAsync();

            if (existingProject != null)
            {
                var legacyProperty = await _context.Properties
                    .FirstOrDefaultAsync(p => p.PropertyId == existingProject.PropertyId && p.UserId == user.UserId && p.ProjectId == null);
                if (legacyProperty != null)
                {
                    legacyProperty.ProjectId = existingProject.ProjectId;
                    legacyProperty.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return existingProject;
            }

            var propertyId = await _context.Properties
                .Where(p => p.UserId == user.UserId)
                .OrderBy(p => p.PropertyId)
                .Select(p => (int?)p.PropertyId)
                .FirstOrDefaultAsync();

            if (!propertyId.HasValue)
            {
                // New users may not have a property yet, so create the minimum record needed for the main project.
                var defaultTypeId = await _context.PropertyTypes
                    .OrderBy(t => t.TypeId)
                    .Select(t => (byte?)t.TypeId)
                    .FirstOrDefaultAsync();
                var defaultStatusId = await _context.PropertyStatuses
                    .OrderBy(s => s.StatusId)
                    .Select(s => (byte?)s.StatusId)
                    .FirstOrDefaultAsync();
                var defaultAreaUnitId = await _context.AreaUnits
                    .OrderBy(a => a.UnitId)
                    .Select(a => (byte?)a.UnitId)
                    .FirstOrDefaultAsync();

                if (!defaultTypeId.HasValue || !defaultStatusId.HasValue || !defaultAreaUnitId.HasValue)
                {
                    throw new InvalidOperationException("Unable to initialize required default project metadata.");
                }

                var defaultProperty = new Property
                {
                    PropertyName = "Default Property",
                    UserId = user.UserId,
                    TypeId = defaultTypeId.Value,
                    StatusId = defaultStatusId.Value,
                    Location = "Not specified",
                    AreaSize = 0,
                    AreaUnitId = defaultAreaUnitId.Value,
                    Notes = "Auto-created for the main project.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Properties.Add(defaultProperty);
                await _context.SaveChangesAsync();
                propertyId = defaultProperty.PropertyId;
            }

            var defaultProject = new Project
            {
                ProjectName = "main",
                PropertyId = propertyId.Value,
                UserId = user.UserId,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalBudget = 0,
                IsCompleted = false
            };
            _context.Projects.Add(defaultProject);
            await _context.SaveChangesAsync();

            var defaultPropertyForProject = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId.Value && p.UserId == user.UserId);
            if (defaultPropertyForProject != null && defaultPropertyForProject.ProjectId == null)
            {
                defaultPropertyForProject.ProjectId = defaultProject.ProjectId;
                defaultPropertyForProject.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return defaultProject;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
                {
                    return BadRequest(new { success = false, message = "Login token was not received. Please try signing in again." });
                }

                if (FirebaseApp.DefaultInstance == null)
                {
                    return BadRequest(new { success = false, message = "Firebase authentication is not configured on the server. Add firebase-admin-sdk.json to the project root or set Firebase:ServiceAccountPath." });
                }

                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
                string email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString()! : "";
                
                string name = request.Name ?? request.FullName ?? "";
                
                // Firebase can return an email as the display name, so fall back to better claims before saving.
                if (string.IsNullOrWhiteSpace(name) || name.Contains("@"))
                {
                    if (decodedToken.Claims.ContainsKey("name") && !decodedToken.Claims["name"].ToString()!.Contains("@"))
                    {
                        name = decodedToken.Claims["name"].ToString()!;
                    }
                }

                if (string.IsNullOrWhiteSpace(name) || name.Contains("@"))
                {
                    name = email.Contains("@") ? email.Split('@')[0] : "User";
                }

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { success = false, message = "Email not found in token or claims." });
                }

                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // The password hash is a marker because Firebase owns the real password check.
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        PasswordHash = "FIREBASE_AUTH",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                var activeProject = await EnsureDefaultProjectAsync(user);
                HttpContext.Session.SetInt32("SelectedProjectId", activeProject.ProjectId);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.UserId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    });

                return Ok(new { success = true, redirectUrl = "/Home/Dashboard" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Profession = user.Profession,
                CreatedAt = user.CreatedAt,
                ProjectCount = await _context.Projects.CountAsync(p => p.UserId == userId)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            int userId = GetUserId();
            if (userId == 0 || model.UserId != userId) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                model.CreatedAt = user.CreatedAt;
                model.ProjectCount = await _context.Projects.CountAsync(p => p.UserId == userId);
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            user.Profession = string.IsNullOrWhiteSpace(model.Profession) ? null : model.Profession.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var identity = (ClaimsIdentity?)User.Identity;
            var nameClaim = identity?.FindFirst(ClaimTypes.Name);
            if (identity != null && nameClaim != null)
            {
                identity.RemoveClaim(nameClaim);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    });
            }

            TempData["ProfileSaved"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        public class FirebaseLoginRequest
        {
            [JsonPropertyName("idToken")]
            public string IdToken { get; set; } = null!;

            [JsonPropertyName("name")]
            public string? Name { get; set; }
            
            [JsonPropertyName("fullName")]
            public string? FullName { get; set; }
        }
    }
}
