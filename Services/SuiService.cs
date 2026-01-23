using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SuiService
{
    private readonly ILogger<SuiService> _logger;

    private const string PackageId =
        "0xab7c191f829fd3364da9176d0d8190ca030aa1a8f1cf0df54880312266669744";
    private const string MoveModule = "payment";
    private const string MoveFunction = "record_payment";

    public SuiService(ILogger<SuiService> logger)
    {
        _logger = logger;
    }

    public async Task<string> RecordPaymentAsync(
        string studentWallet,
        string tutorWallet,
        long amount,
        string orderId,
        long timestamp
    )
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sui",
            Arguments =
                $"client call " +
                $"--package {PackageId} " +
                $"--module {MoveModule} " +
                $"--function {MoveFunction} " +
                $"--args {studentWallet} {tutorWallet} {amount} {orderId} {timestamp} " +
                $"--gas-budget 100000000 " +
                $"--json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        _logger.LogInformation("SUI OUTPUT:\n{Output}", output);
        if (!string.IsNullOrWhiteSpace(error))
            _logger.LogError("SUI ERROR:\n{Error}", error);

        var txHash = ExtractTxHash(output);

        return txHash ?? "PENDING";
    }

    private string? ExtractTxHash(string output)
    {
        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (root.TryGetProperty("digest", out var d))
                return d.GetString();

            if (root.TryGetProperty("transactionDigest", out var t))
                return t.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }
}
