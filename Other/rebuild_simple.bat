@echo off
dotnet clean
dotnet restore
dotnet build
dotnet run
pause

