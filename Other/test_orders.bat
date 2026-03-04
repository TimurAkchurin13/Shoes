@echo off
chcp 65001 >nul
echo Checking orders display...
echo.
echo 1. Make sure PostgreSQL is running
echo 2. Check if orders exist in database:
echo    SELECT COUNT(*) FROM orders;
echo.
echo 3. Run the application and check Debug Output
echo.
pause

