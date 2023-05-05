set -e

dotnet run --configuration Release --project src

git add .
git commit -am "Updated"
git push