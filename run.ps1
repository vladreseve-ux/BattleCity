[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Battle City — Console Arcade"
try {
    $Host.UI.RawUI.WindowSize = New-Object System.Management.Automation.Host.Size(62, 26)
    $Host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(62, 26)
} catch { }
dotnet run --project BattleCity.csproj
