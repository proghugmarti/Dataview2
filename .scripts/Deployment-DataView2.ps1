# Last Updated 25/11/2024 tensorflow
#Set-ExecutionPolicy Unrestricted -Scope Process
 
#cd C:\azagent\A1\_work\2\s\
#$projectRootPath="C:\azagent\A1\_work\2\s\"
#$pswCertificate=""
#$PSScriptRoot="C:\azagent\A1\_work\2\s\DataView2\.scripts"
#$devopsProjectName="DataView2"
param(
    [string]$projectRootPath,
    [string]$pswCertificate,
    [string]$devopsProjectName,
    [string]$certificateSubjectName,
    [string]$certificateFileName
)

$PSScriptRoot="$projectRootPath\$devopsProjectName\.scripts"

#Write-Host "🔹 projectRootPath: $projectRootPath"
#Write-Host "🔹 pswCertificate: $pswCertificate"
#Write-Host "🔹 devopsProjectName: $devopsProjectName"
Write-Host "🔹 devopsProjectName: $certificateSubjectName"

$attempt = $env:RETRY_ATTEMPT
if (-not $attempt) { $attempt = 1 } else { $attempt = [int]$attempt + 1 }
Write-Host "🔁 Attempt #$attempt"
Write-Host "##vso[task.setvariable variable=RETRY_ATTEMPT]$attempt"

if (-not (Test-Path $projectRootPath)) {
    Write-Error "❌ Ruta base inválida: $projectRootPath"
    exit 1
}

Write-Host "📂 Changed at the directory: $projectRootPath"
 

$appName="DataView2"
$pfxFilePath = Join-Path -Path $PSScriptRoot -ChildPath $certificateFileName

if (-not (Test-Path $pfxFilePath)) {
    Write-Error "❌ File .pfx not found: $pfxFilePath"
    exit 1
}

Write-Host "📥 Importing certify .pfx"

$password = ConvertTo-SecureString -String $pswCertificate -Force -AsPlainText  # <-- Si tiene contraseña, ponela aquí

$importedCert = Import-PfxCertificate -FilePath $pfxFilePath -CertStoreLocation "cert:\CurrentUser\My" -Password $password 

if (-not $importedCert) {
    Write-Error "❌ Certify could not be imported"
    exit 1
}

$cert = $importedCert.Thumbprint
#Write-Host "🔐 Certify imported, Thumbprint: $cert"
Write-Host "🔐 Certify imported"


function Delete-Folder
{
    param([string]$folder)
    if(Test-Path -Path "$folder")
    {
        Remove-Item -Path "$folder" -Recurse -Force
    }
}
function Copy-BuildFiles {
    param([string]$projectName, [string]$target, [string]$publishFolder)
    
    $sourcePath = ".\$devopsProjectName\$projectName\bin\Release\$target\win-x64"
    $destinationPath = "$publishFolder\$projectName"

    if( $projectName -match "DataView2.WS.Processing"){$sourcePath=".\$projectName\$projectName\bin\Release\$target\win-x64"}

    if (Test-Path -Path $sourcePath) {
        Write-Host "Copying files from $sourcePath to $destinationPath"
        # Create folder if not exits
        if (-not (Test-Path -Path $destinationPath)) {
            New-Item -ItemType Directory -Path $destinationPath -Force
        }

        # Copy folders and files
        Copy-Item -Path "$sourcePath\*" -Destination $destinationPath -Recurse -Force
    } else {
        Write-Host "The origin folder $sourcePath was not found."
    }
}

function Publish-Project
{
    param([string]$projectName, [string]$target, [string]$publishFolder, [string]$version, [bool]$singleFile, [bool]$maui)    
    Delete-Folder ".\$devopsProjectName\$projectName\bin"
    Delete-Folder ".\$devopsProjectName\$projectName\obj"    
    $fullPath = "$publishFolder\$projectName"
    if ($projectName -match "DataView2.WS.Processing"){
       $oldEAP = $ErrorActionPreference
       & dotnet publish  ".\$devopsProjectName\$projectName\$projectName\$projectName.csproj" -c Release -o "$publishFolder\$projectName" -f "$target" -p:Version="$version" -p:PublishSingleFile=true -r win-x64 --no-self-contained
       $ErrorActionPreference = $oldEAP 
    }
    else{
     if ($singleFile) {
        $oldEAP = $ErrorActionPreference
        & dotnet publish  ".\$devopsProjectName\$projectName\$projectName.csproj" -c Release -o "$publishFolder\$projectName" -f "$target" -p:Version="$version" -p:PublishSingleFile=true -r win-x64 --no-self-contained
        $ErrorActionPreference = $oldEAP 
        }
     else {  
        $oldEAP = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
            & dotnet publish  ".\$devopsProjectName\$projectName\$projectName.csproj" -c Release -o "$publishFolder\$projectName" -f "$target" -p:Version="$version"  
        $ErrorActionPreference = $oldEAP 
        }          
    }
}
#Clear-Host
Write-Host "`n----- Next Step -----`n"
#Set-PSDebug -Trace 2
$ErrorActionPreference = "Stop"
[xml]$buildPropsXmlDocument = Get-Content -Path ".\Directory.Build.props"
$version = $buildPropsXmlDocument.Project.PropertyGroup.AssemblyVersion
Write-Host "Version: $version"
$publishFolder = ".\$devopsProjectName\.publish\$version"
Delete-Folder $publishFolder

Publish-Project "DataView2.GrpcService" "net9.0-windows10.0.26100.0" $publishFolder $version $false $false
Publish-Project "DataView2.WS.Processing" "net9.0-windows" $publishFolder $version $true $false
Copy-BuildFiles "DataView2.WS.Processing" "net9.0-windows" $publishFolder

Get-ChildItem "$publishFolder\*.pdb" -Recurse | foreach { Remove-Item -Path $_.FullName }
Get-ChildItem "$publishFolder\*.Development.json" -Recurse | foreach { Remove-Item -Path $_.FullName }
Get-ChildItem "$publishFolder\web.config" -Recurse | foreach { Remove-Item -Path $_.FullName }

$currDate = Get-Date -Format "[dd-MMM-yyyy]"
Set-PSDebug -Off
Compress-Archive -Path "$publishFolder\*" -Update -DestinationPath ".\$devopsProjectName\.publish\v${version}_Services.zip" 
Copy-Item ".\$devopsProjectName\.publish\v${version}_Services.zip" "$appName\DataView2Services.zip"
#Clear-Host
Set-PSDebug -Trace 2
$filePath = "$devopsProjectName\$appName\$appName.csproj"
$oldText = ">None<"
$newText = ">MSIX<"

dotnet publish "$devopsProjectName\$appName\$appName.csproj" -f net9.0-windows10.0.26100.0 -c Release   -o "$publishFolder\$projectName" -p:WindowsAppSDKSelfContained=true -p:PackageCertificateThumbprint=$cert -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true #-p:IncludeAllContent=true

 
Set-PSDebug -Off
