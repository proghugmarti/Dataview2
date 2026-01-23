param (
    [string]$ArtifactPath,
    [string]$ExePath
)

#$exePath = "C:\azagent\A1\_work\1\s\Source\Romdas.LFOD.RTA\bin\Release\net6.0-windows10.0.22621.0\Romdas.LFOD.RTA.exe"
$exePath = $ExePath
$testPassed = $false

#Write-Host "ExePath: $exePath"    

if (Test-Path $exePath) {
    Write-Host "[DEBUG] Running App: $exePath"

     Start-Sleep -Seconds 10
     Write-Host "Starting test: Check if the executable starts..."
     Start-Process -FilePath $exePath
     Start-Sleep -Seconds 10

    # Search the process
    $process = Get-Process | Where-Object { $_.ProcessName -eq "Romdas.LFOD.RTA" }

    if ($process) {
        #Write-Host "✅ The Romdas.LFOD.RTA process is running."
        Write-Host "[SUCCESS] The Romdas.LFOD.RTA process is running."
        $testPassed = $true
    } else {
        #Write-Host "❌ The Romdas.LFOD.RTA process was not found running."
        Write-Host "[ERROR] The Romdas.LFOD.RTA process was not found running."
    }

# Generate results file in JUnit format
$testResult = @"
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="StartupTests" tests="1" failures="$(if ($testPassed) { '0' } else { '1' })">
  <testcase classname="StartupTests" name="Romdas.LFOD.RTA_Starts_Successfully">
    $(if (-not $testPassed) { '<failure message="Romdas.LFOD.RTA is not running." />' })
  </testcase>
</testsuite>
"@

    $resultsPath = Join-Path $ArtifactPath "test-results.xml"
    $testResult | Out-File -FilePath $resultsPath -Encoding utf8

    # Close the process at the end
    if ($process) {
        Stop-Process -Name "Romdas.LFOD.RTA" -Force
        Write-Host "[SUCCESS] Romdas.LFOD.RTA process stopped after the test."
    }

} else {
    Write-Host "[ERROR] File NOT found: $exePath"
}