using KrishivVideoUploader.Modal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KrishivVideoUploader.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : ControllerBase
    {
        private const string BunnySigningKey = "f66a1c15-9efc-4c59-8ce2-80c61e530b8d";
        private const string BunnyBaseUrl = "https://krishivtestvideo.b-cdn.net"; // No trailing slash
        private readonly AppDbContext _dbContext;

        public VideoController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet("token")]
        public IActionResult GetSecureVideoUrl(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return BadRequest("Missing file");

            long expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;
            string tokenPath = ""; // Allow access to all files in /videos/

            // Base string for signing
            string baseString = $"{BunnySigningKey}{tokenPath}{expires}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            string token = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", ""); // Bunny-safe Base64

            string signedUrl = $"{BunnyBaseUrl}/{file}?token={token}&expires={expires}";

            Console.WriteLine($"Generated Signed URL: {signedUrl}");
            return Ok(new { signedUrl });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password");
            }

            // Optionally set auth cookie or return JWT
            return Ok(new
            {
                userId = user.Id,
                isAdmin=user.IsAdmin,
            });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            // Check if username or email already exists
            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest("Username or email already exists.");
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { userId = user.Id });
        }
        [HttpGet("{userId}")]
        public async Task<IActionResult> IsSubscribed(Guid userId)
        {
            var sub = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            bool isActive = sub != null && sub.IsSubscribed && (sub.ExpiryDate == null || sub.ExpiryDate > DateTime.UtcNow);

            return Ok(new { isSubscribed = isActive });
        }
        [HttpPost("user/{userId}/subscribe")]
        public async Task<IActionResult> SubscribeUser(Guid userId)
        {
            var sub = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (sub == null)
            {
                _dbContext.UserSubscriptions.Add(new UserSubscription
                {
                    UserId = userId,
                    IsSubscribed = true,
                    SubscribedOn = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddMonths(1)
                });
            }
            else
            {
                sub.IsSubscribed = true;
                sub.SubscribedOn = DateTime.UtcNow;
                sub.ExpiryDate = DateTime.UtcNow.AddMonths(1);
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true });
        }


        [HttpGet("user/{userId}/subscription")]
        public async Task<IActionResult> GetSubscriptionStatus(Guid userId)
        {
            var sub = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            bool isActive = sub != null && sub.IsSubscribed && (sub.ExpiryDate == null || sub.ExpiryDate > DateTime.UtcNow);

            return Ok(new { isSubscribed = isActive });
        }


    }
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

