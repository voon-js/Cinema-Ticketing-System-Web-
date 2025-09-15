using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using CinemaTicketingSystem.Models;

namespace CinemaTicketingSystem.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(string toEmail, string userName, Booking booking);
        Task SendPaymentConfirmationAsync(string toEmail, string userName, Booking booking, Payment payment);
        Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPaymentConfirmationAsync(string toEmail, string userName, Booking booking, Payment payment)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Cinema Ticketing System", _configuration["Email:From"]));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = $"Payment Confirmation - Booking #{booking.BookingId}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <h2>Payment Confirmed! 🎉</h2>
                    <p>Dear {userName},</p>
                    <p>Your payment has been processed successfully. Your booking is now confirmed!</p>
                    
                    <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                        <h3>Payment Details</h3>
                        <p><strong>Transaction ID:</strong> {payment.TransactionId}</p>
                        <p><strong>Amount Paid:</strong> {payment.Amount:C}</p>
                        <p><strong>Payment Method:</strong> {payment.PaymentMethod}</p>
                        <p><strong>Payment Date:</strong> {payment.PaymentDate:yyyy-MM-dd HH:mm}</p>
                        <p><strong>Status:</strong> <span style='color: green; font-weight: bold;'>{payment.Status}</span></p>
                    </div>

                    <div style='background: #e8f4f8; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                        <h3>Booking Details</h3>
                        <p><strong>Movie:</strong> {booking.Showtime?.Movie?.Title}</p>
                        <p><strong>Cinema:</strong> {booking.Showtime?.Cinema?.Name}</p>
                        <p><strong>Showtime:</strong> {booking.Showtime?.StartTime:yyyy-MM-dd HH:mm}</p>
                        <p><strong>Duration:</strong> {booking.Showtime?.Movie?.Duration} minutes</p>
                        <p><strong>Tickets:</strong> {booking.NumberOfTickets}</p>
                        <p><strong>Seats:</strong> {GetSeatLabels(booking.SeatNumbers)}</p>
                        <p><strong>Booking Reference:</strong> #{booking.BookingId}</p>
                    </div>

                    <div style='margin: 20px 0; padding: 15px; background: #fff3cd; border-radius: 5px;'>
                        <h4>🎬 Important Information</h4>
                        <ul>
                            <li>Please arrive at least 30 minutes before the showtime</li>
                            <li>Bring your ID and this confirmation email</li>
                            <li>Seats will be held until 15 minutes after showtime</li>
                            <li>No refunds for missed shows</li>
                        </ul>
                    </div>

                    <p>Thank you for choosing our cinema! We hope you enjoy the movie.</p>
                    <p>Best regards,<br>The Cinema Team</p>
                "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["Email:SmtpServer"],
                                     int.Parse(_configuration["Email:Port"]),
                                     bool.Parse(_configuration["Email:UseSsl"]));

            await client.AuthenticateAsync(_configuration["Email:Username"],
                                          _configuration["Email:Password"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendBookingConfirmationAsync(string toEmail, string userName, Booking booking)
        {
            // Keep the original method for backward compatibility
            await SendPaymentConfirmationAsync(toEmail, userName, booking, new Payment
            {
                TransactionId = "N/A",
                Amount = booking.TotalAmount,
                PaymentMethod = "Not Specified",
                Status = "Confirmed",
                PaymentDate = DateTime.Now
            });
        }

        public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Cinema Ticketing System", _configuration["Email:From"]));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Password Reset Request - Cinema Paradise";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
            <h2>Password Reset Request</h2>
            <p>Hi {userName},</p>
            <p>You (or someone else) requested to reset your password. 
            Please click the button below to reset it:</p>
            <p><a href='{resetLink}' 
                style='display:inline-block; padding:10px 20px; 
                       background:#007bff; color:#fff; 
                       text-decoration:none; border-radius:5px;'>
                Reset My Password
            </a></p>
            <p>If you didn’t request this, you can safely ignore this email.</p>
            <p><strong>This link will expire in 1 hour.</strong></p>
            <p>— The Cinema Paradise Team 🎬</p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["Email:SmtpServer"],
                                     int.Parse(_configuration["Email:Port"]),
                                     bool.Parse(_configuration["Email:UseSsl"]));
            await client.AuthenticateAsync(_configuration["Email:Username"], _configuration["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }


        private string GetSeatLabels(string seatNumbers)
        {
            if (string.IsNullOrEmpty(seatNumbers)) return "General Admission";

            var seats = seatNumbers.Split(',')
                .Select(s => int.TryParse(s, out int seatNum) ?
                    $"{(char)('A' + (seatNum / 10))}{(seatNum % 10) + 1}" : "")
                .Where(s => !string.IsNullOrEmpty(s));

            return string.Join(", ", seats);
        }
    }
}