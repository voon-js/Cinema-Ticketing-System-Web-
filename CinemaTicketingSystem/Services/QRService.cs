using QRCoder;
using System.Drawing.Imaging;

namespace CinemaTicketingSystem.Services
{
    public interface IQRService
    {
        string GenerateBookingQRCode(int bookingId, string movieTitle, DateTime showtime);
    }

    public class QRService : IQRService
    {
        public string GenerateBookingQRCode(int bookingId, string movieTitle, DateTime showtime)
        {
            var qrText = $"CINEMA-TICKET\nBooking: #{bookingId}\nMovie: {movieTitle}\nTime: {showtime:yyyy-MM-dd HH:mm}\nValid Entry";

            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
                {
                    // Use Base64QRCode for simpler base64 output
                    var qrCode = new Base64QRCode(qrCodeData);
                    return qrCode.GetGraphic(20); // Returns base64 string directly
                }
            }
        }
    }
}