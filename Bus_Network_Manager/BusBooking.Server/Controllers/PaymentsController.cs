using BusBooking.Server.Data;
using BusBooking.Server.Extensions;
using BusBooking.Server.Services;
using BusBooking.Shared.DTOs;
using BusBooking.Shared.Enums;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Server.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly BusBookingDbContext _db;
    private readonly IMomoService _momoService;
    private readonly IPaymentConfirmationService _confirmationService;

    public PaymentsController(
        BusBookingDbContext db,
        IMomoService momoService,
        IPaymentConfirmationService confirmationService)
    {
        _db = db;
        _momoService = momoService;
        _confirmationService = confirmationService;
    }

    // 15. POST /api/payments/momo/ipn
    [HttpPost("momo/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MomoIpn([FromBody] MomoIpnRequest request, CancellationToken ct)
    {
        var (outcome, message) = await _confirmationService.ProcessMomoIpnAsync(request, ct);

        return outcome switch
        {
            IpnProcessOutcome.Success => NoContent(),
            IpnProcessOutcome.TicketNotFound => NotFound(ApiResponse.Fail(message)),
            _ => BadRequest(ApiResponse.Fail(message))
        };
    }

    // Endpoint hỗ trợ giả lập thanh toán MoMo thành công để test local khi chưa có public IP cho Webhook
    [HttpPost("momo/simulate/{ticketId:int}")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> SimulateMomoPaymentSuccess(int ticketId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out int userId))
            return Unauthorized(ApiResponse.Fail("Token thiếu hoặc không hợp lệ"));

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null)
            return NotFound(ApiResponse.Fail("Không tìm thấy vé"));

        if (ticket.CustomerId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Vé không thuộc về người dùng hiện tại"));

        if (ticket.Status == TicketStatuses.Cancelled)
            return Conflict(ApiResponse.Fail("Vé đã bị hủy, không thể thanh toán"));

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ipn = new MomoIpnRequest
        {
            PartnerCode = _momoService.PartnerCode,
            OrderId = ticket.TicketCode,
            RequestId = $"SIM{nowMs}",
            Amount = (long)ticket.TotalAmount,
            OrderInfo = $"Thanh toan ve {ticket.TicketCode}",
            OrderType = "momo_wallet",
            TransId = nowMs,
            ResultCode = 0,
            Message = "Successful.",
            PayType = "qr",
            ResponseTime = nowMs,
            ExtraData = string.Empty
        };
        ipn.Signature = _momoService.ComputeIpnSignature(ipn);

        var (outcome, message) = await _confirmationService.ProcessMomoIpnAsync(ipn, ct);
        if (outcome != IpnProcessOutcome.Success)
            return BadRequest(ApiResponse.Fail(message));

        var updated = await _db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId, ct);
        return Ok(ApiResponse<PaymentStatusResponseData>.Ok(new PaymentStatusResponseData
        {
            TicketId = updated.Id,
            TicketCode = updated.TicketCode,
            Status = updated.Status,
            PaidAt = updated.PaidAt
        }, "Giả lập thanh toán MoMo thành công"));
    }
}
