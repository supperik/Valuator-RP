@echo off
chcp 1251
echo Запуск экземпляров Valuator...

:: Запуск первого экземпляра

start "Valuator-5001" /min cmd /k "dotnet run --urls http://0.0.0.0:5001"
echo Ожидание запуска на порту 5001...
:wait5001
netstat -ano | findstr :5001 >nul
if %ERRORLEVEL% neq 0 (
    timeout /t 2 >nul
    goto wait5001
)
echo Экземпляр на 5001 запущен.

:: Запуск второго экземпляра
start "Valuator-5002" /min cmd /k "dotnet run --urls http://0.0.0.0:5002"
echo Ожидание запуска на порту 5002...
:wait5002
netstat -ano | findstr :5002 >nul
if %ERRORLEVEL% neq 0 (
    timeout /t 2 >nul
    goto wait5002
)
echo Экземпляр на 5002 запущен.

:: Запуск RankCalculator
REM Переход в нужную директорию для запуска RankCalculator
cd /d "E:\RP\RankCalculator"
start "RankCalculator" /min cmd /k "dotnet run"
echo RankCalculator запущен.

:: Запуск RankCalculator
REM Переход в нужную директорию для запуска RankCalculator
cd /d "E:\RP\RankCalculator"
start "RankCalculator" /min cmd /k "dotnet run"
echo RankCalculator запущен.

:: Запуск EventLogger
REM Переход в нужную директорию для запуска EventLogger
cd /d "E:\RP\EventsLogger"
start "EventsLogger" /min cmd /c "dotnet run"
echo EventsLogger запущен.

:: Запуск EventLogger
REM Переход в нужную директорию для запуска EventLogger
cd /d "E:\RP\EventsLogger"
start "EventsLogger" /min cmd /ck "dotnet run"
echo EventsLogger запущен.

:: Запуск Nginx
REM Переход в нужную директорию для запуска nginx
cd /d "E:\RP\Valuator"
start "Nginx" /min cmd /c "nginx"
echo Nginx запущен.
