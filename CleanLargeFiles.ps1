# Script: CleanLargeFiles.ps1
# Descripción: Quita del seguimiento de Git los archivos grandes de ciertas extensiones
# No los borra del disco, solo los quita de Git.

# Carpeta base del repo
$repoPath = "D:\Will\Romdas\Developments\Dataview_GP\DataView2"
Set-Location $repoPath

# Extensiones de archivos grandes a limpiar
$extensions = @(
    "*.dll", "*.exe", "*.zip", "*.db", "*.onnx", "*.pb", "*.tar", "*.tgz"
)

foreach ($ext in $extensions) {
    Write-Host "Buscando archivos $ext..."
    # Buscar todos los archivos con la extensión, incluyendo subcarpetas
    $files = Get-ChildItem -Recurse -File -Include $ext
    foreach ($file in $files) {
        # Verificar si el archivo está siendo trackeado por Git
        $relativePath = $file.FullName.Substring($repoPath.Length + 1) -replace "\\", "/"
        $isTracked = git ls-files --error-unmatch $relativePath 2>$null
        if ($isTracked) {
            Write-Host "Eliminando del tracking: $relativePath"
            git rm --cached $relativePath
        }
    }
}

Write-Host "✅ Archivos grandes limpiados del seguimiento de Git."
Write-Host "Ahora haz un commit: git commit -m 'Remove large files from Git tracking'"
