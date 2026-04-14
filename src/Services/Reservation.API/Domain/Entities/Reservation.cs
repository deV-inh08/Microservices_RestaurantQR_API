using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Reservation.API.Domain.Entities
{

    public enum ReservationStatus
    {
        // Đã cọc tiền, đang chờ khách đến
        Booked = 1,

        // Khách đã đến và đang ngồi tại bàn
        CheckedIn = 2,

        // Đơn bị hủy (không đến hoặc khách chủ động hủy)
        Cancelled = 3
    }

    public enum DepositStatus
    {
        None = 0,         // Không yêu cầu cọc
        Pending = 1,      // Đang chờ thanh toán cọc
        Paid = 2,         // Đã nộp cọc thành công
        Refunded = 3,     // Đã hoàn tiền (nếu khách hủy đúng quy định)
        Forfeited = 4     // Bị mất cọc (nếu khách bùng lịch)
    }
    public class Reservation
    {
        [BsonId]
        public int Id { get; set; }

        public string GuestName { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestEmail { get; set; }
        public int? TableId { get; set; }
        public int NumberOfPeople { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Booked;
        public DateTime ReservationDate { get; set; }

        // Deposit
        public decimal DepositAmount { get; set; }
        public DepositStatus DepositStatus { get; set; } = DepositStatus.Pending;


        public string? Note { get; set; }
        public int? AccountId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
