@echo off
chcp 1251
echo ������ ����������� Valuator...

:: ������ ������� ����������
start "Valuator-5001" /min cmd /c "dotnet run --urls http://0.0.0.0:5001"
echo �������� ������� �� ����� 5001...
:wait5001
netstat -ano | findstr :5001 >nul
if %ERRORLEVEL% neq 0 (
    timeout /t 2 >nul
    goto wait5001
)
echo ��������� �� 5001 �������.

:: ������ ������� ����������
start "Valuator-5002" /min cmd /c "dotnet run --urls http://0.0.0.0:5002"
echo �������� ������� �� ����� 5002...
:wait5002
netstat -ano | findstr :5002 >nul
if %ERRORLEVEL% neq 0 (
    timeout /t 2 >nul
    goto wait5002
)
echo ��������� �� 5002 �������.

:: ������ Nginx
start "Nginx" /min cmd /c "nginx"
echo Nginx �������.
