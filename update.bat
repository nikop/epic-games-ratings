updater\epic-ratings-test.exe %*

if %errorlevel% neq 0 exit /b %errorlevel%

git add .
git commit -am "Updated"
git push

pause