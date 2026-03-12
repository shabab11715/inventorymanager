using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryManager.Domain.Entities;
using InventoryManager.Infrastructure.Persistence;
using InventoryManager.Infrastructure.Services;
using InventoryManager.WebApi.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManager.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly InventoryManagerDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly ExternalAuthOptions _externalAuthOptions;
    private readonly EmailSender _emailSender;

    public AuthController(
        InventoryManagerDbContext db,
        IOptions<JwtOptions> jwtOptions,
        IOptions<ExternalAuthOptions> externalAuthOptions,
        EmailSender emailSender)
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
        _externalAuthOptions = externalAuthOptions.Value;
        _emailSender = emailSender;
    }

    [HttpPost("register")]
    public async Task<ActionResult<object>> Register([FromBody] RegisterRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var existingUser = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (existingUser is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var token = GenerateToken();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Name = BuildNameFromEmail(normalizedEmail),
            Provider = "local",
            ProviderId = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "user",
            IsBlocked = false,
            IsEmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            VerificationEmailLastSentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var verifyUrl = Url.ActionLink(
            nameof(VerifyEmail),
            values: new { token },
            protocol: Request.Scheme
        );

        await _emailSender.SendAsync(
            user.Email,
            "Verify your email",
            $"""
            <p>Thanks for registering.</p>
            <p>Please verify your email by clicking this link:</p>
            <p><a href="{verifyUrl}">Verify Email</a></p>
            <p>This link expires in 24 hours.</p>
            """
        );

        return Ok(new { message = "Verification email sent. Please check your inbox." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash != PasswordHasher.Hash(request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (user.IsBlocked)
        {
            return Unauthorized(new { message = "Your account is blocked." });
        }

        var token = CreateJwt(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role,
                user.IsBlocked,
                user.IsEmailVerified
            }
        });
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<object>> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is null)
        {
            return NotFound(new { message = "No account found with this email." });
        }

        if (user.IsEmailVerified)
        {
            return BadRequest(new { message = "This account is already verified." });
        }

        if (user.VerificationEmailLastSentAt.HasValue &&
            DateTime.UtcNow < user.VerificationEmailLastSentAt.Value.AddMinutes(1))
        {
            return BadRequest(new { message = "Please wait 1 minute before requesting another email." });
        }

        var token = GenerateToken();

        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        user.VerificationEmailLastSentAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var verifyUrl = Url.ActionLink(
            nameof(VerifyEmail),
            values: new { token },
            protocol: Request.Scheme
        );

        await _emailSender.SendAsync(
            user.Email,
            "Verify your email",
            $"""
            <p>Please verify your email by clicking the link below:</p>
            <p><a href="{verifyUrl}">Verify Email</a></p>
            <p>This link expires in 24 hours.</p>
            """
        );

        return Ok(new { message = "Verification email resent. Please check your inbox." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        token = token?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect(BuildFrontendLoginUrl("verify_invalid"));
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.EmailVerificationToken == token);

        if (user is null)
        {
            return Redirect(BuildFrontendLoginUrl("verify_invalid"));
        }

        if (!user.EmailVerificationTokenExpiresAt.HasValue || user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return Redirect(BuildFrontendLoginUrl("verify_expired"));
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;

        await _db.SaveChangesAsync();

        return Redirect(BuildFrontendLoginUrl("verified"));
    }

    [HttpPost("dev-login")]
    public async Task<ActionResult<object>> DevLogin([FromBody] DevLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "email and name are required" });
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Name = request.Name.Trim(),
                Provider = "dev",
                ProviderId = $"dev:{normalizedEmail}",
                PasswordHash = null,
                Role = "user",
                IsBlocked = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        if (user.IsBlocked)
        {
            return Unauthorized(new { message = "User is blocked." });
        }

        var token = CreateJwt(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role,
                user.IsBlocked,
                user.IsEmailVerified
            }
        });
    }

    [HttpGet("google/start")]
    public IActionResult StartGoogle([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalCallback), new
        {
            schemeName = GoogleDefaults.AuthenticationScheme,
            returnUrl
        });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("github/start")]
    public IActionResult StartGitHub([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalCallback), new
        {
            schemeName = "GitHub",
            returnUrl
        });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, "GitHub");
    }

    [HttpGet("external/callback")]
    public async Task<IActionResult> ExternalCallback([FromQuery] string schemeName, [FromQuery] string? returnUrl = null)
    {
        var authResult = await HttpContext.AuthenticateAsync("Cookies");

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return Redirect(BuildFrontendLoginUrl("external_auth_failed"));
        }

        var principal = authResult.Principal;

        var providerId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? email ?? "User";

        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("Cookies");
            return Redirect(BuildFrontendLoginUrl("external_auth_missing_profile"));
        }

        var normalizedEmail = NormalizeEmail(email);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Provider == schemeName && x.ProviderId == providerId);

        if (user is null)
        {
            user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        }

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                Name = name.Trim(),
                Provider = schemeName,
                ProviderId = providerId,
                Role = "user",
                IsBlocked = false,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            user.Email = normalizedEmail;
            user.Name = name.Trim();
            user.Provider = schemeName;
            user.ProviderId = providerId;
            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();
        }

        await HttpContext.SignOutAsync("Cookies");

        if (user.IsBlocked)
        {
            return Redirect(BuildFrontendLoginUrl("blocked"));
        }

        var token = CreateJwt(user);
        var finalReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? $"{_externalAuthOptions.FrontendBaseUrl}/auth/callback?token={Uri.EscapeDataString(token)}"
            : $"{_externalAuthOptions.FrontendBaseUrl}/auth/callback?token={Uri.EscapeDataString(token)}&returnUrl={Uri.EscapeDataString(returnUrl)}";

        return Redirect(finalReturnUrl);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parsedUserId);

        if (user is null || user.IsBlocked)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Name,
            user.Role,
            user.IsBlocked,
            user.IsEmailVerified
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out" });
    }

    private string CreateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role)
        };

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.ExpMinutes <= 0 ? 60 : _jwtOptions.ExpMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string BuildFrontendLoginUrl(string reason)
    {
        return $"{_externalAuthOptions.FrontendBaseUrl}/login?reason={Uri.EscapeDataString(reason)}";
    }

    private static string NormalizeEmail(string email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string BuildNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }

    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public sealed record RegisterRequest(string Email, string Password);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record ResendVerificationRequest(string Email);
    public sealed record DevLoginRequest(string Email, string Name, bool IsAdmin);
}