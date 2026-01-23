param (
    [string]$ArtifactPath,	
	[string]$ServicesActive,
	[string]$PathExtraServices,
	[string]$TestResultPath
)

function End_Process {
    param (
        [string]$nameProcess
    )

    try {
        # Obtener el proceso cuyo nombre contenga el texto proporcionado
        $proceso = Get-Process | Where-Object {
            $_.MainModule.FileName -like "*$nameProcess*"
        }

        if ($proceso) {
            $proceso | Stop-Process -Force
            Write-Host "✅ Process '$nameProcess' stopped successfully."
         
        }
        else {
            Write-Host "⚠️ No running process found matching: $nameProcess"
        }
    } catch {
        Write-Host "❌ Error stopping process '$nameProcess': $_"
    }
}

function GetAppInstallNameID {
    param (
        [string]$identifierPattern1 = "", #"C2DE6DFC-5006-4F36-A151-E19A945E85E7*",
        [string]$identifierPattern2 = "Romdas.dataview2*",
        [string]$typeData = "Name" 
    )   
   
        $installedApps = Get-AppxPackage
   
        #$matchingApp = $installedApps | Where-Object { $_.PackageFamilyName -match $identifierPattern1 -or $_.Name -match $identifierPattern2 }
        $matchingApp = $installedApps | Where-Object { $_.Name -match $identifierPattern2 }
    
        if ($matchingApp) {
            if ($typeData -eq "ID") {
                return $matchingApp.PackageFamilyName  
            } else {
                return $matchingApp.PackageFullName       
            }
        }
    

    return $null  
}

function Start-Romdas {
    param (
        [string]$appPackageName 
    )

     Write-Host "Starting Romdas DataView2, wait a few seconds..."
     #$appPackageName = "Romdas.dataview2_zz8shxaabb20j" 
    $appIdentifier = "shell:AppsFolder\$appPackageName!App"

   
    try {     
        Start-Process "explorer.exe" -ArgumentList $appIdentifier
        Write-Host "App $appPackageName started successfully." -ForegroundColor Cyan
        #Write-Log "App $appPackageName started successfully."
    } catch {
        Write-Host "Error starting $appPackageName." -ForegroundColor Red
        #Write-Log "Starting app failed: $($_ | Out-String)"        
    }
}

function UninstallApp {
    param (
        [string]$NameApp,
        [string]$CodeApp
    )   
    # $WindowsApps = Get-AppxPackage -AllUsers | Where-Object { $_.Name -like "$NameApp" }
    $WindowsApps = Get-AppxPackage  | Where-Object { $_.Name -like "$NameApp" }

    if ($WindowsApps.Count -gt 0) {
        $WindowsApps | ForEach-Object {
            $packageName = $_.PackageFullName
            Write-Host "Uninstalling: $packageName"
			

      #      Write-Log "Uninstalling: $packageName"
        
            try {
                Remove-AppxPackage -Package $packageName -ErrorAction Stop
                Write-Host "✅Successfully uninstalled: $packageName"
            } catch {
                Write-Host "❌ Error uninstalling: $packageName" 
				Write-Host "❌ Exception: $($_.Exception.Message)" 
			    Write-Host "❌ StackTrace: $($_.ScriptStackTrace)"
            }
        }
    } else {
        #Write-Host "No matching registered apps found." -ForegroundColor Yellow
   #     Write-Log "No matching registered apps found." 
    }

    
    $ProvisionedApps = Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -like "$NameApp" }

    if ($ProvisionedApps.Count -gt 0) {
        $ProvisionedApps | ForEach-Object {
            $provPackageName = $_.PackageName
    #        Write-Log "Removing provisioned package: $provPackageName"

            Start-Process -FilePath "cmd.exe" -ArgumentList "/c dism /Online /Remove-ProvisionedAppxPackage /PackageName:$provPackageName" -Wait -NoNewWindow
        }
    } else {
        #Write-Host "No provisioned packages found." -ForegroundColor Yellow
    #    Write-Log "No provisioned packages found."
    }

    
    $AppFolders = Get-ChildItem $WindowsAppsPath -Directory | Where-Object { $_.Name -like "$NameApp" }

    if ($AppFolders.Count -gt 0) {
        $AppFolders | ForEach-Object {
            $folderPath = $_.FullName
    #        Write-Log "Taking ownership of: $folderPath"        
            
            Start-Process -FilePath "cmd.exe" -ArgumentList "/c takeown /F `"$folderPath`" /R /D Y" -Wait -NoNewWindow
            Start-Process -FilePath "cmd.exe" -ArgumentList "/c icacls `"$folderPath`" /grant Administrators:F /T /C /Q" -Wait -NoNewWindow

    #        Write-Log "Deleting folder: $folderPath"
        
            try {
                Remove-Item -Path $folderPath -Recurse -Force -ErrorAction Stop
    #            Write-Log "Successfully deleted: $folderPath" -ForegroundColor Cyan
            } catch {
    #            Write-Log "Error deleting: $folderPath" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "No residual folders found." -ForegroundColor Yellow
  #      Write-Log "No residual folders found."  
    }
}

function InstallAppWithRetries {
    param (
        [string]$MsixPath,
        [int]$MaxRetries = 3,
        [int]$DelaySeconds = 5
    )

    if (-not (Test-Path $MsixPath)) {
        Write-Host "❌ File .msix doesn't exist at: $MsixPath"
        return
    }

    $attempt = 0
    $installed = $false

    while (-not $installed -and $attempt -lt $MaxRetries) {
        try {
            Write-Host "📦 Installing MSIX: $MsixPath (Intento $($attempt + 1))"
            Add-AppxPackage -Path $MsixPath -ErrorAction Stop
            Write-Host "✅ Installation completed successfully."
            $installed = $true
        } catch {
            $attempt++
            Write-Host "⚠️ Error intalling: $($_.Exception.Message)"
            if ($attempt -lt $MaxRetries) {
                Write-Host "🔁 Retrying in $DelaySeconds seconds..."
                Start-Sleep -Seconds $DelaySeconds
            } else {
                Write-Host "❌ Failed after $attempt tries."
            }
        }
    }
}

function Start_ServiceProcess {
    param (
        [string]$pathExtraServicesName  
    )

    
    $paths = $pathExtraServicesName -split ',' | ForEach-Object { $_.Trim() }

    # Maximum attempts and wait (in seconds)
    $maxRetries = 4
    $retryDelay = 15

    # Paths still not found
    $pendingPaths = $paths.Clone()

    for ($attempt = 1; $attempt -le $maxRetries; $attempt++) {
        Write-Host "🔍 Try $($attempt) of $($maxRetries): Searching pending paths..."

        $newPendingPaths = @()

        foreach ($path in $pendingPaths) {
            if (Test-Path -Path $path -PathType Container) {

             $isWSProcessing = $path -like "*DataView2.WS.Processing*"

                if ($isWSProcessing) {
                   Write-Host "⏳ Additional wait for DataView2.WS.Processing." -ForegroundColor Cyan
                   Start-Sleep -Seconds 35
                }

                # Get more intenal folder path
                $folderName = Split-Path -Path $path -Leaf

                # Build executable full path
                $exePath = Join-Path -Path $path -ChildPath "$folderName.exe"

                if (Test-Path -Path $exePath -PathType Leaf) {
                    Write-Host "🔹 Executing: $exePath" -ForegroundColor Green
                    try {
                        Start-Process -FilePath $exePath -WorkingDirectory $path 
                    }
                    catch {
                        Write-Host "❌ Error running [$exePath]: $_" -ForegroundColor Red
                    }
                }
                else {
                    Write-Host "⚠️ Executable was not found: $exePath" -ForegroundColor Yellow
                }
            }
            else {
                Write-Host "⏳ Path not found (It will be retried): $path" -ForegroundColor Yellow
                
                $newPendingPaths += $path
            }
        }

        # Update path list
        $pendingPaths = $newPendingPaths

        if ($pendingPaths.Count -eq 0) {
            Write-Host "✅ All paths has been processed successfully." -ForegroundColor Green
            break
        }

        if ($attempt -lt $maxRetries) {
            Write-Host "🔄 Retrying in $retryDelay seconds..." -ForegroundColor Cyan
            Start-Sleep -Seconds $retryDelay
        }
    }

    if ($pendingPaths.Count -gt 0) {
        Write-Host "❌ The following paths was not found after $maxRetries attempts:" -ForegroundColor Red
        $pendingPaths | ForEach-Object { Write-Host "📍 $_" }
    }
}

function Test_Services {
    param (
        [string]$savePath,
        [string]$services
    )

    $setServices = $services.Split(',') | ForEach-Object { $_.Trim() }
    $servicesFoundFinal = [System.Collections.ArrayList]@()
    $runningProcesses = @{}
    $testCaseResults = @{}  
    $secondsWait=25

    function Run-ServiceTest {
        param ([string[]]$serviceList)

        foreach ($service in $serviceList) {
            $process = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq "$service" }  #-like "*$service*" }

            if ($process) {
                Write-Host "✅ $service process is running."

                if (-not $servicesFoundFinal.Contains($service)) {
                    $null = $servicesFoundFinal.Add($service)

                    # Saving only the first process found for this service
                    if (-not $runningProcesses.ContainsKey($service)) {
                        $runningProcesses[$service] = $process
                    }
                }

                # Save orr update  the result for this service
                $testCaseResults[$service] = "    <testcase classname=""StartupTests"" name=""$service Starts_Successfully"" />"
            } else {
                Write-Host "❌ $service process was not found running."
                $testCaseResults[$service] = "    <testcase classname=""StartupTests"" name=""$service Failed"" />"
            }
        }
    }

    # Test attempts
    $attempt = 1
    $maxAttempts = 3

    while ($attempt -le $maxAttempts) {
        Write-Host "`n[INFO] Attempt #$attempt testing services..."
        Run-ServiceTest -serviceList $setServices

        if ($servicesFoundFinal.Count -eq $setServices.Count) {
            break
        }

        if ($attempt -lt $maxAttempts) {
            Write-Host "[INFO] Retrying in $secondsWait seconds..."
            Start-Sleep -Seconds $secondsWait
        }

        $attempt++
    }

    # Generate final XML without duplicates
    $failures = ($setServices.Count - $servicesFoundFinal.Count)
    $totalTests = $setServices.Count
    $testCasesXmlTotal = ($setServices | ForEach-Object { $testCaseResults[$_] }) -join "`n"

    $testResult = @"
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="StartupTests" tests="$totalTests" failures="$failures">
$testCasesXmlTotal
</testsuite>
"@

    # Save XML file
    if (Test-Path $savePath) {
        $resultsPath = Join-Path $savePath "test-results.xml"
        $testResult | Out-File -FilePath $resultsPath -Encoding utf8
        Write-Host "[INFO] Test results written to: $resultsPath"
    } else {
        Write-Host "[ERROR] Save path not found: $savePath"
    }

    # DEBUG: Show which services were found in some attempt
    Write-Host "`n[DEBUG] Services found during any attempt:"
    if ($servicesFoundFinal.Count -gt 0) {
        foreach ($svc in $servicesFoundFinal) {
            Write-Host "   - $svc"
        }
    } else {
        Write-Host "   (none)"
    }

    # Final summary
    if ($servicesFoundFinal.Count -eq 0) {
        Write-Host "`n❌ No services were ever found running across all attempts. Test stopped."
        #exit 1
    }
    elseif ($servicesFoundFinal.Count -lt $setServices.Count) {
        $foundNames = $servicesFoundFinal -join ", "
        Write-Host "`n❌ Only these services were found running ($foundNames). Test stopped."
        #exit 1
    }
    else {
        Write-Host "`n✅ All services were found running. Test stopped."
    }


    #Printing processes registered:
    write-host "`n[debug] contents of `\$runningprocesses`:"
    if ($runningprocesses.count -gt 0) {
        foreach ($key in $runningprocesses.keys) {
            write-host "🔹 service: $key"
            $procs = $runningprocesses[$key]
            if ($procs -is [system.array]) {
                foreach ($proc in $procs) {
                    write-host "   ↪ processname: $($proc.processname), id: $($proc.id)"
                }
            } else {
                write-host "   ↪ (non-array) processname: $($procs.processname), id: $($procs.id)"
            }
        }
    } else {
        write-host "   (runningprocesses is empty)"
    }


   
    if ($runningProcesses.Count -gt 0) {
        Write-Host "`n[INFO] Stopping services that were found running..."

        # We use a set to ensure that each ID stops only once.
        $processedIds = @()

        foreach ($svc in $runningProcesses.Keys) {
            $proc = $runningProcesses[$svc]

            if ($proc -and $proc.Id -and -not ($processedIds -contains $proc.Id)) {
                Write-Host "[INFO] Stopping process: $($proc.ProcessName) (ID: $($proc.Id))"
                try {
                    Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                    Write-Host "✅ $($proc.ProcessName) successfully stopped."
                    # Add ID into the processed list
                    $processedIds += $proc.Id
                }
                catch {
                    Write-Host "❌ Failed to stop process ID $($proc.Id): $_" -ForegroundColor Red
                }
            }
            else {
                Write-Host "⚠️ Process for '$svc' already stopped or invalid." -ForegroundColor Yellow
            }
        }
    }
}



#Start Tests:
#$ArtifactPath= "C:\azagent\A1\DataView2_Installer_Files\win-x64\"
#$ServicesActive= "DataView2,DataView2.GrpcService,DataView2.WS.Processing"
#$TestResultPath= "C:\azagent\A1\ArtifactDataView"
#$pathExtraServices = "C:\DataView2\DataView2Services\DataView2.WS.Processing"

 Write-Host "Services PathExtraServices : $PathExtraServices" 

$msixPathFiles = $ArtifactPath
$testPassed = $false

# =====================================
# 1- Stop existing processes
# =====================================
foreach ($service in $ServicesActive -split ',') {
    End_Process -nameProcess $service.Trim()
}

Start-Sleep -Seconds 3

# =====================================
# 2- Delete services folder
# =====================================
$serviceFolder = "C:\DataView2\DataView2Services" 
if (Test-Path $serviceFolder) {
    Remove-Item -Path $serviceFolder -Recurse -Force
    Write-Host "Services folder upgraded." -ForegroundColor DarkGray
}

# Delete version file
$cfgFile = "C:\DataView2\version.json"
if (Test-Path -Path $cfgFile) {
    Remove-Item -Path $cfgFile -Force
}

#3- Uninstall installer:
#set-executionpolicy unrestricted -scope process
#uninstallapp -nameapp "Romdas.dataview2*" -codeapp "agre6dfc-5006-4f36-a151-e19a945e85e7*"
#start-sleep -seconds 5

#Get path installer:
#Write-Host "[INFO] Let's see the msixPathFiles: $msixPathFiles"


# =====================================
# 3- Install MSIX application
# =====================================
$msixFiles = Get-ChildItem -Path $msixPathFiles -Filter "DataView2*.msix"
Write-Host "[INFO] Path of the msixFiles: $msixFiles"

if ($msixFiles -ne $null) {
    $latestMsixFile = $msixFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $msixPath = $latestMsixFile.FullName
    Write-Host "[INFO] Installing MSIX: $msixPath"
    
    if ($msixPath -ne $null) {
        Write-Host "Installing Romdas $latestMsixFile..." -ForegroundColor Yellow
        try {
            # Execute installation with retries
            InstallAppWithRetries -MsixPath $msixPath -MaxRetries 3 -DelaySeconds 5
            Write-Host "Romdas DataView2 $latestMsixFile was successfully installed." -ForegroundColor Cyan
            
            # Get installed package name
            $packageNameApp = GetAppInstallNameID -typeData "ID"

            # =====================================
            # 4- Start the application
            # =====================================
            Start-Romdas -appPackageName $packageNameApp
            Write-Host "Starting Romdas DataView2, wait a few seconds..." -ForegroundColor DarkGreen
            Start-Sleep -Seconds 50

            # =====================================
            # 5- Start extra services (parallel if any)
            # =====================================
            Start_ServiceProcess -pathExtraServicesName $PathExtraServices
            # NOTE: This step may launch parallel processes internally. It does not block the pipeline.

            Start-Sleep -Seconds 20

            # =====================================
            # 6- Test services and close
            # =====================================
            # This is the final step that blocks the script completion.
            Test_Services -savePath $TestResultPath -services $ServicesActive
            # The YAML pipeline will wait until this step finishes.
            
        }
        catch {
            Write-Host "❌ Romdas DataView2 $latestMsixFile was not installed. Try again." 
            Write-Host "❌ Exception: $($_.Exception.Message)" 
            Write-Host "❌ StackTrace: $($_.ScriptStackTrace)"
        }
    }
}