# Fitness Tracker API (Minimal API, .NET 8)

Starter scaffold for your team: Minimal API, xUnit tests, GitHub Actions CI, and Docker.

## Run locally
```bash
dotnet build
dotnet run --project src/FitnessTracker.Api
# open: https://localhost:5001/swagger (dev) or http://localhost:8080 when Dockerized
```

## Run tests
```bash
dotnet test
```

## Docker
```bash
docker compose up --build
# API -> http://localhost:8080
```

## Suggested branch model
- `main`: production
- `develop`: integration branch
- `feature/*`: short‑lived feature branches → PR into `develop`

## First push
```bash
git init
git add .
git commit -m "feat: bootstrap minimal api, tests, ci, docker"
git branch -M main
git remote add origin https://github.com/<you>/fitness-tracker.git
git push -u origin main
git checkout -b develop
git push -u origin develop
```
