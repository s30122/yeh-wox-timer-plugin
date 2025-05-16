# 設定變數
$projectName = "WoxTimerPlugin"
$projectPath = $PSScriptRoot
$buildConfiguration = "Release"
$framework = "net452"

# Wox 插件目錄
$woxPluginDir = "$env:APPDATA\Wox\Plugins\$projectName"

# 檢查並停止 Wox 程序
Write-Host "檢查 Wox 程序..." -ForegroundColor Yellow
$woxProcess = Get-Process "Wox" -ErrorAction SilentlyContinue
if ($woxProcess) {
    Write-Host "正在停止 Wox..." -ForegroundColor Yellow
    $woxProcess | Stop-Process -Force
    Start-Sleep -Seconds 2 # 等待進程完全停止
}

Write-Host "開始部署 $projectName..." -ForegroundColor Green

# 1. 建置專案
Write-Host "正在建置專案..." -ForegroundColor Yellow
dotnet build "$projectPath\wox-extension.csproj" -c $buildConfiguration

if ($LASTEXITCODE -ne 0) {
    Write-Host "建置失敗！" -ForegroundColor Red
    exit 1
}

# 2. 創建插件目錄（如果不存在）
Write-Host "正在準備插件目錄..." -ForegroundColor Yellow
if (-not (Test-Path $woxPluginDir)) {
    New-Item -ItemType Directory -Path $woxPluginDir -Force
}

# 3. 複製必要檔案
Write-Host "正在複製檔案..." -ForegroundColor Yellow

# 複製 DLL 和相依檔案
$buildOutput = "$projectPath\bin\$buildConfiguration\$framework"
Copy-Item "$buildOutput\*" -Destination $woxPluginDir -Recurse -Force

# 確保 Images 目錄存在
$imagesDir = "$woxPluginDir\Images"
if (-not (Test-Path $imagesDir)) {
    New-Item -ItemType Directory -Path $imagesDir -Force
}

# 複製圖片
Copy-Item "$projectPath\Images\timer.png" -Destination "$imagesDir\timer.png" -Force

# 複製 plugin.json
Copy-Item "$projectPath\plugin.json" -Destination $woxPluginDir -Force

# 4. 驗證安裝
Write-Host "正在驗證安裝..." -ForegroundColor Yellow
$requiredFiles = @(
    "$woxPluginDir\WoxTimerPlugin.dll",
    "$woxPluginDir\plugin.json",
    "$woxPluginDir\Images\timer.png"
)

$missingFiles = $false
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "缺少必要檔案: $file" -ForegroundColor Red
        $missingFiles = $true
    }
}

if ($missingFiles) {
    Write-Host "部署失敗：缺少必要檔案！" -ForegroundColor Red
    exit 1
}

Write-Host "部署完成！" -ForegroundColor Green
Write-Host "插件已安裝到: $woxPluginDir" -ForegroundColor Green

# 詢問是否要重新啟動 Wox
$woxPath = "$env:LOCALAPPDATA\Wox\Wox.exe"
$restart = Read-Host "是否要重新啟動 Wox？(Y/N)"
if ($restart -eq "Y" -or $restart -eq "y") {
    Write-Host "正在啟動 Wox..." -ForegroundColor Yellow
    if (Test-Path $woxPath) {
        Start-Process $woxPath
    } else {
        Write-Host "找不到 Wox 執行檔：$woxPath" -ForegroundColor Red
        Write-Host "請手動重新啟動 Wox。" -ForegroundColor Yellow
    }
} else {
    Write-Host "請手動重新啟動 Wox 或在設定中重新載入插件。" -ForegroundColor Yellow
}
