[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Battle City — Console Arcade"
try {
    $Host.UI.RawUI.WindowSize = New-Object System.Management.Automation.Host.Size(76, 28)
    $Host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(76, 28)
} catch { }
dotnet run --project BattleCity.csproj
