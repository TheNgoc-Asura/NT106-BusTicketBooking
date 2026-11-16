using System.Globalization;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Server.Helpers;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff}")]
public class AdminVehiclesController : ControllerBase
{
    private static readonly HashSet<string> ValidVehicleStatuses = new(StringComparer.Ordinal)
    {
        VehicleStatuses.Active,
        VehicleStatuses.Maintenance,
        VehicleStatuses.Inactive
    };

    private readonly BusBookingDbContext _db;

    public AdminVehiclesController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 34. GET /api/admin/vehicles
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles(
        [FromQuery] string? licensePlate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Vehicles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            string plateFilter = licensePlate.Trim();
            query = query.Where(v => v.LicensePlate.Contains(plateFilter));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(v => v.Status == statusFilter);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(v => v.LicensePlate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new AdminVehicleDto
            {
                VehicleId = v.Id,
                LicensePlate = v.LicensePlate,
                VehicleTypeId = v.VehicleTypeId,
                VehicleType = v.VehicleType.Name,
                Status = v.Status
            })
            .ToListAsync(ct);

        var paged = new PagedResult<AdminVehicleDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<AdminVehicleDto>>.Ok(paged, "Thành công"));
    }

    // 35. GET /api/admin/maintenance
    [HttpGet("maintenance")]
    public async Task<IActionResult> GetMaintenanceOverview(CancellationToken ct)
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.LicensePlate)
            .Select(v => new AdminVehicleDto
            {
                VehicleId = v.Id,
                LicensePlate = v.LicensePlate,
                VehicleTypeId = v.VehicleTypeId,
                VehicleType = v.VehicleType.Name,
                Status = v.Status
            })
            .ToListAsync(ct);

        var schedules = await _db.MaintenanceSchedules
            .AsNoTracking()
            .OrderByDescending(m => m.StartDate)
            .ThenByDescending(m => m.Id)
            .Select(m => new MaintenanceScheduleDto
            {
                Id = m.Id,
                VehicleId = m.VehicleId,
                LicensePlate = m.Vehicle.LicensePlate,
                StartDate = m.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDate = m.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Note = m.Note
            })
            .ToListAsync(ct);

        var data = new MaintenanceOverviewDto
        {
            Vehicles = vehicles,
            Schedules = schedules
        };

        return Ok(ApiResponse<MaintenanceOverviewDto>.Ok(data, "Thành công"));
    }

    // 36. GET /api/admin/vehicle-types
    [HttpGet("vehicle-types")]
    public async Task<IActionResult> GetVehicleTypes(CancellationToken ct)
    {
        var types = await _db.VehicleTypes
            .AsNoTracking()
            .OrderBy(vt => vt.SeatCount)
            .Select(vt => new VehicleTypeDto
            {
                Id = vt.Id,
                Name = vt.Name,
                SeatCount = vt.SeatCount
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<VehicleTypeDto>>.Ok(types, "Thành công"));
    }

    // 37. POST /api/admin/vehicles
    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.LicensePlate) || request.VehicleTypeId <= 0)
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));

        string plate = request.LicensePlate.Trim().ToUpperInvariant();

        bool typeExists = await _db.VehicleTypes.AsNoTracking().AnyAsync(vt => vt.Id == request.VehicleTypeId, ct);
        if (!typeExists)
            return BadRequest(ApiResponse.Fail("Loại xe không tồn tại"));

        bool plateExists = await _db.Vehicles.AsNoTracking().AnyAsync(v => v.LicensePlate.ToUpper() == plate, ct);
        if (plateExists)
            return Conflict(ApiResponse.Fail("Biển số xe đã tồn tại"));

        var vehicle = new Vehicle
        {
            LicensePlate = plate,
            VehicleTypeId = request.VehicleTypeId,
            Status = VehicleStatuses.Active,
            CreatedAt = DateTime.Now
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<object>.Ok(new { vehicleId = vehicle.Id }, "Thêm xe thành công"));
    }

    // 38. GET /api/admin/vehicles/{vehicleId}
    [HttpGet("vehicles/{vehicleId:int}")]
    public async Task<IActionResult> GetVehicleDetail(int vehicleId, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == vehicleId)
            .Select(v => new AdminVehicleDto
            {
                VehicleId = v.Id,
                LicensePlate = v.LicensePlate,
                VehicleTypeId = v.VehicleTypeId,
                VehicleType = v.VehicleType.Name,
                Status = v.Status
            })
            .FirstOrDefaultAsync(ct);

        if (vehicle == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy xe"));

        return Ok(ApiResponse<AdminVehicleDto>.Ok(vehicle, "Thành công"));
    }

    // 39. PUT /api/admin/vehicles/{vehicleId}
    [HttpPut("vehicles/{vehicleId:int}")]
    public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.LicensePlate) || request.VehicleTypeId <= 0)
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));

        string? targetStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            targetStatus = request.Status.Trim();
            if (!ValidVehicleStatuses.Contains(targetStatus))
                return BadRequest(ApiResponse.Fail("Trạng thái xe không hợp lệ"));
        }

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, ct);
        if (vehicle == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy xe"));

        bool typeExists = await _db.VehicleTypes.AsNoTracking().AnyAsync(vt => vt.Id == request.VehicleTypeId, ct);
        if (!typeExists)
            return BadRequest(ApiResponse.Fail("Loại xe không tồn tại"));

        string plate = request.LicensePlate.Trim().ToUpperInvariant();
        bool duplicatePlate = await _db.Vehicles
            .AsNoTracking()
            .AnyAsync(v => v.Id != vehicleId && v.LicensePlate.ToUpper() == plate, ct);

        if (duplicatePlate)
            return Conflict(ApiResponse.Fail("Biển số xe đã tồn tại"));

        if (vehicle.VehicleTypeId != request.VehicleTypeId)
        {
            bool hasScheduledTrips = await _db.Trips.AnyAsync(
                tr => tr.VehicleId == vehicleId && (tr.Status == TripStatuses.Upcoming || tr.Status == TripStatuses.Running), ct);
            if (hasScheduledTrips)
                return Conflict(ApiResponse.Fail("Không thể đổi loại xe khi xe đang được gán cho chuyến sắp khởi hành hoặc đang chạy"));
        }

        vehicle.LicensePlate = plate;
        vehicle.VehicleTypeId = request.VehicleTypeId;
        if (targetStatus != null)
        {
            vehicle.Status = targetStatus;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { vehicleId = vehicle.Id }, "Cập nhật xe thành công"));
    }

    // 40. POST /api/admin/maintenance
    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenance([FromBody] CreateMaintenanceRequest request, CancellationToken ct)
    {
        if (request.VehicleId <= 0 ||
            string.IsNullOrWhiteSpace(request.StartDate) ||
            string.IsNullOrWhiteSpace(request.EndDate))
        {
            return BadRequest(ApiResponse.Fail("Dữ liệu gửi lên thiếu hoặc sai định dạng"));
        }

        if (!InputParseHelper.TryParseDate(request.StartDate, allowGeneralFallback: true, out var startDate) ||
            !InputParseHelper.TryParseDate(request.EndDate, allowGeneralFallback: true, out var endDate) ||
            endDate < startDate)
        {
            return BadRequest(ApiResponse.Fail("Ngày bắt đầu hoặc ngày kết thúc bảo trì không hợp lệ (yyyy-MM-dd)"));
        }

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);
        if (vehicle == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy xe"));

        var schedule = new MaintenanceSchedule
        {
            VehicleId = vehicle.Id,
            StartDate = startDate,
            EndDate = endDate,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.Now
        };

        _db.MaintenanceSchedules.Add(schedule);

        // Nếu lịch bảo trì có hiệu lực ngay hôm nay thì chuyển trạng thái xe sang Bảo trì
        var today = DateTime.Today;
        if (startDate <= today && endDate >= today && vehicle.Status == VehicleStatuses.Active)
        {
            vehicle.Status = VehicleStatuses.Maintenance;
        }

        await _db.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<object>.Ok(new { id = schedule.Id }, "Thêm lịch bảo trì thành công"));
    }
}
