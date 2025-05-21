# Wox 計時器與鬧鐘插件

此專案提供 Wox 的插件，基於 .NET Framework 4.5.2 開發，具有倒數計時與鬧鐘功能，並在桌面右下角通知使用者。使用 Serilog 進行日誌記錄，提供更好的錯誤診斷和使用情況分析。

## 技術規格

- **開發框架**: .NET Framework 4.5.2

## 功能

- **倒數計時器**:
  - 設定一個倒數計時器，時間到後顯示通知
  - 支援小時:分鐘:秒 (HH:mm:ss) 格式
  - 可自訂計時標題和描述
  - 完成時在桌面右下角顯示通知

- **鬧鐘功能**:
  - 設定特定時間點的提醒，到時會顯示通知
  - 支援小時:分鐘 (HH:mm) 格式
  - 如果設定的時間已過，將自動設定為明天同一時間
  - 可自訂鬧鐘標題和描述

- **計時管理**:
  - 即時查看所有正在運行的計時器與鬧鐘
  - 查看每個計時器/鬧鐘的剩餘時間
  - 一鍵取消所有計時器/鬧鐘
  - 單獨取消特定計時器或鬧鐘
  
- **日誌記錄**:
  - 使用 Serilog 記錄操作和錯誤
  - 日誌檔案保留 5 天
  - 支援 Console 和 File 輸出

## 使用方法

### 基本用法

輸入 `timer` 顯示使用說明和當前活動的計時器和鬧鐘。

### 倒數計時功能

使用 `HH:mm:ss` 格式設定倒數計時：

```
timer 00:05:00 喝水
```

上面的命令會設定一個 5 分鐘的倒數計時，標題為「喝水」。

### 鬧鐘功能

使用 `HH:mm` 格式設定鬧鐘：

```
timer 14:30 開會
```

上面的命令會設定一個 14:30 的鬧鐘，標題為「開會」。如果當前時間已經過了 14:30，則會設定為明天 14:30。

### 管理功能

- 列出所有計時器和鬧鐘：`timer 列表` 或 `timer list`
- 取消所有計時器和鬧鐘：`timer 取消` 或 `timer cancel`
- 取消特定計時器/鬧鐘：在列表中選擇要取消的項目並按 Enter

## 安裝方法

### 方法一：使用部署腳本（推薦）

專案包含一個自動部署腳本 `deploy.ps1`，它會自動執行以下操作：
1. 編譯專案為發布版本
2. 停止正在運行的 Wox 程序（如果存在）
3. 創建必要的插件目錄
4. 複製所有必要檔案到 Wox 插件目錄
5. 驗證安裝的完整性
6. 詢問是否重新啟動 Wox

使用方式：
```powershell
# 在專案根目錄執行
.\deploy.ps1
```

### 方法二：手動安裝

1. 下載並編譯此專案
2. 將編譯後的文件複製到 Wox 插件目錄：
   ```
   %APPDATA%\Wox\Plugins\WoxTimerPlugin\
   ```
3. 確保包含以下文件：
   - WoxTimerPlugin.dll
   - plugin.json
   - Images\timer.png
   - 相關 Serilog DLL 檔案 (若有靜態參考)
4. 重啟 Wox 或在設定中重新載入插件

## 資料儲存

- 所有計時器和鬧鐘存放在記憶體中，程式關閉後不會保留
- 日誌檔案位置：`%LocalAppData%\WoxTimerPlugin\Logs\wox-timer-plugin-YYYYMMDD.log`
- 日誌檔案保留最近 5 天記錄

## 開發與編譯指南

### 環境需求
- Visual Studio 2019+ 或 JetBrains Rider
- .NET Framework 4.5.2 SDK
- PowerShell 5.0+ (用於部署腳本)

### 目錄結構
- `Main.cs` - 主要插件邏輯和實現
- `plugin.json` - Wox 插件配置檔
- `Images/` - 包含插件圖示
- `deploy.ps1` - 自動化部署腳本

### 開發步驟
1. Clone儲存庫
   ```
   git clone https://github.com/s30122/yeh-wox-timer-plugin.git
   cd yeh-wox-timer-plugin
   ```

2. 使用 Visual Studio 或 Rider 開啟 `wox-extension.sln` 解決方案

3. 還原 NuGet 套件
   ```
   dotnet restore
   ```

4. 編譯專案
   ```
   dotnet build
   ```

5. 使用部署腳本進行測試部署
   ```powershell
   .\deploy.ps1
   ```


## 文件參考
- [Wox Plugin API](http://doc.wox.one/zh/plugin/csharp_plugin.html)

