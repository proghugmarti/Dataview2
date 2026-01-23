param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath, 

    [Parameter(Mandatory = $true)]
    [string]$AccessToken
)

# --- INICIALIZATION & VALIDATION ---

# Load the required library for compatibility with Windows PowerShell
try {
    Add-Type -AssemblyName System.Net.Http
    Add-Type -AssemblyName System.Web
}
catch {
    Write-Error "The assembly could not be loaded System.Net.Http. This script requires .NET Framework 4.5+ or PowerShell 7+."
    return # Return null
}
 
if (-not (Test-Path $FilePath -PathType Leaf)) {
    Write-Error "File is not found at this path: '$FilePath'"
    return # Return null
}

# --- CONFIGURATION ---
$chunkSize = 60MB # Robust fragment size
$fileSize = (Get-Item $FilePath).Length
$fileName = [System.IO.Path]::GetFileName($FilePath)

# --- STEP 1: Create load session  ---
Write-Verbose "➡️ Creting load sesion for '$fileName'..."
$encodedFileName = [System.Web.HttpUtility]::UrlEncode($fileName)
$sessionUrl = "https://graph.microsoft.com/v1.0/me/drive/root:/${encodedFileName}:/createUploadSession"
$sessionBody = @{ "@microsoft.graph.conflictBehavior" = "rename" } | ConvertTo-Json

try {
    $uploadSession = Invoke-RestMethod -Uri $sessionUrl `
        -Method POST `
        -Headers @{ Authorization = "Bearer $AccessToken"; "Content-Type" = "application/json" } `
        -Body $sessionBody

    $uploadUrl = $uploadSession.uploadUrl
    Write-Verbose "✅ Load session created successfully."
    Write-Verbose "   Upload URL: $($uploadUrl.Substring(0, 70))..."
}
catch {
    Write-Error "Error creating load session: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $errorBody = $_.Exception.Response.GetResponseStream() | ForEach-Object { (New-Object System.IO.StreamReader($_)).ReadToEnd() }
        Write-Error "Server response: $errorBody"
    }
    return # Return null
}

# --- STEP 2: Upload by fragments with HttpClient ---
Write-Verbose "📤 Starting upload of '$fileName' ($([math]::Round($fileSize/1MB, 2)) MB) in fragments of $([math]::Round($chunkSize/1MB, 2)) MB..."

$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.Timeout = [System.TimeSpan]::FromMinutes(60) # Timeout generoso (60 minutes per fragment)

$lastResponseObject = $null
$fileStream = $null
try {
    $fileStream = [System.IO.File]::OpenRead($FilePath)
    $buffer = New-Object byte[] $chunkSize
    $offset = 0

    while ($offset -lt $fileSize) {
        $bytesRead = $fileStream.Read($buffer, 0, $chunkSize)
        if ($bytesRead -eq 0) { break }

        $chunk = if ($bytesRead -eq $chunkSize) { $buffer } else { $buffer[0..($bytesRead - 1)] }
        
        $startRange = $offset
        $endRange = $offset + $bytesRead - 1
        $progress = [math]::Round(($endRange + 1) / $fileSize * 100)
        
        Write-Verbose "   Uploading fragment: bytes $startRange-$endRange de $fileSize ($progress %)"
        
        $content = [System.Net.Http.ByteArrayContent]::new($chunk)
        $content.Headers.Add("Content-Range", "bytes $startRange-$endRange/$fileSize")

        $response = $httpClient.PutAsync($uploadUrl, $content).GetAwaiter().GetResult()

        if (-not $response.IsSuccessStatusCode) {
            $errorContent = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            throw "API Error: $($response.StatusCode) - $errorContent"
        }

        $jsonResponse = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($jsonResponse) {
            $lastResponseObject = $jsonResponse | ConvertFrom-Json
        }
        
        $offset += $bytesRead
    }
}
catch {
    Write-Error "Error during the upload of the fragment $startRange - ${endRange}: $($_.Exception.Message)"
    return # Return null
}
finally {
    if ($fileStream) { $fileStream.Dispose() }
    if ($httpClient) { $httpClient.Dispose() }
}

# --- STEP 3: Returning the result ---
if ($lastResponseObject -and $lastResponseObject.webUrl) {
    Write-Verbose "✅ Load Process completed successfully."
    # Returning ONLY the URL to the output standard stream  
    return $lastResponseObject.webUrl
}
else {
    Write-Error "Upload finalized, but it wasn't received a valid response with the file URL."
    return # Return null
}
