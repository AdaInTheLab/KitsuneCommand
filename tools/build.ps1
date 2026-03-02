param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./dist"
)

$ErrorActionPreference = "Stop"
$modName = "KitsuneCommand"
$modDir = "$OutputDir/$modName"

Write-Host "=== KitsuneCommand Build Script ===" -ForegroundColor Cyan

# Clean
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $modDir | Out-Null

# Build frontend
Write-Host "`n--- Building Frontend ---" -ForegroundColor Yellow
Push-Location frontend
npm ci
npm run build
Pop-Location

# Build backend
Write-Host "`n--- Building Backend ---" -ForegroundColor Yellow
dotnet build "src/$modName/$modName.csproj" -c $Configuration

# Copy mod files
Write-Host "`n--- Packaging Mod ---" -ForegroundColor Yellow
$binDir = "src/$modName/bin/$Configuration"
Copy-Item "$binDir/*.dll" $modDir
Copy-Item "src/$modName/ModInfo.xml" $modDir

# Copy SQLite.Interop.dll (native component for System.Data.SQLite)
# Must be in a subfolder so the game's mod loader doesn't try to load it as a managed assembly
$interopSrc = "$binDir/x64/SQLite.Interop.dll"
if (Test-Path $interopSrc) {
    $nativeDir = "$modDir/x64"
    New-Item -ItemType Directory -Path $nativeDir -Force | Out-Null
    Copy-Item $interopSrc $nativeDir
    Write-Host "  Copied SQLite.Interop.dll to x64/" -ForegroundColor Gray
} else {
    Write-Warning "SQLite.Interop.dll not found at $interopSrc — SQLite may fail at runtime!"
}

# Copy System.ComponentModel.DataAnnotations (required by System.Web.Http, not in Unity/Mono)
$dataAnnotations = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.ComponentModel.DataAnnotations.dll'
if (Test-Path $dataAnnotations) {
    Copy-Item $dataAnnotations $modDir
    Write-Host "  Copied System.ComponentModel.DataAnnotations.dll" -ForegroundColor Gray
} else {
    Write-Warning "System.ComponentModel.DataAnnotations.dll not found — Web API may fail at runtime!"
}

# Copy config
if (Test-Path "src/$modName/Config") {
    Copy-Item -Recurse "src/$modName/Config" "$modDir/Config"
}

# Copy wwwroot
if (Test-Path "src/$modName/wwwroot") {
    Copy-Item -Recurse "src/$modName/wwwroot" "$modDir/wwwroot"
}

# Create Plugins directory
New-Item -ItemType Directory -Path "$modDir/Plugins" -Force | Out-Null

Write-Host "`n=== Build Complete ===" -ForegroundColor Green
Write-Host "Mod packaged to: $modDir" -ForegroundColor Green
Write-Host "Copy the '$modDir' folder to your 7D2D server's Mods/ directory." -ForegroundColor Gray
