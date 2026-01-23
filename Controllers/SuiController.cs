using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Webgiasu.Models;

[ApiController]
[Route("sui")]
public class SuiController : ControllerBase
{
    private readonly SuiService _suiService;

    public SuiController(SuiService suiService)
    {
        _suiService = suiService;
    }

    /// <summary>
    /// Ghi payment lên Sui blockchain (record_payment)
    /// </summary>
    [HttpPost("record-payment")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var txHash = await _suiService.RecordPaymentAsync(
            req.StudentWallet,
            req.TutorWallet,
            req.Amount,
            req.OrderId,
            req.Timestamp
        );

        return Ok(new
        {
            success = true,
            txHash,
            explorer = string.IsNullOrEmpty(txHash) || txHash == "PENDING"
                ? null
                : $"https://suiexplorer.com/txblock/{txHash}"
        });
    }
}
