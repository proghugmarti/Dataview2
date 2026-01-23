function UninstallApp {
    param (
        [string]$NameApp,
        [string]$CodeApp
    )

    $maxRetries = 3

    # Get the installed packages
    #$WindowsApps = Get-AppxPackage | Where-Object { $_.Name -like "$NameApp" }
    $WindowsApps = Get-AppxPackage | Where-Object {
       $_.Name.ToLower() -like "*$($NameApp.ToLower())*" }
    if ($WindowsApps.Count -gt 0) {
        $WindowsApps | ForEach-Object {
            $packageName = $_.PackageFullName
            Write-Host "🔄 Trying to uninstall: $packageName"

            $attempt = 0
            $success = $false

            while (-not $success -and $attempt -lt $maxRetries) {
                try {
                    Remove-AppxPackage -Package $packageName -ErrorAction Stop
                    Write-Host "✅ Successfully uninstalled: $packageName"
                    $success = $true
                } catch {
                    $attempt++
                    Write-Host "⚠️ Error uninstalling attempt $attempt : $($_.Exception.Message)"
                    if ($attempt -lt $maxRetries) {
                        Write-Host "🔁 Reintentando en 5 segundos..."
                        Start-Sleep -Seconds 5
                    } else {
                        Write-Host "❌ Failed after $attempt attempts: $packageName"
                    }
                }
            }
        }
    } else {
        Write-Host "⚠️ No matching registered apps found." 
    }

    # Remove provisioned packages
    $ProvisionedApps = Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -like "$NameApp" }

    if ($ProvisionedApps.Count -gt 0) {
        $ProvisionedApps | ForEach-Object {
            $provPackageName = $_.PackageName
            Write-Host "🔄 Removing provisioned package: $provPackageName"
            Start-Process -FilePath "cmd.exe" -ArgumentList "/c dism /Online /Remove-ProvisionedAppxPackage /PackageName:$provPackageName" -Wait -NoNewWindow
        }
    }

    # Remove residual folders (only if $WindowsAppsPath is defined).
    if ($WindowsAppsPath) {
        $AppFolders = Get-ChildItem $WindowsAppsPath -Directory | Where-Object { $_.Name -like "$NameApp" }

        if ($AppFolders.Count -gt 0) {
            $AppFolders | ForEach-Object {
                $folderPath = $_.FullName
                Write-Host "🧹 Removing folder: $folderPath"

                Start-Process -FilePath "cmd.exe" -ArgumentList "/c takeown /F `"$folderPath`" /R /D Y" -Wait -NoNewWindow
                Start-Process -FilePath "cmd.exe" -ArgumentList "/c icacls `"$folderPath`" /grant Administrators:F /T /C /Q" -Wait -NoNewWindow

                try {
                    Remove-Item -Path $folderPath -Recurse -Force -ErrorAction Stop
                    Write-Host "✅ Folder removed: $folderPath"
                } catch {
                    Write-Host "❌ Error removing folder: $($_.Exception.Message)"
                }
            }
        } else {
            Write-Host "No residual folders found." -ForegroundColor Yellow
        }
    }
}


Set-ExecutionPolicy Unrestricted -Scope Process
UninstallApp -NameApp "dataview" -CodeApp "AGRE6DFC-5006-4F36-A151-E19A945E85E7*"


#Start-Sleep -Seconds 10