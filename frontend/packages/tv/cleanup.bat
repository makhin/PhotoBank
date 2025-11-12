@echo off
echo Stopping all processes...
taskkill /F /IM java.exe /T 2>nul
taskkill /F /IM javaw.exe /T 2>nul
taskkill /F /IM node.exe /T 2>nul
taskkill /F /IM adb.exe /T 2>nul

echo Waiting...
timeout /t 5 /nobreak

echo Cleaning Gradle cache...
rmdir /s /q "%USERPROFILE%\.gradle\caches\8.14.3" 2>nul

echo Cleaning project...
rmdir /s /q "android\app\build" 2>nul
rmdir /s /q "android\build" 2>nul
rmdir /s /q "android\.gradle" 2>nul

echo Done! Now you can run: pnpm android
pause