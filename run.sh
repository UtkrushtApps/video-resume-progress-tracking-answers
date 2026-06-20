#!/usr/bin/env bash
set -euo pipefail

BASE_DIR="/root/task"
cd "$BASE_DIR"

echo "Starting SQL Server container..."
docker compose up -d

echo "Waiting for SQL Server to become healthy..."
for i in $(seq 1 30); do
  STATUS=$(docker inspect -f '{{.State.Health.Status}}' streaming_sqlserver 2>/dev/null || echo "starting")
  if [ "$STATUS" = "healthy" ]; then
    echo "SQL Server is healthy."
    break
  fi
  echo "  ... still waiting ($i): $STATUS"
  sleep 5
done

echo "Building solution..."
dotnet build "$BASE_DIR/StreamingProgress.sln" -c Debug --nologo

echo "Running tests..."
dotnet test "$BASE_DIR/tests/StreamingProgress.Tests/StreamingProgress.Tests.csproj" -c Debug --nologo

