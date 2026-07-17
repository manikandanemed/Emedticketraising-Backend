using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeamTrack.DTOs;
using TeamTrack.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TeamTrack.Repositories;
using TeamTrack.Models;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(ApiResponse<string>.FailureResponse("All fields are required"));

            var result = await _authService.LoginStep1Async(request);
            if (result == null)
                return Unauthorized(ApiResponse<string>.FailureResponse("Invalid credentials"));

            var message = result.OtpRequired ? "OTP sent to your email" : "Login successful";
            return Ok(ApiResponse<LoginStep1ResultDto>.SuccessResponse(result, message));
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            if (string.IsNullOrEmpty(request.UserType) ||
                string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Otp))
                return BadRequest(ApiResponse<string>.FailureResponse("Username, OTP and User Type are required"));

            var result = await _authService.VerifyOtpAsync(request);
            if (result == null)
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid or expired OTP"));

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful"));
        }

        [HttpPost("switch-role")]
        [Authorize]
        public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequestDto request)
        {
            if (string.IsNullOrEmpty(request.TargetRole))
                return BadRequest(ApiResponse<string>.FailureResponse("Target role is required"));

            if (request.TargetRole != "Employee" && request.TargetRole != "ProductManager")
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid target role"));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<string>.FailureResponse("Unauthorized"));
            int userId = int.Parse(userIdClaim.Value);

            var result = await _authService.SwitchRoleAsync(userId, request.TargetRole);
            if (result == null)
                return BadRequest(ApiResponse<string>.FailureResponse("Role switch not allowed for this user"));

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Role switched successfully"));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(ApiResponse<string>.FailureResponse("Email is required"));

            var success = await _authService.ForgotPasswordAsync(request.Email);
            if (!success)
                return BadRequest(ApiResponse<string>.FailureResponse("User not found or inactive"));

            return Ok(ApiResponse<string>.SuccessResponse("Success", "Reset verification code sent to your email"));
        }

        [HttpPost("reset-password-otp")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest(ApiResponse<string>.FailureResponse("Email, OTP and New Password are required"));

            var success = await _authService.ResetPasswordWithOtpAsync(request.Email, request.Otp, request.NewPassword);
            if (!success)
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid or expired OTP"));

            return Ok(ApiResponse<string>.SuccessResponse("Success", "Password has been reset successfully"));
        }

        [HttpPost("register")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(ApiResponse<string>.FailureResponse("Name, Email and Password are required"));

            if (request.UserType != "Employee" && request.UserType != "ProductManager" && request.UserType != "Both")
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid UserType role selected"));
            }

            var result = await _authService.CreateUserAsync(request);
            if (result == null)
                return Conflict(ApiResponse<string>.FailureResponse("Email already exists"));

            return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(result, "User registered successfully"));
        }

        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(ApiResponse<string>.FailureResponse("Name, Email and Password are required"));

            var result = await _authService.RegisterEmployeeAsync(request);
            if (result == null)
                return Conflict(ApiResponse<string>.FailureResponse("Email already exists"));

            return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(result, "Employee registered successfully"));
        }

        [HttpPost("register/productmanager")]
        public async Task<IActionResult> RegisterProductManager([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(ApiResponse<string>.FailureResponse("Name, Email and Password are required"));

            var result = await _authService.RegisterProductManagerAsync(request);
            if (result == null)
                return Conflict(ApiResponse<string>.FailureResponse("Email already exists"));

            return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(result, "Product Manager registered successfully"));
        }

        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Current and new passwords are required"));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<string>.FailureResponse("Unauthorized"));
            int userId = int.Parse(userIdClaim.Value);

            var success = await _authService.ResetPasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid current password"));
            }

            return Ok(ApiResponse<string>.SuccessResponse("Success", "Password reset successfully"));
        }

        [HttpPut("update-email")]
        [Authorize]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequestDto request, [FromServices] IRepository<User> userRepo)
        {
            if (string.IsNullOrEmpty(request.NewEmail) || !request.NewEmail.Contains("@"))
                return BadRequest(ApiResponse<string>.FailureResponse("A valid email is required"));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<string>.FailureResponse("Unauthorized"));
            int userId = int.Parse(userIdClaim.Value);

            var emailLower = request.NewEmail.Trim().ToLower();

            var duplicate = await userRepo.GetAsync(u => u.Email.ToLower() == emailLower && u.Id != userId);
            if (duplicate != null)
                return Conflict(ApiResponse<string>.FailureResponse("This email is already in use"));

            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user == null) return NotFound(ApiResponse<string>.FailureResponse("User not found"));

            user.Email = emailLower;
            await userRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse(emailLower, "Email updated successfully"));
        }

        [HttpPost("profile-picture")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file, [FromServices] IRepository<User> userRepo, [FromServices] IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("No file uploaded"));
            }

            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed."));
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("File size exceeds 5MB limit."));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<string>.FailureResponse("Unauthorized"));
            int userId = int.Parse(userIdClaim.Value);

            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            // Create directory if not exists
            var uploadDir = Path.Combine(env.WebRootPath, "uploads", "profile_photos");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Delete old photo if it exists
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldPath = Path.Combine(env.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* Ignore delete error */ }
                }
            }

            // Generate unique filename to prevent caching
            var filename = $"profile_user_{userId}_{DateTime.UtcNow.Ticks}{extension}";
            var filepath = Path.Combine(uploadDir, filename);

            using (var stream = new FileStream(filepath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user record
            var relativePath = $"/uploads/profile_photos/{filename}";
            user.ProfilePicture = relativePath;
            await userRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse(relativePath, "Profile picture uploaded successfully"));
        }

        [HttpDelete("profile-picture")]
        [Authorize]
        public async Task<IActionResult> RemoveProfilePicture([FromServices] IRepository<User> userRepo, [FromServices] IWebHostEnvironment env)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<string>.FailureResponse("Unauthorized"));
            int userId = int.Parse(userIdClaim.Value);

            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResponse("User not found"));
            }

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldPath = Path.Combine(env.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* Ignore delete error */ }
                }
                user.ProfilePicture = null;
                await userRepo.SaveAsync();
            }

            return Ok(ApiResponse<string>.SuccessResponse("Success", "Profile picture removed successfully"));
        }
    }
}
