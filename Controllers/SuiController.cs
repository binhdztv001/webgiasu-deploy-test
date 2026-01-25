using Microsoft.AspNetCore.Mvc;
using Webgiasu.Models;

[ApiController]
[Route("internal/sui")]
[ApiExplorerSettings(IgnoreApi = true)]
public class SuiController : ControllerBase
{
    private readonly SuiService _suiService;

    public SuiController(SuiService suiService)
    {
        _suiService = suiService;
    }

    [HttpPost("record-payment")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest req)
    {
        var txHash = await _suiService.RecordPaymentAsync(
            req.OrderId,
            req.Amount,
            req.Timestamp
        );

        return Ok(new
        {
            txHash
        });
    }
}
