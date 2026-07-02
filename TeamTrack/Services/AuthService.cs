using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IConfiguration _config;

        public AuthService(IRepository<User> userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepo.GetAsync(u => u.Email.ToLower() == request.UserName.ToLower() && u.UserType == request.UserType && u.IsActive);
            if (user == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                UserType = user.UserType,
                Name = user.Name,
                UserId = user.Id,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture
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

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.UserType),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim("role", user.UserType)
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
            var user = await _userRepo.GetAsync(u => u.Id == userId && u.UserType == "Employee");
            if (user == null) return false;

            user.IsActive = false;
            await _userRepo.SaveAsync();
            return true;
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
    }
}