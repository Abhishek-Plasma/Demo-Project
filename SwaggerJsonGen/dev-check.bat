@echo off
setlocal

REM ===== CONFIG =====
set ROOT_DIR=%~dp0
set PROJECT_DLL=%ROOT_DIR%bin\Debug\net8.0\SwaggerJsonGen.dll
set BASELINE_SWAGGER=%ROOT_DIR%swagger.json
set GENERATED_SWAGGER=%ROOT_DIR%generated-swagger.json
set RULESET=%~dp0spectral.yaml

echo =========================
echo BUILD (DEBUG)
echo =========================
dotnet build || exit /b 1

echo =========================
echo GENERATE SWAGGER
echo =========================
dotnet tool run swagger tofile --output %GENERATED_SWAGGER% %PROJECT_DLL% v1 || exit /b 1

echo =========================
echo LINT SWAGGER
echo =========================
npx @stoplight/spectral-cli lint %GENERATED_SWAGGER% -r %RULESET% || exit /b 1

echo =========================
echo BREAKING CHANGE CHECK
echo =========================
"C:\Program Files\oasdiff\oasdiff.exe" breaking %BASELINE_SWAGGER% %GENERATED_SWAGGER% || exit /b 1

echo =========================
echo COPY NEW SWAGGER
echo =========================
copy /Y %GENERATED_SWAGGER% %BASELINE_SWAGGER%

echo =========================
echo DONE - READY TO COMMIT
echo =========================

endlocal
