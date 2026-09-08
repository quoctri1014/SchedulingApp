$root = 'D:\SchedulingApp'
$inputPath = Join-Path $root 'BAO_CAO_NGUYEN_THANH_HIEU.md'
$outputPath = Join-Path $root 'BAO_CAO_NGUYEN_THANH_HIEU.doc'
$text = Get-Content -LiteralPath $inputPath -Raw -Encoding UTF8
$text = [regex]::Replace($text, '<img src="([^"]+)"[^>]*>', {
    param($match)
    $relative = $match.Groups[1].Value.Replace('/', '\')
    $imagePath = Join-Path $root $relative
    if (-not (Test-Path $imagePath)) { return '' }
    $bytes = [IO.File]::ReadAllBytes($imagePath)
    $base64 = [Convert]::ToBase64String($bytes)
    return "<img src=""data:image/svg+xml;base64,$base64"" style=""max-width:900px;display:block;margin:18px auto"">"
})
$text = [Net.WebUtility]::HtmlEncode($text)
$text = $text -replace '&lt;img src=&quot;data:image/svg\\+xml;base64,([^&]+)&quot;[^&]*&gt;', '<img src="data:image/svg+xml;base64,$1" style="max-width:900px;display:block;margin:18px auto">'
$text = $text -replace '(?m)^#{1}\s+(.+)$', '<h1>$1</h1>'
$text = $text -replace '(?m)^#{2}\s+(.+)$', '<h2>$1</h2>'
$text = $text -replace '(?m)^#{3}\s+(.+)$', '<h3>$1</h3>'
$text = $text -replace '(?m)^---+$', '<hr>'
$text = $text -replace '(?m)^\s*$', '<br>'
$text = $text -replace "`r?`n", '<br>'
$html = @"
<html><head><meta charset="utf-8"><style>body{font-family:Calibri,Arial,sans-serif;font-size:11pt;line-height:1.45;margin:36px;color:#172033}h1{color:#123a63;border-bottom:2px solid #2563eb;padding-bottom:8px}h2{color:#1e4d78;margin-top:26px}h3{color:#315f82;margin-top:20px}hr{border:0;border-top:1px solid #cbd5e1;margin:20px 0}code{font-family:Consolas,monospace;background:#eef2f7}</style></head><body>$text</body></html>
"@
[IO.File]::WriteAllText($outputPath, $html, [Text.UTF8Encoding]::new($false))
Write-Output "Created $outputPath"
