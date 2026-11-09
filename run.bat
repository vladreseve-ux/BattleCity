@echo off
chcp 65001 > nul
title Battle City — Console Arcade
mode con: cols=62 lines=26
dotnet run --project BattleCity.csproj
if errorlevel 1 (
    echo.
    echo Ошибка при запуске игры. Проверьте установку .NET 10 SDK.
    pause
)
