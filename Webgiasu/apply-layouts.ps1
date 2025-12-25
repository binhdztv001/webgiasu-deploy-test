# Apply Layouts to All Views
# This script automatically adds layout declarations to all Student and Tutor views

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  APPLY LAYOUTS TO ALL VIEWS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalFiles = 0
$updatedFiles = 0
$skippedFiles = 0
$errors = 0

# Student Views
$studentViews = @(
    "Webgiasu\Views\Student\CreateProblem.cshtml",
    "Webgiasu\Views\Student\MyProblems.cshtml",
    "Webgiasu\Views\Student\ProblemDetails.cshtml",
    "Webgiasu\Views\Student\Payments.cshtml",
    "Webgiasu\Views\Student\Profile.cshtml",
    "Webgiasu\Views\Student\RateTutor.cshtml",
    "Webgiasu\Views\Student\BrowseTutors.cshtml",
    "Webgiasu\Views\Student\Messages.cshtml",
    "Webgiasu\Views\Student\PaymentHistory.cshtml",
    "Webgiasu\Views\Student\Settings.cshtml"
)

# Tutor Views
$tutorViews = @(
    "Webgiasu\Views\Tutor\AvailableProblems.cshtml",
    "Webgiasu\Views\Tutor\MyProblems.cshtml",
    "Webgiasu\Views\Tutor\ProblemDetails.cshtml",
    "Webgiasu\Views\Tutor\SubmitSolution.cshtml",
    "Webgiasu\Views\Tutor\MySolutions.cshtml",
    "Webgiasu\Views\Tutor\MyRatings.cshtml",
    "Webgiasu\Views\Tutor\Profile.cshtml",
    "Webgiasu\Views\Tutor\Earnings.cshtml",
    "Webgiasu\Views\Tutor\Messages.cshtml",
    "Webgiasu\Views\Tutor\Settings.cshtml"
)

function Update-ViewLayout {
    param(
        [string]$filePath,
        [string]$layoutPath,
        [string]$roleColor
    )
    
    if (!(Test-Path $filePath)) {
        Write-Host "  ? File not found: $filePath" -ForegroundColor Yellow
        return $false
    }
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    
    # Check if layout already set
    if ($content -match 'Layout\s*=\s*"~/Views/Shared/_.*Layout\.cshtml"') {
        Write-Host "  ? SKIP: $(Split-Path $filePath -Leaf) - Already has layout" -ForegroundColor DarkGray
        return $false
    }
    
    # Find the @{ block
    if ($content -match '@\{[\s\S]*?\}') {
        $match = $Matches[0]
        
        # Check if Layout is mentioned at all
        if ($match -match 'Layout') {
            # Replace existing layout
            $newMatch = $match -replace 'Layout\s*=\s*[^;]+;?', "Layout = ""$layoutPath"";"
            if (!($newMatch -match "Layout = ""$layoutPath"";")) {
                $newMatch = $newMatch.TrimEnd('}').TrimEnd() + "`n    Layout = ""$layoutPath"";`n}"
            }
        } else {
            # Add layout to existing block
            $newMatch = $match.TrimEnd('}').TrimEnd() + "`n    Layout = ""$layoutPath"";`n}"
        }
        
        $content = $content -replace [regex]::Escape($match), $newMatch
        
        try {
            $utf8NoBom = New-Object System.Text.UTF8Encoding($true)
            [System.IO.File]::WriteAllText($filePath, $content, $utf8NoBom)
            Write-Host "  ? UPDATED: $(Split-Path $filePath -Leaf)" -ForegroundColor $roleColor
            return $true
        } catch {
            Write-Host "  ? ERROR: $(Split-Path $filePath -Leaf) - $($_.Exception.Message)" -ForegroundColor Red
            return $null
        }
    } else {
        Write-Host "  ? SKIP: $(Split-Path $filePath -Leaf) - No @{} block found" -ForegroundColor Yellow
        return $false
    }
}

# Process Student Views
Write-Host "Processing Student Views..." -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????" -ForegroundColor DarkGray
foreach ($view in $studentViews) {
    $totalFiles++
    $result = Update-ViewLayout -filePath $view -layoutPath "~/Views/Shared/_StudentLayout.cshtml" -roleColor "Green"
    if ($result -eq $true) { $updatedFiles++ }
    elseif ($result -eq $false) { $skippedFiles++ }
    else { $errors++ }
}

Write-Host ""

# Process Tutor Views
Write-Host "Processing Tutor Views..." -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????" -ForegroundColor DarkGray
foreach ($view in $tutorViews) {
    $totalFiles++
    $result = Update-ViewLayout -filePath $view -layoutPath "~/Views/Shared/_TutorLayout.cshtml" -roleColor "Magenta"
    if ($result -eq $true) { $updatedFiles++ }
    elseif ($result -eq $false) { $skippedFiles++ }
    else { $errors++ }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total files processed: $totalFiles" -ForegroundColor White
Write-Host "? Updated: $updatedFiles" -ForegroundColor Green
Write-Host "? Skipped: $skippedFiles" -ForegroundColor Yellow
Write-Host "? Errors: $errors" -ForegroundColor Red
Write-Host ""

if ($updatedFiles -gt 0) {
    Write-Host "? Successfully updated $updatedFiles view(s)!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Review the updated files" -ForegroundColor White
    Write-Host "2. Run: .\quick-rebuild.ps1" -ForegroundColor White
    Write-Host "3. Test navigation between pages" -ForegroundColor White
    Write-Host "4. Verify sidebar and header persist" -ForegroundColor White
} else {
    Write-Host "? All files already have layouts or were skipped." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Updated views will now have:" -ForegroundColor Cyan
Write-Host "  • Persistent sidebar" -ForegroundColor White
Write-Host "  • Fixed header" -ForegroundColor White
Write-Host "  • Navigation menu" -ForegroundColor White
Write-Host "  • Profile dropdown" -ForegroundColor White
Write-Host "  • Notifications" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
