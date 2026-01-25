using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SuiService
{
    private readonly ILogger<SuiService> _logger;

    private const string PackageId =
        "0xc55e654338597ccac35e79cf780a45e65e06cff420a51e7ef782d7807019a3dc";
    private const string MoveModule = "payment";
    private const string MoveFunction = "record_payment";

    public SuiService(ILogger<SuiService> logger)
    {
        _logger = logger;
    }

    public async Task<string> RecordPaymentAsync(
        string orderId,
        long amount,
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
                $"--args \"{orderId}\" {amount} {timestamp} " +
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

        return ExtractTxHash(output) ?? "PENDING";
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
