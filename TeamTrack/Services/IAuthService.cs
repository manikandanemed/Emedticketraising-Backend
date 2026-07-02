using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto?> RegisterEmployeeAsync(RegisterRequestDto request);
        Task<RegisterResponseDto?> RegisterProductManagerAsync(RegisterRequestDto request);
        Task<bool> DeactivateEmployeeAsync(int userId);
        Task<bool> ResetPasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> AdminResetPasswordAsync(int employeeId, string newPassword);
    }
}
