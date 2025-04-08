@echo off
chcp 1251
taskkill /IM dotnet.exe /F
taskkill /IM nginx.exe /F
