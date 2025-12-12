# Quick Clean & Rebuild Script
# Run this after stopping debug in Visual Studio

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  QUICK CLEAN & REBUILD" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean bin and obj folders
Write-Host "Step 1: Cleaning bin and obj folders..." -ForegroundColor Yellow
$binPath = "Webgiasu\bin"
$objPath = "Webgiasu\obj"

if (Test-Path $binPath) {
    Remove-Item -Recurse -Force $binPath
    Write-Host "? Removed bin folder" -ForegroundColor Green
}

if (Test-Path $objPath) {
    Remove-Item -Recurse -Force $objPath
    Write-Host "? Removed obj folder" -ForegroundColor Green
}

# Step 2: Clean temp cshtml files
Write-Host "`nStep 2: Cleaning temp .cshtml files..." -ForegroundColor Yellow
$tempPath = "C:\Users\$env:USERNAME\AppData\Local\Temp"
$tempFiles = Get-ChildItem -Path $tempPath -Filter "*.cshtml" -ErrorAction SilentlyContinue

if ($tempFiles) {
    $tempFiles | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "? Removed $($tempFiles.Count) temp files" -ForegroundColor Green
} else {
    Write-Host "? No temp files found" -ForegroundColor Green
}

# Step 3: Clean solution
Write-Host "`nStep 3: Running dotnet clean..." -ForegroundColor Yellow
dotnet clean --verbosity quiet
Write-Host "? Solution cleaned" -ForegroundColor Green

# Step 4: Rebuild
Write-Host "`nStep 4: Rebuilding solution..." -ForegroundColor Yellow
$buildResult = dotnet build --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!" -ForegroundColor Green
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "1. Make sure Visual Studio debug is stopped" -ForegroundColor White
    Write-Host "2. Close all Visual Studio instances" -ForegroundColor White
    Write-Host "3. Try running as Administrator" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ? ALL DONE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Open Visual Studio" -ForegroundColor White
Write-Host "2. Press F5 to start debugging" -ForegroundColor White
Write-Host "3. Clear browser cache (Ctrl + Shift + Delete)" -ForegroundColor White
Write-Host "4. Test the profile dropdown" -ForegroundColor White
Write-Host ""
Write-Host "Expected improvements:" -ForegroundColor Cyan
Write-Host "? Smooth dropdown animation" -ForegroundColor Green
Write-Host "? Menu items with gradient border" -ForegroundColor Green
Write-Host "? Logout button with better red color" -ForegroundColor Green
Write-Host "? Avatar scale on hover" -ForegroundColor Green
Write-Host "? Chevron icon rotation" -ForegroundColor Green
Write-Host ""

# Wait for user
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
