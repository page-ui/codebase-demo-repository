param(
    [int]$TimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

docker compose up --build -d

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    $services = docker compose config --services
    $pending = @()

    foreach ($service in $services) {
        $containerId = docker compose ps -q $service
        if ([string]::IsNullOrWhiteSpace($containerId)) {
            $pending += "${service}:not-created"
            continue
        }

        $state = docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}no-health{{end}}|{{.State.ExitCode}}' $containerId
        $parts = $state -split '\|'
        $status = $parts[0]
        $health = $parts[1]
        $exitCode = [int]$parts[2]

        if ($status -eq "exited" -and $exitCode -eq 0) {
            continue
        }

        if ($status -ne "running") {
            $pending += "${service}:${status}"
            continue
        }

        if ($health -ne "no-health" -and $health -ne "healthy") {
            $pending += "${service}:${health}"
        }
    }

    if ($pending.Count -eq 0) {
        docker compose ps
        Write-Host "All Page UI services are up and healthy."
        exit 0
    }

    Write-Host ("Waiting for services: " + ($pending -join ", "))
    Start-Sleep -Seconds 5
}

docker compose ps
throw "Timed out after $TimeoutSeconds seconds waiting for Page UI services to become healthy."
