@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

rem Usage: git-push.bat ["commit message"]  (optional custom message, default below)
set "MSG=Update newsanguo source code to 0.2.3"
if not "%~1"=="" set "MSG=%~1"

echo === Staging all changes (.gitignore filters sensitive dirs) ===
git add -A
if errorlevel 1 goto :err

git diff --cached --quiet
if not errorlevel 1 (
    echo Nothing to commit.
    goto :end
)

echo === Committing ===
git -c user.name="Elusye" -c user.email="87292818+Elusye@users.noreply.github.com" commit -m "%MSG%"
if errorlevel 1 goto :err

echo === Pushing to GitHub ===
git push
if errorlevel 1 goto :err

echo === Done ===
goto :end

:err
echo Failed. Check the output above.
exit /b 1

:end
exit /b 0
