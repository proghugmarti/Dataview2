function Update-AzVariableGroupValue {
    [CmdletBinding()]

param (
    [string]$Organization,
    [string]$Project,
    [string]$GroupId,
    [string]$VariableName,
    [string]$NewValue,
    [string]$SystemAccessToken
)

$ErrorActionPreference = "Stop"
 
  if (-not $Organization) { throw "Organizationis required." }
  if (-not $Project)      { throw "Projectis required." }
  if (-not $GroupId)     { throw "GroupIdis required." }
  if (-not $VariableName){ throw "VariableNameis required." }
  if (-not $NewValue)    { throw "NewValueis required." }
  if (-not $SystemAccessToken) { throw "SystemAccessTokenis required." }

   # Logs
   # Write-Host "Organization   : '$Organization'"
   # Write-Host "Project        : '$Project'"
   # Write-Host "GroupId        : '$GroupId'"
   # Write-Host "VariableName   : '$VariableName'"
   # Write-Host "NewValue       : '$NewValue'"

# Group URL
$url = "https://dev.azure.com/$Organization/$Project/_apis/distributedtask/variablegroups/${GroupId}?api-version=7.0-preview.2"
Write-Host "🌐 URL: $url"

# Headers with token 
$headers = @{
    Authorization = "Bearer $SystemAccessToken"
    "Content-Type" = "application/json"
}


# Verify connexion and existence before updating 
try {
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers $headers
    #Write-Host "🔍 Group founded: $($response.name)"
} catch {
    Write-Error "❌ Error accessing the variable group."
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $statusDescription = $_.Exception.Message
        Write-Host "StatusCode:" $statusCode
        Write-Host "StatusDescription:" $statusDescription
    } else {
        Write-Host "Server could not be founded."
    }
    exit 1
}


# Update variable 
if ($response.variables.PSObject.Properties.Name -contains $VariableName) {
    $response.variables.$VariableName.value = $NewValue
} else {
    Write-Host "Variables available:"
    $response.variables.PSObject.Properties.Name | Out-Host
    Write-Error "❌ The variable '$VariableName' does not exist at the grupo ID $GroupId."
    exit 1
}

# Convert to JSON
$body = $response | ConvertTo-Json -Depth 10

# Send Update
try {
    $responseUpdate = Invoke-RestMethod -Uri $url -Method Put -Headers $headers -Body $body
    Write-Host "✅ '$VariableName' updated. '" 
   # Write-Host "✅ Variable '$VariableName' updated to '$NewValue'"
} catch {
    Write-Error "❌ Error updating variable: '$VariableName' $_"
    if ($_.Exception.Response) {
        Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__
        Write-Host "Response:" $_.ErrorDetails.Message
    }
    exit 1
}

}

