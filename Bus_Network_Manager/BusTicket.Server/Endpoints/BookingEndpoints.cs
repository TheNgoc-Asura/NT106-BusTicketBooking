using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using BusTicket.Core.DTOs;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using BusTicket.Server.Hubs;
using BusTicket.Server.Services;

namespace BusTicket.Server.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        // 14. Tạo đơn đặt vé và khởi tạo thanh toán
        app.MapPost("/api/bookings", (CreateBookingRequest req, HttpRequest httpReq, DataStore store, TokenService tokenService, MomoService momoService) =>
        {
            if (!tokenService.ValidateToken(httpReq.Headers.Authorization, out int userId, out string role, out string userEmail))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (req.TripId <= 0 || req.SeatId <= 0 || string.IsNullOrWhiteSpace(req.PassengerName) || string.IsNullOrWhiteSpace(req.PassengerPhone))
                return Results.Json(ApiResponse<object>.Fail("Thiếu thông tin đặt vé hoặc hành khách"), statusCode: 400);

            var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == req.TripId);
            if (trip == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy chuyến xe"), statusCode: 404);

            var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == req.SeatId && s.TripId == req.TripId);
            if (seat == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy ghế"), statusCode: 404);

            // Check if seat is available or lock expired
            if (seat.Status == SeatStatuses.Booked || (seat.Status == SeatStatuses.Pending && seat.LockedUntil > DateTime.Now))
            {
                return Results.Json(ApiResponse<object>.Fail("Ghế đã có người đặt hoặc đang được giữ"), statusCode: 409);
            }

            // Lock seat for 10 minutes
            seat.Status = SeatStatuses.Pending;
            seat.LockedUntil = DateTime.Now.AddMinutes(10);

            string ticketCode = store.GenerateTicketCode();
            decimal totalAmount = trip.Price + trip.Surcharge;

            var ticket = new Ticket
            {
                TicketId = store.GetNextTicketId(),
                TicketCode = ticketCode,
                TripId = req.TripId,
                SeatId = req.SeatId,
                CustomerId = userId,
                PassengerName = req.PassengerName.Trim(),
                PassengerPhone = req.PassengerPhone.Trim(),
                PassengerEmail = string.IsNullOrWhiteSpace(req.PassengerEmail) ? userEmail : req.PassengerEmail.Trim(),
                PickupPointId = req.PickupPointId,
                DropoffPointId = req.DropoffPointId,
                TotalAmount = totalAmount,
                Channel = BookingChannels.Online,
                Status = TicketStatuses.PendingPayment,
                BookedAt = DateTime.Now
            };

            store.Db.Tickets.Add(ticket);

            // Init MoMo transaction
            string ipnUrl = $"{httpReq.Scheme}://{httpReq.Host}/api/payments/momo/ipn";
            string redirectUrl = $"{httpReq.Scheme}://{httpReq.Host}/api/bookings/{ticket.TicketId}/payment-status";
            var paymentInfo = momoService.CreatePaymentRequest(ticketCode, (long)totalAmount, $"Thanh toan ve {ticketCode}", ipnUrl, redirectUrl);

            var paymentRecord = new Payment
            {
                Id = store.Db.Payments.Count + 1,
                TicketId = ticket.TicketId,
                Provider = "MoMo",
                OrderId = ticketCode,
                RequestId = "RQ" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Amount = (long)totalAmount,
                PayType = "qr",
                ResultCode = 0,
                CreatedAt = DateTime.Now
            };
            store.Db.Payments.Add(paymentRecord);

            store.SaveChanges();

            var res = new BookingResponseData
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status,
                TotalAmount = ticket.TotalAmount,
                Payment = paymentInfo
            };

            return Results.Json(ApiResponse<BookingResponseData>.Ok(res, "Tạo vé thành công, vui lòng thanh toán"), statusCode: 201);
        });

        // 15. Nhận kết quả thanh toán từ MoMo (IPN Webhook)
        app.MapPost("/api/payments/momo/ipn", async (MomoIpnRequest ipn, DataStore store, MomoService momoService, EmailService emailService, IHubContext<SupportHub> hubContext) =>
        {
            if (!momoService.VerifyIpnSignature(ipn))
                return Results.BadRequest(new { message = "Chữ ký không hợp lệ" });

            var ticket = store.Db.Tickets.FirstOrDefault(t => t.TicketCode == ipn.OrderId);
            if (ticket == null)
                return Results.NotFound(new { message = "Không tìm thấy vé ứng với orderId" });

            var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == ticket.SeatId);

            if (ipn.ResultCode == 0) // Thành công
            {
                ticket.Status = TicketStatuses.Paid;
                ticket.PaidAt = DateTime.Now;

                if (seat != null)
                {
                    seat.Status = SeatStatuses.Booked;
                    seat.LockedUntil = null;
                }

                // Add notification
                if (ticket.CustomerId.HasValue)
                {
                    var notif = new Notification
                    {
                        Id = store.GetNextNotificationId(),
                        UserId = ticket.CustomerId.Value,
                        Type = "PaymentConfirmed",
                        Title = "Thanh toán thành công",
                        Content = $"Vé {ticket.TicketCode} đã được thanh toán thành công qua MoMo.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    store.Db.Notifications.Add(notif);

                    // Push via SignalR
                    await hubContext.Clients.Group($"user_{ticket.CustomerId.Value}").SendAsync("ReceiveNotification", new
                    {
                        id = notif.Id,
                        type = notif.Type,
                        title = notif.Title,
                        content = notif.Content,
                        createdAt = notif.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                    });
                }

                // Push PaymentConfirmed event
                await hubContext.Clients.All.SendAsync("PaymentConfirmed", new
                {
                    ticketId = ticket.TicketId,
                    ticketCode = ticket.TicketCode,
                    status = ticket.Status
                });

                // Send email invoice if email available
                if (!string.IsNullOrEmpty(ticket.PassengerEmail))
                {
                    var trip = store.Db.Trips.FirstOrDefault(t => t.TripId == ticket.TripId);
                    var route = trip != null ? store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId) : null;
                    string routeName = route != null ? $"{route.Origin} - {route.Destination}" : "Tuyến xe khách";
                    _ = emailService.SendTicketInvoiceAsync(ticket.PassengerEmail, ticket.TicketCode, routeName, seat?.SeatCode ?? "A01", ticket.TotalAmount);
                }
            }
            else // Giao dịch thất bại / hủy
            {
                ticket.Status = TicketStatuses.Cancelled;
                ticket.CancelledAt = DateTime.Now;
                if (seat != null)
                {
                    seat.Status = SeatStatuses.Available;
                    seat.LockedUntil = null;
                }
            }

            store.SaveChanges();
            return Results.NoContent();
        });

        // 16. Kiểm tra trạng thái thanh toán của vé
        app.MapGet("/api/bookings/{ticketId:int}/payment-status", (int ticketId, HttpRequest httpReq, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(httpReq.Headers.Authorization, out int userId, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var ticket = store.Db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy vé"), statusCode: 404);

            if (role == Roles.Customer && ticket.CustomerId != userId)
                return Results.Json(ApiResponse<object>.Fail("Vé không thuộc về người dùng hiện tại"), statusCode: 403);

            var res = new PaymentStatusResponseData
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status,
                PaidAt = ticket.PaidAt
            };

            return Results.Ok(ApiResponse<PaymentStatusResponseData>.Ok(res, "Thành công"));
        });

        // 17. Xem danh sách vé của tôi
        app.MapGet("/api/tickets/my", (string? status, int page, int pageSize, HttpRequest httpReq, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(httpReq.Headers.Authorization, out int userId, out _, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = store.Db.Tickets.Where(t => t.CustomerId == userId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            int totalItems = query.Count();
            var tickets = query.OrderByDescending(t => t.BookedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var items = new List<MyTicketDto>();
            foreach (var t in tickets)
            {
                var trip = store.Db.Trips.FirstOrDefault(tr => tr.TripId == t.TripId);
                var route = trip != null ? store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId) : null;
                var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == t.SeatId);

                items.Add(new MyTicketDto
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "Chuyến xe",
                    DepartureTime = trip?.DepartureTime ?? DateTime.Now,
                    SeatCode = seat?.SeatCode ?? "A01",
                    TotalAmount = t.TotalAmount,
                    Status = t.Status
                });
            }

            var paged = new PagedResult<MyTicketDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Results.Ok(ApiResponse<PagedResult<MyTicketDto>>.Ok(paged, "Thành công"));
        });

        // 18. Xem chi tiết vé
        app.MapGet("/api/tickets/{ticketId:int}", (int ticketId, HttpRequest httpReq, DataStore store, TokenService tokenService) =>
        {
            if (!tokenService.ValidateToken(httpReq.Headers.Authorization, out int userId, out string role, out _))
                return Results.Json(ApiResponse<object>.Fail("Token thiếu hoặc không hợp lệ"), statusCode: 401);

            var ticket = store.Db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
                return Results.Json(ApiResponse<object>.Fail("Không tìm thấy vé"), statusCode: 404);

            if (role == Roles.Customer && ticket.CustomerId != userId)
                return Results.Json(ApiResponse<object>.Fail("Vé không thuộc về người dùng hiện tại"), statusCode: 403);

            var trip = store.Db.Trips.FirstOrDefault(tr => tr.TripId == ticket.TripId);
            var route = trip != null ? store.Db.Routes.FirstOrDefault(r => r.RouteId == trip.RouteId) : null;
            var vehicle = trip != null ? store.Db.Vehicles.FirstOrDefault(v => v.VehicleId == trip.VehicleId) : null;
            var seat = store.Db.Seats.FirstOrDefault(s => s.SeatId == ticket.SeatId);
            var pickup = store.Db.StopPoints.FirstOrDefault(p => p.Id == ticket.PickupPointId);
            var dropoff = store.Db.StopPoints.FirstOrDefault(p => p.Id == ticket.DropoffPointId);

            var res = new TicketDetailDto
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status,
                Trip = new TripSummaryDto
                {
                    TripId = trip?.TripId ?? 0,
                    TripCode = trip?.TripCode ?? "",
                    RouteName = route != null ? $"{route.Origin} - {route.Destination}" : "",
                    DepartureTime = trip?.DepartureTime ?? DateTime.Now,
                    VehiclePlate = vehicle?.LicensePlate ?? ""
                },
                SeatCode = seat?.SeatCode ?? "",
                Passenger = new PassengerDto
                {
                    Name = ticket.PassengerName,
                    Phone = ticket.PassengerPhone
                },
                PickupPoint = pickup?.Name ?? "Điểm đón",
                DropoffPoint = dropoff?.Name ?? "Điểm trả",
                TotalAmount = ticket.TotalAmount,
                PaymentMethod = "MoMo",
                PaidAt = ticket.PaidAt
            };

            return Results.Ok(ApiResponse<TicketDetailDto>.Ok(res, "Thành công"));
        });
    }
}
