@echo off
chcp 65001 >nul
echo ========================================
echo Диагностика и исправление проблем
echo ========================================
echo.

echo [1/6] Очистка проекта...
dotnet clean
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo Очистка завершена!
echo.

echo [2/6] Проверка .NET SDK...
dotnet --version
if %errorlevel% neq 0 (
    echo ОШИБКА: .NET SDK не установлен!
    pause
    exit /b 1
)
echo .NET SDK найден!
echo.

echo [3/6] Восстановление пакетов...
dotnet restore
if %errorlevel% neq 0 (
    echo ОШИБКА при восстановлении пакетов!
    pause
    exit /b 1
)
echo Пакеты восстановлены!
echo.

echo [4/6] Проверка компиляции...
dotnet build --no-incremental
if %errorlevel% neq 0 (
    echo ОШИБКА при компиляции!
    echo Проверьте ошибки выше.
    pause
    exit /b 1
)
echo Компиляция успешна!
echo.

echo [5/6] Проверка файлов...
if not exist "Views\Images\Icon.ico" (
    echo ПРЕДУПРЕЖДЕНИЕ: Views\Images\Icon.ico не найден!
)
if not exist "Services\DatabaseService.cs" (
    echo ОШИБКА: Services\DatabaseService.cs не найден!
    pause
    exit /b 1
)
echo Файлы проверены!
echo.

echo [6/6] Готово к запуску!
echo ========================================
echo.
echo Для запуска выполните: dotnet run
echo Или нажмите любую клавишу для запуска сейчас...
pause >nul

echo.
echo Запуск приложения...
dotnet run

pause

