# Complete Fix for Encoding Issues
# Run this whenever you see garbled Vietnamese characters

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPLETE ENCODING FIX" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop any running processes
Write-Host "Step 1: Stopping running processes..." -ForegroundColor Yellow
Get-Process -Name "iisexpress","dotnet","Webgiasu" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "? Processes stopped" -ForegroundColor Green
Write-Host ""

# Step 2: Clean temp files
Write-Host "Step 2: Cleaning temp .cshtml files..." -ForegroundColor Yellow
$tempPath = "$env:LOCALAPPDATA\Temp"
$tempFiles = Get-ChildItem -Path $tempPath -Filter "*.cshtml" -ErrorAction SilentlyContinue
if ($tempFiles) {
    $tempFiles | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "? Removed $($tempFiles.Count) temp files" -ForegroundColor Green
} else {
    Write-Host "? No temp files found" -ForegroundColor Green
}
Write-Host ""

# Step 3: Clean Visual Studio temp folder
Write-Host "Step 3: Cleaning VS temp folders..." -ForegroundColor Yellow
$vsTempPaths = @(
    "$env:LOCALAPPDATA\Microsoft\VisualStudio",
    "$env:TEMP\VSD*",
    "$env:TEMP\.vs"
)
foreach ($path in $vsTempPaths) {
    Get-ChildItem -Path $path -Filter "*.cshtml" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
}
Write-Host "? VS temp folders cleaned" -ForegroundColor Green
Write-Host ""

# Step 4: Verify all source files have UTF-8 BOM
Write-Host "Step 4: Verifying source files encoding..." -ForegroundColor Yellow
$files = Get-ChildItem -Path "Webgiasu\Views" -Filter "*.cshtml" -Recurse | Where-Object {
    $_.FullName -notmatch '\\bin\\|\\obj\\|\\Temp\\'
}
$needFix = 0
foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $hasBOM = ($bytes.Length -ge 3) -and 
              ($bytes[0] -eq 0xEF) -and 
              ($bytes[1] -eq 0xBB) -and 
              ($bytes[2] -eq 0xBF)
    
    if (!$hasBOM) {
        Write-Host "  ? Fixing: $($file.Name)" -ForegroundColor Yellow
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $utf8WithBom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8WithBom)
        $needFix++
    }
}
if ($needFix -gt 0) {
    Write-Host "? Fixed $needFix files" -ForegroundColor Green
} else {
    Write-Host "? All files already have UTF-8 BOM" -ForegroundColor Green
}
Write-Host ""

# Step 5: Clean solution
Write-Host "Step 5: Cleaning solution..." -ForegroundColor Yellow
dotnet clean --verbosity quiet
Write-Host "? Solution cleaned" -ForegroundColor Green
Write-Host ""

# Step 6: Remove bin and obj
Write-Host "Step 6: Removing bin and obj folders..." -ForegroundColor Yellow
Remove-Item -Recurse -Force "Webgiasu\bin","Webgiasu\obj" -ErrorAction SilentlyContinue
Write-Host "? Folders removed" -ForegroundColor Green
Write-Host ""

# Step 7: Rebuild
Write-Host "Step 7: Rebuilding solution..." -ForegroundColor Yellow
$buildResult = dotnet build --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!" -ForegroundColor Green
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ? ENCODING FIXED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Close ALL instances of Visual Studio" -ForegroundColor White
Write-Host "2. Delete this temp file manually:" -ForegroundColor White
Write-Host "   $env:LOCALAPPDATA\Temp\*.cshtml" -ForegroundColor DarkGray
Write-Host "3. Reopen Visual Studio" -ForegroundColor White
Write-Host "4. Start debugging (F5)" -ForegroundColor White
Write-Host "5. Vietnamese text should display correctly!" -ForegroundColor White
Write-Host ""
Write-Host "If still see garbled text:" -ForegroundColor Yellow
Write-Host "• Clear browser cache: Ctrl + Shift + Delete" -ForegroundColor White
Write-Host "• Hard reload: Ctrl + F5" -ForegroundColor White
Write-Host "• Check browser encoding is UTF-8" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
