dotnet run --configuration Release --project src

if %errorlevel% neq 0 exit /b %errorlevel%

git add .
git commit -am "Updated"
git push