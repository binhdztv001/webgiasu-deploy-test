using System.ComponentModel.DataAnnotations;

namespace Webgiasu.Models
{
    public class RecordPaymentRequest
    {
        [Required]
        public string StudentWallet { get; set; } = default!;

        [Required]
        public string TutorWallet { get; set; } = default!;

        [Range(1, long.MaxValue)]
        public long Amount { get; set; }

        [Required]
        public string OrderId { get; set; } = default!;

        public long Timestamp { get; set; } =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
