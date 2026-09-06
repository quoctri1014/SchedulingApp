param(
    [string]$ApiBaseUrl = 'http://localhost:5000/api/v1',
    [int]$RunsPerConfiguration = 30,
    [string]$OutputDirectory = "$PSScriptRoot/results"
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$employees = (Invoke-RestMethod "$ApiBaseUrl/employees").data
$shifts = (Invoke-RestMethod "$ApiBaseUrl/shifts").data

if ($employees.Count -lt 200 -or $shifts.Count -lt 200) {
    throw "Dataset must contain at least 200 employees and 200 shifts. Found employees=$($employees.Count), shifts=$($shifts.Count)."
}

$datasets = @(
    @{ Name = 'small'; ShiftCount = 50; EmployeeCount = 50 },
    @{ Name = 'medium'; ShiftCount = 100; EmployeeCount = 100 },
    @{ Name = 'large'; ShiftCount = 200; EmployeeCount = 200 }
)

$configurations = @(
    @{ Name = 'baseline'; InitialTemperature = 1000; CoolingRate = 0.95; MaxIterations = 1000 },
    @{ Name = 'low-temperature'; InitialTemperature = 100; CoolingRate = 0.95; MaxIterations = 1000 },
    @{ Name = 'fast-cooling'; InitialTemperature = 1000; CoolingRate = 0.80; MaxIterations = 1000 },
    @{ Name = 'short-search'; InitialTemperature = 1000; CoolingRate = 0.95; MaxIterations = 100 }
)

$rawResults = [System.Collections.Generic.List[object]]::new()
$totalRuns = $datasets.Count * $configurations.Count * $RunsPerConfiguration
$runNumber = 0

foreach ($dataset in $datasets) {
    $shiftIds = @($shifts | Select-Object -First $dataset.ShiftCount | ForEach-Object { [int]$_.id })
    $employeeIds = @($employees | Select-Object -First $dataset.EmployeeCount | ForEach-Object { [string]$_.id })

    foreach ($configuration in $configurations) {
        for ($replicate = 1; $replicate -le $RunsPerConfiguration; $replicate++) {
            $runNumber++
            $body = @{
                shiftIds = $shiftIds
                employeeIds = $employeeIds
                fromDate = (Get-Date).ToString('o')
                toDate = (Get-Date).ToString('o')
                options = @{
                    initialTemperature = $configuration.InitialTemperature
                    coolingRate = $configuration.CoolingRate
                    maxIterations = $configuration.MaxIterations
                }
            } | ConvertTo-Json -Depth 5

            $response = Invoke-RestMethod "$ApiBaseUrl/schedule/run?algorithm=sa" -Method Post -ContentType 'application/json' -Body $body
            $result = $response.data

            $rawResults.Add([pscustomobject]@{
                Dataset = $dataset.Name
                Configuration = $configuration.Name
                Replicate = $replicate
                InitialTemperature = $configuration.InitialTemperature
                CoolingRate = $configuration.CoolingRate
                MaxIterations = $configuration.MaxIterations
                TotalShifts = $result.totalShifts
                FilledShifts = $result.filledShifts
                FilledPercentage = if ($result.totalShifts -gt 0) { [math]::Round(($result.filledShifts / $result.totalShifts) * 100, 2) } else { 0 }
                ExecutionTimeMs = $result.executionTimeMs
                TotalPenaltyScore = $result.totalPenaltyScore
            })

            Write-Progress -Activity 'Running SA ablation study' -Status "$runNumber / $totalRuns" -PercentComplete (($runNumber / $totalRuns) * 100)
        }
    }
}

$rawPath = Join-Path $OutputDirectory 'sa-ablation-raw.csv'
$summaryPath = Join-Path $OutputDirectory 'sa-ablation-summary.csv'
$svgPath = Join-Path $OutputDirectory 'sa-ablation-penalty.svg'

$rawResults | Export-Csv -Path $rawPath -NoTypeInformation -Encoding UTF8

$summary = $rawResults |
    Group-Object Dataset, Configuration |
    ForEach-Object {
        $rows = $_.Group
        [pscustomobject]@{
            Dataset = $rows[0].Dataset
            Configuration = $rows[0].Configuration
            Replicates = $rows.Count
            AvgExecutionTimeMs = [math]::Round(($rows | Measure-Object ExecutionTimeMs -Average).Average, 2)
            AvgPenaltyScore = [math]::Round(($rows | Measure-Object TotalPenaltyScore -Average).Average, 2)
            AvgFilledPercentage = [math]::Round(($rows | Measure-Object FilledPercentage -Average).Average, 2)
            BestPenaltyScore = [math]::Round(($rows | Measure-Object TotalPenaltyScore -Minimum).Minimum, 2)
            WorstPenaltyScore = [math]::Round(($rows | Measure-Object TotalPenaltyScore -Maximum).Maximum, 2)
        }
    } | Sort-Object Dataset, Configuration

$summary | Export-Csv -Path $summaryPath -NoTypeInformation -Encoding UTF8

$colors = @('#64748b', '#0f766e', '#d97706', '#7c3aed')
$width = 1200
$height = 620
$chartTop = 90
$chartBottom = 500
$maxPenalty = [math]::Max(1, ($summary | Measure-Object AvgPenaltyScore -Maximum).Maximum)
$barWidth = 48
$gap = 18
$svg = [System.Text.StringBuilder]::new()
[void]$svg.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' width='$width' height='$height' viewBox='0 0 $width $height'>")
[void]$svg.AppendLine("<rect width='100%' height='100%' fill='#f8fafc'/>")
[void]$svg.AppendLine("<text x='40' y='42' font-family='Segoe UI, sans-serif' font-size='24' font-weight='700' fill='#0f172a'>SA Ablation Study - Average Penalty</text>")
[void]$svg.AppendLine("<text x='40' y='68' font-family='Segoe UI, sans-serif' font-size='13' fill='#64748b'>Lower is better. Each bar is the mean of $RunsPerConfiguration replicates.</text>")

$datasetIndex = 0
foreach ($dataset in $datasets) {
    $datasetRows = @($summary | Where-Object Dataset -eq $dataset.Name)
    $groupStart = 80 + ($datasetIndex * 370)
    [void]$svg.AppendLine("<text x='$groupStart' y='535' font-family='Segoe UI, sans-serif' font-size='14' font-weight='700' fill='#334155'>$($dataset.Name.ToUpper())</text>")
    for ($configIndex = 0; $configIndex -lt $datasetRows.Count; $configIndex++) {
        $row = $datasetRows[$configIndex]
        $x = $groupStart + ($configIndex * ($barWidth + $gap))
        $barHeight = (($row.AvgPenaltyScore / $maxPenalty) * ($chartBottom - $chartTop))
        $y = $chartBottom - $barHeight
        $color = $colors[$configIndex % $colors.Count]
        [void]$svg.AppendLine("<rect x='$x' y='$y' width='$barWidth' height='$barHeight' rx='5' fill='$color'/>")
        [void]$svg.AppendLine("<text x='$($x + ($barWidth / 2))' y='$($y - 8)' text-anchor='middle' font-family='Segoe UI, sans-serif' font-size='11' fill='#334155'>$($row.AvgPenaltyScore)</text>")
        [void]$svg.AppendLine("<text x='$($x + ($barWidth / 2))' y='520' text-anchor='middle' font-family='Segoe UI, sans-serif' font-size='10' fill='#64748b'>$($row.Configuration)</text>")
    }
    $datasetIndex++
}

[void]$svg.AppendLine("<line x1='40' y1='$chartBottom' x2='1150' y2='$chartBottom' stroke='#cbd5e1'/>")
[void]$svg.AppendLine("<text x='40' y='585' font-family='Segoe UI, sans-serif' font-size='12' fill='#64748b'>Dataset size</text>")
[void]$svg.AppendLine('</svg>')
[System.IO.File]::WriteAllText($svgPath, $svg.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Progress -Activity 'Running SA ablation study' -Completed
Write-Output "Raw results: $rawPath"
Write-Output "Summary: $summaryPath"
Write-Output "Chart: $svgPath"