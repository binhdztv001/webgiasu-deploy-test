using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models
{
    public class RecordPaymentRequest
    {
        [Required]
        public string OrderId { get; set; } = default!;

        [Range(1, long.MaxValue)]
        public long Amount { get; set; }

        public long Timestamp { get; set; } =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
