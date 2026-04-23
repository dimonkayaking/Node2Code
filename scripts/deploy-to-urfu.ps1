param(
  [string]$ServerIp = "10.40.241.59",
  [string]$ServerUser = "root",
  [string]$ServerPath = "/opt/node2code",
  [string]$ProjectDirName = "CustomGameEngineModule"
)

$ErrorActionPreference = "Stop"

function Assert-LastExitCode {
  param([string]$Step)
  if ($LASTEXITCODE -ne 0) {
    throw "$Step failed with exit code $LASTEXITCODE"
  }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$archiveName = "node2code-deploy.tar.gz"
$archivePath = Join-Path $projectRoot $archiveName
$remoteArchivePath = "$ServerPath/$archiveName"

Write-Host "==> Проверяю наличие ssh/scp..."
if (-not (Get-Command ssh -ErrorAction SilentlyContinue)) {
  throw "ssh не найден в PATH"
}
if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
  throw "scp не найден в PATH"
}

Write-Host "==> Собираю архив проекта (без node_modules/.git/dist)..."
Push-Location $projectRoot
try {
  if (Test-Path $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
  }

  tar -czf $archivePath `
    --exclude=".git" `
    --exclude="**/node_modules" `
    --exclude="**/dist" `
    --exclude="$archiveName" `
    .
  Assert-LastExitCode "Archive creation"
}
finally {
  Pop-Location
}

Write-Host "==> Создаю папку на сервере..."
ssh "$ServerUser@$ServerIp" "mkdir -p $ServerPath"
Assert-LastExitCode "Remote directory creation"

Write-Host "==> Передаю архив на сервер..."
scp $archivePath "${ServerUser}@${ServerIp}:${remoteArchivePath}"
Assert-LastExitCode "Archive upload"

$remoteCmd = @"
set -e
mkdir -p $ServerPath
cd $ServerPath
rm -rf $ProjectDirName
mkdir -p $ProjectDirName
tar -xzf $archiveName -C $ProjectDirName
cd $ProjectDirName
docker compose up -d --build
docker compose ps
"@

Write-Host "==> Запускаю деплой на сервере..."
ssh "$ServerUser@$ServerIp" $remoteCmd
Assert-LastExitCode "Remote deploy"

Write-Host "==> Deploy completed. API check:"
Write-Host "ssh $ServerUser@$ServerIp `"curl -s http://localhost/ping`""
