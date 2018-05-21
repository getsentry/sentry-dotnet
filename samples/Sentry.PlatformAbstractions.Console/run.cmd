@echo off

echo Running any already built Console sample:

setlocal enabledelayedexpansion
for /f %%a in ('dir /s /b *Console.exe *Console.dll ^| find /v "obj" ^| find /v "Debug"') do (
	set sample=%%a
	echo.
	echo Running: !sample!
	if "!sample:~-3!" == "exe" (
		!sample!
	) else (
		dotnet !sample!
	)
)
endlocal
