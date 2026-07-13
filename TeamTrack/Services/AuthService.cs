using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly IRepository<UserOtp> _otpRepo;

        public AuthService(IRepository<User> userRepo, IConfiguration config, IMemoryCache cache, IEmailService emailService, IRepository<UserOtp> otpRepo)
        {
            _userRepo = userRepo;
            _config = config;
            _cache = cache;
            _emailService = emailService;
            _otpRepo = otpRepo;
        }

        public async Task<LoginStep1ResultDto?> LoginStep1Async(LoginRequestDto request)
        {
            var user = await _userRepo.GetAsync(u => u.Email.ToLower() == request.UserName.ToLower() && u.IsActive);
            if (user == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            // Generate a random 6 digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // Store in DB for 10 minutes
            var existingOtps = await _otpRepo.Query()
                .Where(o => o.Email.ToLower() == user.Email.ToLower() && o.Purpose == "Login")
                .ToListAsync();
            foreach (var oldOtp in existingOtps)
            {
                _otpRepo.Remove(oldOtp);
            }

            var userOtp = new UserOtp
            {
                Email = user.Email.ToLower(),
                OtpCode = otp,
                Purpose = "Login",
                ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow
            };
            await _otpRepo.AddAsync(userOtp);
            await _otpRepo.SaveAsync();

            // Send OTP via email
            var subject = "Your OTP for Emedticket Verification";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px; max-width: 500px;'>
                    <h2 style='color: #0078d4;'>Emedlogix Solutions</h2>
                    <p>Dear {user.Name},</p>
                    <p>You requested to log into the Emedticket Portal. Please use the following One-Time Password (OTP) to complete your login. This OTP is valid for 10 minutes.</p>
                    <div style='background-color: #f3f2f1; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #333; margin: 20px 0; border-radius: 5px;'>
                        {otp}
                    </div>
                    <p style='color: #666; font-size: 12px;'>If you did not request this, please ignore this email or contact support.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            var roles = new List<string>();
            if (user.UserType == "Both")
            {
                roles.Add("ProductManager");
                roles.Add("Employee");
            }
            else
            {
                roles.Add(user.UserType);
            }

            return new LoginStep1ResultDto
            {
                OtpRequired = true,
                Email = user.Email,
                Roles = roles
            };
        }

        public async Task<LoginResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            var cachedOtp = await _otpRepo.GetAsync(o => o.Email.ToLower() == request.UserName.ToLower() && o.Purpose == "Login" && o.ExpiryTime > DateTime.UtcNow);
            if (cachedOtp == null)
            {
                return null; // OTP expired or not found
            }

            if (cachedOtp.OtpCode != request.Otp)
            {
                return null; // Incorrect OTP
            }

            // OTP verified, remove from DB
            _otpRepo.Remove(cachedOtp);
            await _otpRepo.SaveAsync();

            var user = await _userRepo.GetAsync(u => u.Email.ToLower() == request.UserName.ToLower() && u.IsActive);
            if (user == null) return null;

            if (user.UserType != "Both" && user.UserType != request.UserType)
            {
                return null;
            }

            var token = GenerateJwtToken(user, request.UserType);

            var roles = new List<string>();
            if (user.UserType == "Both")
            {
                roles.Add("ProductManager");
                roles.Add("Employee");
            }
            else
            {
                roles.Add(user.UserType);
            }

            return new LoginResponseDto
            {
                Token = token,
                UserType = request.UserType,
                Name = user.Name,
                UserId = user.Id,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Roles = roles
            };
        }

        public async Task<RegisterResponseDto?> RegisterEmployeeAsync(RegisterRequestDto request)
        {
            var exists = await _userRepo.GetAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (exists != null) return null;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Mobile = request.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                UserType = "Employee",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveAsync();

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserType = user.UserType,
                Message = "Employee registered successfully"
            };
        }

        public async Task<RegisterResponseDto?> RegisterProductManagerAsync(RegisterRequestDto request)
        {
            var exists = await _userRepo.GetAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (exists != null) return null;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Mobile = request.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                UserType = "ProductManager",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };


            await _userRepo.AddAsync(user);
            await _userRepo.SaveAsync();

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserType = user.UserType,
                Message = "Product Manager registered successfully"
            };
        }

        public async Task<RegisterResponseDto?> CreateUserAsync(RegisterRequestDto request)
        {
            var exists = await _userRepo.GetAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (exists != null) return null;

            var user = new User
            {
                Name = request.Name,
                Email = request.Email.ToLower(),
                Mobile = request.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                UserType = request.UserType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveAsync();

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserType = user.UserType,
                Message = "User registered successfully"
            };
        }

        private string GenerateJwtToken(User user, string selectedRole)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, selectedRole),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim("role", selectedRole)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> DeactivateEmployeeAsync(int userId)
        {
            var user = await _userRepo.GetAsync(u => u.Id == userId && (u.UserType == "Employee" || u.UserType == "Both"));
            if (user == null) return false;

            user.IsActive = false;
            await _userRepo.SaveAsync();
            return true;
        }

        public async Task<LoginResponseDto?> SwitchRoleAsync(int userId, string targetRole)
        {
            var user = await _userRepo.GetAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;

            if (user.UserType != "Both" && user.UserType != targetRole)
            {
                return null;
            }

            var token = GenerateJwtToken(user, targetRole);

            var roles = new List<string>();
            if (user.UserType == "Both")
            {
                roles.Add("ProductManager");
                roles.Add("Employee");
            }
            else
            {
                roles.Add(user.UserType);
            }

            return new LoginResponseDto
            {
                Token = token,
                UserType = targetRole,
                Name = user.Name,
                UserId = user.Id,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Roles = roles
            };
        }

        public async Task<bool> ResetPasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepo.GetAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.SaveAsync();
            return true;
        }

        public async Task<bool> AdminResetPasswordAsync(int employeeId, string newPassword)
        {
            var user = await _userRepo.GetAsync(u => u.Id == employeeId && u.IsActive);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.SaveAsync();
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userRepo.GetAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
            if (user == null) return false;

            // Generate a random 6 digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // Store in DB for 10 minutes
            var existingOtps = await _otpRepo.Query()
                .Where(o => o.Email.ToLower() == user.Email.ToLower() && o.Purpose == "ResetPassword")
                .ToListAsync();
            foreach (var oldOtp in existingOtps)
            {
                _otpRepo.Remove(oldOtp);
            }

            var userOtp = new UserOtp
            {
                Email = user.Email.ToLower(),
                OtpCode = otp,
                Purpose = "ResetPassword",
                ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow
            };
            await _otpRepo.AddAsync(userOtp);
            await _otpRepo.SaveAsync();

            // Send OTP via email
            var subject = "Password Reset Verification Code - Emedticket";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px; max-width: 500px;'>
                    <h2 style='color: #0078d4;'>Emedlogix Solutions</h2>
                    <p>Dear {user.Name},</p>
                    <p>We received a request to reset the password for your Emedticket account. Please use the following One-Time Password (OTP) code to verify your identity. This OTP is valid for 10 minutes.</p>
                    <div style='background-color: #f3f2f1; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #333; margin: 20px 0; border-radius: 5px;'>
                        {otp}
                    </div>
                    <p style='color: #666; font-size: 12px;'>If you did not request a password reset, please ignore this email.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, subject, body);
            return true;
        }

        public async Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            var user = await _userRepo.GetAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
            if (user == null) return false;

            var cachedOtp = await _otpRepo.GetAsync(o => o.Email.ToLower() == email.ToLower() && o.Purpose == "ResetPassword" && o.ExpiryTime > DateTime.UtcNow);
            if (cachedOtp == null)
            {
                return false; // Expired or not requested
            }

            if (cachedOtp.OtpCode != otp)
            {
                return false; // Incorrect OTP
            }

            // OTP verified, remove from DB
            _otpRepo.Remove(cachedOtp);
            await _otpRepo.SaveAsync();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.SaveAsync();
            return true;
        }
    }
}