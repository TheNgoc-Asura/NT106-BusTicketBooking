using System.ComponentModel.DataAnnotations;
using BusBooking.Shared.Enums;

namespace BusBooking.Server.Entities;

public class OtpCode
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Purpose { get; set; } = OtpPurposes.Register;

    [MaxLength(64)]
    public string? TempToken { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
