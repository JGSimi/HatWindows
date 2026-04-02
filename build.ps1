# Hat Windows - Build Script
# Run this on Windows with PowerShell:
#   .\build.ps1
#
# Requirements:
#   - .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
#   - Inno Setup 6 (optional, for installer: https://jrsoftware.org/isinfo.php)

param(
    [switch]$Installer,  # Also build installer with Inno Setup
    [switch]$Portable    # Create portable ZIP
)

$ErrorActionPreference = "Stop"
$ProjectPath = "src\Hat\Hat.csproj"
$PublishDir = "publish"
$DistDir = "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hat Windows - Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean
Write-Host "[1/4] Limpando build anterior..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Restore
Write-Host "[2/4] Restaurando dependencias..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) { throw "Restore falhou" }

# Publish
Write-Host "[3/4] Compilando e publicando..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $PublishDir

if ($LASTEXITCODE -ne 0) { throw "Publish falhou" }

Write-Host "[4/4] Build completo!" -ForegroundColor Green
Write-Host ""
Write-Host "  Executavel: $PublishDir\Hat.exe" -ForegroundColor White

# Portable ZIP
if ($Portable) {
    Write-Host ""
    Write-Host "Criando ZIP portatil..." -ForegroundColor Yellow
    Compress-Archive -Path "$PublishDir\*" -DestinationPath "$DistDir\Hat-Portable.zip"
    Write-Host "  ZIP: $DistDir\Hat-Portable.zip" -ForegroundColor Green
}

# Installer
if ($Installer) {
    Write-Host ""
    Write-Host "Criando instalador com Inno Setup..." -ForegroundColor Yellow

    $InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $InnoPath)) {
        Write-Host "  ERRO: Inno Setup 6 nao encontrado em $InnoPath" -ForegroundColor Red
        Write-Host "  Baixe em: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    } else {
        & $InnoPath "installer\hat-installer.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Instalador: $DistDir\Hat-Setup-1.0.0.exe" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Pronto!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
