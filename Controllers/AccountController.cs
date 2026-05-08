using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;
using BuildWise.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Controllers
{
    public class AccountController : Controller
    {
        private readonly BuildWiseDbContext _context;

        public AccountController(BuildWiseDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                // 1. Verify the ID Token
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
                string uid = decodedToken.Uid;
                string email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString()! : "";
                
                // Determine the best name to use
                string name = request.Name ?? "";
                
                // If name from request is empty or just the email, look in claims
                if (string.IsNullOrWhiteSpace(name) || name.Contains("@"))
                {
                    if (decodedToken.Claims.ContainsKey("name") && !decodedToken.Claims["name"].ToString()!.Contains("@"))
                    {
                        name = decodedToken.Claims["name"].ToString()!;
                    }
                }

                // Final fallback: Email prefix
                if (string.IsNullOrWhiteSpace(name) || name.Contains("@"))
                {
                    name = email.Contains("@") ? email.Split('@')[0] : "User";
                }

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { success = false, message = "Email not found in token or claims." });
                }

                // 2. Check if user exists in local SQL DB
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Create new user if they don't exist
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        PasswordHash = "FIREBASE_AUTH", // Handled by Firebase
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // 3. Create Local Cookie Session
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
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
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

        public class FirebaseLoginRequest
        {
            [JsonPropertyName("idToken")]
            public string IdToken { get; set; } = null!;

            [JsonPropertyName("name")]
            public string? Name { get; set; }
            
            // Adding a fallback property name in case the serializer is picky
            [JsonPropertyName("fullName")]
            public string? FullName { get; set; }
        }
    }
}
