param(
    [string]$DbPath = "D:\GAME\MapleStory228\Data\mapleitemdb.db"
)

$ErrorActionPreference = "Stop"
$today = Get-Date -Format "MMdd"
$distDir = "dist"
$publishDir = "src/MapleItemDB.UI/bin/publish"
$intermediateDir = "src/MapleItemDB.UI/bin/Release/net8.0-windows/win-x64"

# 确定当天版本号
$version = 1
$existingZips = @()
if (Test-Path $distDir) {
    $existingZips += Get-ChildItem $distDir -Filter "*.zip"
}
$existingZips += Get-ChildItem "." -Filter "*$today*.zip"
if ($existingZips) {
    $maxVer = ($existingZips | ForEach-Object {
        if ($_.BaseName -match "v(\d+)$") { [int]$Matches[1] } else { 0 }
    } | Measure-Object -Maximum).Maximum
    $version = $maxVer + 1
}
$zipName = "MSEA代码查询器-$today-v$version.zip"

Write-Host "=== 1/4 发布项目 ===" -ForegroundColor Cyan
dotnet publish src/MapleItemDB.UI/MapleItemDB.UI.csproj `
    -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true `
    --no-self-contained `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "发布失败" }

Write-Host "=== 2/4 创建 dist 目录 ===" -ForegroundColor Cyan
if (-not (Test-Path $intermediateDir/e_sqlite3.dll)) {
    dotnet build src/MapleItemDB.UI/MapleItemDB.UI.csproj -c Release -r win-x64 --no-restore | Out-Null
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Write-Host "=== 3/4 复制文件 ===" -ForegroundColor Cyan
Copy-Item "$publishDir/MapleItemDB.UI.exe" $distDir -Force
Copy-Item "$intermediateDir/e_sqlite3.dll" $distDir -Force
Copy-Item $DbPath $distDir -Force

Write-Host "=== 4/4 打包 $zipName ===" -ForegroundColor Cyan
$compress = @{
    Path             = @("$distDir/MapleItemDB.UI.exe", "$distDir/e_sqlite3.dll", "$distDir/mapleitemdb.db")
    DestinationPath  = $zipName
    Force            = $true
}
Compress-Archive @compress

# 删除 dist 目录
Remove-Item $distDir -Recurse -Force

# 保留最新 3 个 zip，删除更旧的
$allZips = Get-ChildItem "." -Filter "MSEA代码查询器-*.zip" | Sort-Object CreationTime -Descending
if ($allZips.Count -gt 3) {
    $allZips | Select-Object -Skip 3 | ForEach-Object {
        Write-Host "  删除旧包: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
    }
}

Write-Host "完成: $zipName" -ForegroundColor Green
