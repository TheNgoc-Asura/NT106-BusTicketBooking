using BusBooking.Server.Data;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly BusBookingDbContext _db;

    public HealthController(BusBookingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetHealth()
    {
        bool dbConnected = false;
        int userCount = 0;
        int tripCount = 0;

        try
        {
            dbConnected = await _db.Database.CanConnectAsync();
            if (dbConnected)
            {
                userCount = await _db.Users.CountAsync();
                tripCount = await _db.Trips.CountAsync();
            }
        }
        catch
        {
            dbConnected = false;
        }

        var info = new
        {
            status = dbConnected ? "Healthy" : "Degraded",
            databaseConnected = dbConnected,
            usersSeeded = userCount,
            tripsSeeded = tripCount,
            serverTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        return Ok(ApiResponse<object>.Ok(info, "BusBooking Server đang hoạt động"));
    }
}
