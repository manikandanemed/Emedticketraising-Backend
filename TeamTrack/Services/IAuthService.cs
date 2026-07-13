using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IAuthService
    {
        Task<LoginStep1ResultDto?> LoginStep1Async(LoginRequestDto request);
        Task<LoginResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto request);
        Task<RegisterResponseDto?> RegisterEmployeeAsync(RegisterRequestDto request);
        Task<RegisterResponseDto?> RegisterProductManagerAsync(RegisterRequestDto request);
        Task<RegisterResponseDto?> CreateUserAsync(RegisterRequestDto request);
        Task<bool> DeactivateEmployeeAsync(int userId);
        Task<bool> ResetPasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> AdminResetPasswordAsync(int employeeId, string newPassword);
        Task<LoginResponseDto?> SwitchRoleAsync(int userId, string targetRole);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
    }
}
