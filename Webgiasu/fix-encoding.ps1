# Fix Encoding Script for .cshtml files
# This script converts all .cshtml files to UTF-8 with BOM

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX ENCODING FOR CSHTML FILES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = Get-Location
Write-Host "Working directory: $rootPath" -ForegroundColor Yellow
Write-Host ""

# Find all .cshtml files
$files = Get-ChildItem -Path . -Filter *.cshtml -Recurse | Where-Object { 
    $_.FullName -notmatch '\\bin\\' -and 
    $_.FullName -notmatch '\\obj\\' -and
    $_.FullName -notmatch '\\Temp\\'
}

$totalFiles = $files.Count
Write-Host "Found $totalFiles .cshtml files to process" -ForegroundColor Yellow
Write-Host ""

$fixed = 0
$skipped = 0
$errors = 0

foreach ($file in $files) {
    try {
        # Read file content
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        
        # Check if file already has UTF-8 BOM
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $hasBOM = ($bytes.Length -ge 3) -and 
                  ($bytes[0] -eq 0xEF) -and 
                  ($bytes[1] -eq 0xBB) -and 
                  ($bytes[2] -eq 0xBF)
        
        if ($hasBOM) {
            Write-Host "? SKIP: $($file.Name) - Already UTF-8 with BOM" -ForegroundColor DarkGray
            $skipped++
        } else {
            # Write with UTF-8 BOM
            $utf8WithBom = New-Object System.Text.UTF8Encoding($true)
            [System.IO.File]::WriteAllText($file.FullName, $content, $utf8WithBom)
            Write-Host "? FIXED: $($file.Name)" -ForegroundColor Green
            $fixed++
        }
    }
    catch {
        Write-Host "? ERROR: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total files: $totalFiles" -ForegroundColor White
Write-Host "Fixed: $fixed" -ForegroundColor Green
Write-Host "Skipped: $skipped" -ForegroundColor Yellow
Write-Host "Errors: $errors" -ForegroundColor Red
Write-Host ""

if ($fixed -gt 0) {
    Write-Host "? Successfully fixed $fixed files!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Clean Solution in Visual Studio" -ForegroundColor White
    Write-Host "2. Rebuild Solution" -ForegroundColor White
    Write-Host "3. Restart IIS Express / Debug" -ForegroundColor White
    Write-Host "4. Clear browser cache" -ForegroundColor White
    Write-Host "5. Test the application" -ForegroundColor White
} else {
    Write-Host "? All files are already UTF-8 with BOM!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
