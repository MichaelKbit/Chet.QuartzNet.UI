@echo off
echo 开始执行git命令序列...
echo.

:: 设置当前目录为脚本所在目录
cd /d %~dp0

echo 正在执行: git add .
git add .
if errorlevel 1 (
    echo git add 失败，请检查错误信息
    pause
    exit /b 1
)
echo git add 成功

echo.
echo 正在执行: git commit -m "更新代码"
git commit -m "更新代码"
if errorlevel 1 (
    echo git commit 失败，请检查错误信息
    pause
    exit /b 1
)
echo git commit 成功

echo.
echo 正在执行: git push origin main
git push origin main
if errorlevel 1 (
    echo git push 失败，请检查错误信息
    pause
    exit /b 1
)
echo git push 成功

echo.
echo 所有git命令执行完成！
pause