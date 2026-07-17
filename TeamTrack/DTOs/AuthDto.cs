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
        public string UserType { get; set; } = "Employee";
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

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordWithOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
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

        // True for OTP-exempt accounts that hold more than one role ("Both").
        // The frontend must still show the role picker (no OTP box) and call
        // verify-otp with the chosen role to finish logging in.
        public bool RoleSelectionRequired { get; set; }

        // Populated only when OtpRequired is false AND RoleSelectionRequired is false
        // (single-role OTP-exempt account) so the frontend can log the user straight in
        // without a separate verify-otp call.
        public string? Token { get; set; }
        public string? UserType { get; set; }
        public string? Name { get; set; }
        public int? UserId { get; set; }
        public string? ProfilePicture { get; set; }
    }

    public class VerifyOtpRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }
}
