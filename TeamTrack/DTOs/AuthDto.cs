namespace TeamTrack.DTOs
{
    public class LoginRequestDto
    {
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterResponseDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminResetPasswordRequestDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminUpdateEmailRequestDto
    {
        public string NewEmail { get; set; } = string.Empty;
    }

    public class SwitchRoleRequestDto
    {
        public string TargetRole { get; set; } = string.Empty;
    }

    public class UpdateEmailRequestDto
    {
        public string NewEmail { get; set; } = string.Empty;
    }

    public class LoginStep1ResultDto
    {
        public bool OtpRequired { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public class VerifyOtpRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }
}
