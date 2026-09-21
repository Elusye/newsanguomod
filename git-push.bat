@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
cd /d "%~dp0"

rem Usage: git-push.bat ["commit message"]  (optional custom message, default below)
set "MSG=Update newsanguo source code to 0.2.29"
if not "%~1"=="" set "MSG=%~1"

rem NOTE: keep this file ASCII-only.
rem cmd.exe mis-parses a .bat that combines "chcp 65001" with multi-byte text:
rem commands lose their leading characters ("git" becomes "it") and the script dies.

rem Pin the commit identity per-repo (idempotent).
rem This must be set here, not only on the commit command below: "git pull --rebase"
rem and "git rebase --continue" create commits internally WITHOUT those -c flags.
rem With no identity git aborts with "Committer identity unknown" and leaves the repo
rem stuck in the middle of a rebase -- that is what made pushes fail repeatedly.
git config --local user.name "Elusye" >nul
git config --local user.email "87292818+Elusye@users.noreply.github.com" >nul

rem Never let git open an editor: after a conflict is resolved "git rebase --continue"
rem opens one to confirm the message, which hangs a double-clicked .bat (or fails on
rem this machine's unset notepad path) and leaves the rebase stuck again.
set "GIT_EDITOR=true"
set "GIT_SEQUENCE_EDITOR=true"

rem A previous failure may have left the repo mid-rebase / mid-merge.
rem Never run "git add -A" + commit in that state: it would commit conflict content.
if exist ".git\rebase-merge" goto :finish_rebase
if exist ".git\rebase-apply" goto :finish_rebase
if exist ".git\MERGE_HEAD" goto :finish_merge

:stage
echo === Staging all changes (.gitignore filters sensitive dirs) ===
git add -A
if errorlevel 1 goto :err

git diff --cached --quiet
if not errorlevel 1 (
    echo Nothing to commit.
    goto :pull
)

echo === Committing ===
git commit -m "%MSG%"
if errorlevel 1 goto :err

:pull
echo === Pulling latest from GitHub (rebase) ===
git pull --rebase origin main
if errorlevel 1 goto :pull_failed

:push
echo === Pushing to GitHub ===
git push
if errorlevel 1 goto :err

echo === Done ===
goto :end

rem ---------- finish a rebase left behind by a previous run ----------
:pull_failed
if exist ".git\rebase-merge" goto :finish_rebase
if exist ".git\rebase-apply" goto :finish_rebase
echo === git pull --rebase failed (not a conflict) ===
echo Common causes: no network / proxy, or the remote needs you to log in again.
goto :err

:finish_rebase
echo === Unfinished rebase detected, finishing it first ===
set "CONFLICTS=%TEMP%\newsanguo_conflicts.txt"
:check_conflicts
set "HAS_UNMERGED="
set "HAS_NON_UID="
git -c core.quotepath=false diff --name-only --diff-filter=U > "%CONFLICTS%" 2>nul
rem Any unmerged path that is NOT a .uid file means a human has to look at it.
rem (Deliberately not using findstr /e here: with /c: it does not anchor at end of line
rem  on git 2.43 / Windows and mis-classified "ApiUid.uid".)
for /f "usebackq delims=" %%f in ("%CONFLICTS%") do (
    set "HAS_UNMERGED=1"
    set "UNMERGED_PATH=%%f"
    if /i not "!UNMERGED_PATH:~-4!"==".uid" set "HAS_NON_UID=1"
)
if not defined HAS_UNMERGED goto :continue_rebase
if defined HAS_NON_UID goto :manual

echo All conflicts are Godot-generated .uid files (one uid per side, content meaningless), taking upstream copies
for /f "usebackq delims=" %%f in ("%CONFLICTS%") do (
    rem during a rebase --ours is the upstream (new base), --theirs is the local commit
    git checkout --ours -- "%%f"
    git add -- "%%f"
)

:continue_rebase
git rebase --continue
if errorlevel 1 goto :manual

rem another conflict may follow; loop
set "STILL="
git -c core.quotepath=false diff --name-only --diff-filter=U > "%CONFLICTS%" 2>nul
for /f "usebackq delims=" %%f in ("%CONFLICTS%") do set "STILL=1"
if defined STILL goto :check_conflicts

echo Rebase finished, continuing normally
goto :stage

rem ---------- needs a human ----------
:finish_merge
echo === Unfinished merge detected, please handle it manually ===
git status
goto :err

:manual
echo === Conflicts need a manual fix (non-.uid file, or rebase cannot continue) ===
git status
echo.
echo After fixing: git rebase --continue
echo To give up:    git rebase --abort   (then run this script again)
goto :err

:err
echo Failed. Check the output above.
exit /b 1

:end
exit /b 0
