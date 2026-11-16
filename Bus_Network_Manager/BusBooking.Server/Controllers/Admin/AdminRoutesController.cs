using System.Data;
using BusBooking.Server.Data;
using BusBooking.Server.Entities;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Server.Entities.Route;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/routes")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff}")]
public class AdminRoutesController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public AdminRoutesController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 30. GET /api/admin/routes
    [HttpGet]
    public async Task<IActionResult> GetRoutes(
        [FromQuery] string? routeCode,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var now = DateTime.Now;
        var query = _db.Routes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(routeCode))
        {
            string codeFilter = routeCode.Trim();
            query = query.Where(r => r.RouteCode.Contains(codeFilter));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(r => r.Status == statusFilter);
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(r => r.RouteCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRouteDto
            {
                RouteId = r.Id,
                RouteCode = r.RouteCode,
                Origin = r.Origin,
                Destination = r.Destination,
                Status = r.Status,
                UpcomingTrips = r.Trips.Count(tr => tr.Status == TripStatuses.Upcoming && tr.DepartureTime >= now)
            })
            .ToListAsync(ct);

        var paged = new PagedResult<AdminRouteDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<AdminRouteDto>>.Ok(paged, "Thành công"));
    }

    // 31. POST /api/admin/routes
    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var validationError = ValidateRouteInput(request);
        if (validationError != null)
            return validationError;

        string origin = request.Origin.Trim();
        string destination = request.Destination.Trim();

        bool duplicate = await _db.Routes
            .AsNoTracking()
            .AnyAsync(r => r.Origin.ToLower() == origin.ToLower()
                           && r.Destination.ToLower() == destination.ToLower(), ct);

        if (duplicate)
            return Conflict(ApiResponse.Fail("Tuyến xe đã tồn tại"));

        string routeCode = await GenerateNextRouteCodeAsync(ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        var route = new Route
        {
            RouteCode = routeCode,
            Origin = origin,
            Destination = destination,
            DistanceKm = request.DistanceKm,
            DurationMinutes = request.DurationMinutes,
            Status = RouteStatuses.Active,
            CreatedAt = DateTime.Now
        };

        _db.Routes.Add(route);
        await _db.SaveChangesAsync(ct);

        var points = new List<RoutePoint>();
        AddPoints(points, route.Id, RoutePointTypes.Pickup, request.PickupPoints);
        AddPoints(points, route.Id, RoutePointTypes.Dropoff, request.DropoffPoints);

        _db.RoutePoints.AddRange(points);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var data = new CreateRouteResponseData
        {
            RouteId = route.Id,
            RouteCode = route.RouteCode
        };

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateRouteResponseData>.Ok(data, "Thêm tuyến thành công"));
    }

    // 32. GET /api/admin/routes/{routeId}
    [HttpGet("{routeId:int}")]
    public async Task<IActionResult> GetRouteDetail(int routeId, CancellationToken ct)
    {
        var route = await _db.Routes
            .AsNoTracking()
            .Include(r => r.RoutePoints)
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);

        if (route == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy tuyến xe"));

        var orderedPoints = route.RoutePoints
            .OrderBy(p => p.OrderIndex)
            .ThenBy(p => p.Id)
            .Select(p => new RoutePointDto
            {
                Id = p.Id,
                Type = p.Type,
                Name = p.Name,
                Address = p.Address
            })
            .ToList();

        var data = new RouteDetailDto
        {
            RouteId = route.Id,
            RouteCode = route.RouteCode,
            Origin = route.Origin,
            Destination = route.Destination,
            DistanceKm = route.DistanceKm,
            DurationMinutes = route.DurationMinutes,
            Status = route.Status,
            PickupPoints = orderedPoints.Where(p => p.Type == RoutePointTypes.Pickup).ToList(),
            DropoffPoints = orderedPoints.Where(p => p.Type == RoutePointTypes.Dropoff).ToList()
        };

        return Ok(ApiResponse<RouteDetailDto>.Ok(data, "Thành công"));
    }

    // 33. PUT /api/admin/routes/{routeId}
    [HttpPut("{routeId:int}")]
    public async Task<IActionResult> UpdateRoute(int routeId, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var validationError = ValidateRouteInput(request);
        if (validationError != null)
            return validationError;

        string status = request.Status?.Trim() ?? string.Empty;
        if (status != RouteStatuses.Active && status != RouteStatuses.Inactive)
            return BadRequest(ApiResponse.Fail("Trạng thái tuyến xe không hợp lệ"));

        var route = await _db.Routes
            .Include(r => r.RoutePoints)
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);

        if (route == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy tuyến xe"));

        string origin = request.Origin.Trim();
        string destination = request.Destination.Trim();

        bool duplicate = await _db.Routes
            .AsNoTracking()
            .AnyAsync(r => r.Id != routeId
                           && r.Origin.ToLower() == origin.ToLower()
                           && r.Destination.ToLower() == destination.ToLower(), ct);

        if (duplicate)
            return Conflict(ApiResponse.Fail("Tuyến bị trùng"));

        var toDelete = new List<RoutePoint>();
        var toAdd = new List<RoutePoint>();

        SyncRoutePoints(route, RoutePointTypes.Pickup, request.PickupPoints, toAdd, toDelete);
        SyncRoutePoints(route, RoutePointTypes.Dropoff, request.DropoffPoints, toAdd, toDelete);

        if (toDelete.Count > 0)
        {
            var deleteIds = toDelete.Select(p => p.Id).ToList();
            bool hasTicketReferences = await _db.Tickets.AnyAsync(
                t => deleteIds.Contains(t.PickupPointId) || deleteIds.Contains(t.DropoffPointId), ct);

            if (hasTicketReferences)
                return Conflict(ApiResponse.Fail("Không thể xóa điểm đón/trả đã được sử dụng trong vé đặt"));

            _db.RoutePoints.RemoveRange(toDelete);
        }

        if (toAdd.Count > 0)
        {
            _db.RoutePoints.AddRange(toAdd);
        }

        route.Origin = origin;
        route.Destination = destination;
        route.DistanceKm = request.DistanceKm;
        route.DurationMinutes = request.DurationMinutes;
        route.Status = status;

        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { routeId = route.Id }, "Cập nhật tuyến thành công"));
    }

    private IActionResult? ValidateRouteInput(CreateRouteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination))
            return BadRequest(ApiResponse.Fail("Điểm đi và điểm đến không được để trống"));

        if (string.Equals(request.Origin.Trim(), request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("Điểm đi và điểm đến không thể trùng nhau"));

        if (request.DistanceKm.HasValue && request.DistanceKm.Value <= 0)
            return BadRequest(ApiResponse.Fail("Quãng đường phải lớn hơn 0"));

        if (request.DurationMinutes.HasValue && request.DurationMinutes.Value <= 0)
            return BadRequest(ApiResponse.Fail("Thời gian di chuyển dự kiến phải lớn hơn 0"));

        if (request.PickupPoints == null || request.PickupPoints.Count == 0 ||
            request.DropoffPoints == null || request.DropoffPoints.Count == 0)
        {
            return BadRequest(ApiResponse.Fail("Tuyến xe phải có ít nhất 1 điểm đón và 1 điểm trả"));
        }

        if (request.PickupPoints.Any(p => string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Address)) ||
            request.DropoffPoints.Any(p => string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Address)))
        {
            return BadRequest(ApiResponse.Fail("Tên và địa chỉ điểm đón/trả không được để trống"));
        }

        return null;
    }

    private static void AddPoints(List<RoutePoint> target, int routeId, string type, List<RoutePointInputDto> inputs)
    {
        for (int i = 0; i < inputs.Count; i++)
        {
            target.Add(new RoutePoint
            {
                RouteId = routeId,
                Type = type,
                Name = inputs[i].Name.Trim(),
                Address = inputs[i].Address.Trim(),
                OrderIndex = i + 1
            });
        }
    }

    private static void SyncRoutePoints(
        Route route,
        string type,
        List<RoutePointInputDto> inputs,
        List<RoutePoint> toAdd,
        List<RoutePoint> toDelete)
    {
        var existing = route.RoutePoints.Where(p => p.Type == type).ToList();
        var unmatchedExisting = new List<RoutePoint>(existing);

        for (int i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            int orderIndex = i + 1;
            string name = input.Name.Trim();
            string address = input.Address.Trim();

            RoutePoint? match = null;
            if (input.Id.HasValue && input.Id.Value > 0)
            {
                match = unmatchedExisting.FirstOrDefault(p => p.Id == input.Id.Value);
            }

            // Fallback khớp theo tên nếu client gửi PUT không kèm Id của điểm dừng
            match ??= unmatchedExisting.FirstOrDefault(
                p => string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                match.Name = name;
                match.Address = address;
                match.OrderIndex = orderIndex;
                unmatchedExisting.Remove(match);
            }
            else
            {
                toAdd.Add(new RoutePoint
                {
                    RouteId = route.Id,
                    Type = type,
                    Name = name,
                    Address = address,
                    OrderIndex = orderIndex
                });
            }
        }

        toDelete.AddRange(unmatchedExisting);
    }

    private async Task<string> GenerateNextRouteCodeAsync(CancellationToken ct)
    {
        int nextSeq = (await _db.Routes.MaxAsync(r => (int?)r.Id, ct) ?? 0) + 1;
        string code = $"T{nextSeq:D3}";
        while (await _db.Routes.AnyAsync(r => r.RouteCode == code, ct))
        {
            nextSeq++;
            code = $"T{nextSeq:D3}";
        }
        return code;
    }
}
