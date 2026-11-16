using BusBooking.Server.Data;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers.Admin;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Staff},{UserRoles.Support}")]
public class AdminCustomersController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public AdminCustomersController(BusBookingDbContext db)
    {
        _db = db;
    }

    // 48. GET /api/admin/customers
    [HttpGet]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] string? fullName,
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRoles.Customer);

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            string nameFilter = fullName.Trim();
            query = query.Where(u => u.FullName.Contains(nameFilter));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            string emailFilter = email.Trim();
            query = query.Where(u => u.Email.Contains(emailFilter));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            string phoneFilter = phone.Trim();
            query = query.Where(u => u.Phone != null && u.Phone.Contains(phoneFilter));
        }

        int totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminCustomerDto
            {
                CustomerId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                TicketCount = u.Tickets.Count
            })
            .ToListAsync(ct);

        var paged = new PagedResult<AdminCustomerDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return Ok(ApiResponse<PagedResult<AdminCustomerDto>>.Ok(paged, "Thành công"));
    }

    // 49. GET /api/admin/customers/{customerId}
    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> GetCustomerDetail(int customerId, CancellationToken ct)
    {
        var customer = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == customerId && u.Role == UserRoles.Customer)
            .Select(u => new AdminCustomerDetailDto
            {
                CustomerId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Tickets = u.Tickets
                    .OrderByDescending(tk => tk.CreatedAt)
                    .ThenByDescending(tk => tk.Id)
                    .Select(tk => new CustomerTicketSummaryDto
                    {
                        TicketId = tk.Id,
                        TicketCode = tk.TicketCode,
                        RouteName = tk.Trip.Route.Origin + " - " + tk.Trip.Route.Destination,
                        DepartureTime = tk.Trip.DepartureTime,
                        Status = tk.Status
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (customer == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy khách hàng"));

        return Ok(ApiResponse<AdminCustomerDetailDto>.Ok(customer, "Thành công"));
    }
}
