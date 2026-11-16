using BusBooking.Server.Data;
using BusBooking.Server.Extensions;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/staff-accounts")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminStaffController : ControllerBase
{
    private static readonly List<RolePermissionDto> RolePermissionsMatrix = new()
    {
        new RolePermissionDto
        {
            Role = UserRoles.Admin,
            Functions = new List<string>
            {
                "Tổng quan hệ thống",
                "Quản lý chuyến xe",
                "Quản lý tuyến xe",
                "Quản lý xe và bảo trì",
                "Quản lý ghế",
                "Quản lý vé",
                "Quản lý quyền truy cập",
                "Tra cứu khách hàng",
                "Hỗ trợ khách hàng",
                "Báo cáo doanh thu"
            }
        },
        new RolePermissionDto
        {
            Role = UserRoles.Staff,
            Functions = new List<string>
            {
                "Tổng quan hệ thống",
                "Quản lý chuyến xe",
                "Quản lý tuyến xe",
                "Quản lý xe và bảo trì",
                "Quản lý ghế",
                "Quản lý vé",
                "Tra cứu khách hàng"
            }
        },
        new RolePermissionDto
        {
            Role = UserRoles.Support,
            Functions = new List<string>
            {
                "Tổng quan hệ thống",
                "Tra cứu khách hàng",
                "Hỗ trợ khách hàng"
            }
        }
    };

    private readonly BusBookingDbContext _db;

    public AdminStaffController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 46. GET /api/admin/staff-accounts
    [HttpGet]
    public async Task<IActionResult> GetStaffAccounts(CancellationToken ct)
    {
        var accounts = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRoles.Staff || u.Role == UserRoles.Support || u.Role == UserRoles.Admin)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new StaffAccountDto
            {
                AccountId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status
            })
            .ToListAsync(ct);

        var data = new StaffAccountsResponseData
        {
            Accounts = accounts,
            Permissions = RolePermissionsMatrix
        };

        return Ok(ApiResponse<StaffAccountsResponseData>.Ok(data, "Thành công"));
    }

    // 47. PUT /api/admin/staff-accounts/{accountId}/lock
    [HttpPut("{accountId:int}/lock")]
    public async Task<IActionResult> LockStaffAccount(int accountId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int currentAdminId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        var account = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == accountId && u.Role != UserRoles.Customer, ct);

        if (account == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản nhân viên"));

        if (account.Id == currentAdminId || account.Role == UserRoles.Admin)
            return BadRequest(ApiResponse.Fail("Không thể khóa tài khoản Quản trị viên"));

        account.Status = AccountStatuses.Locked;
        await _db.SaveChangesAsync(ct);

        var data = new LockStaffResponseData
        {
            AccountId = account.Id,
            Status = account.Status
        };

        return Ok(ApiResponse<LockStaffResponseData>.Ok(data, "Đã khóa tài khoản"));
    }
}
