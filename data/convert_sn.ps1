# 读取 sn.txt 并转换为 JSON 格式
$content = Get-Content -Path "$PSScriptRoot\sn.txt" -Raw
$results = [System.Collections.Generic.List[object]]::new()

$currentSN = $null
$currentItemId = $null

foreach ($line in $content -split "`r?`n") {
    $line = $line.Trim()
    if ($line -match '^SN\s*=\s*(\d+)') {
        $currentSN = [long]$Matches[1]
    }
    elseif ($line -match '^ItemId\s*=\s*(\d+)') {
        $currentItemId = [long]$Matches[1]
    }
    elseif ($line -eq '}') {
        if ($null -ne $currentSN -and $null -ne $currentItemId) {
            $results.Add([PSCustomObject]@{
                itemId = $currentItemId
                SN     = $currentSN
            })
        }
        $currentSN = $null
        $currentItemId = $null
    }
}

$json = $results | ConvertTo-Json -Depth 2
$json | Set-Content -Path "$PSScriptRoot\sn_map.json" -Encoding UTF8

Write-Host "完成，共转换 $($results.Count) 条记录 -> sn_map.json"
