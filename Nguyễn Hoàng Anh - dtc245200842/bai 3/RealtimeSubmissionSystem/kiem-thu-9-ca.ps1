$hostUrl = "http://localhost:5000"

Write-Host "=== KIEM THU 9 CA REST VA SIGNALR (RealtimeSubmissionSystem) ===" -ForegroundColor Cyan

# 1. GET /
$r1 = curl.exe -s -o /dev/null -w "%{http_code}" "$hostUrl/"
Write-Host "Ca 1: GET /                            -> $r1 (Mong doi: 200)" -ForegroundColor Green

# 2. GET /api/assignments
$r2 = curl.exe -s -o /dev/null -w "%{http_code}" "$hostUrl/api/assignments"
Write-Host "Ca 2: GET /api/assignments                 -> $r2 (Mong doi: 200)" -ForegroundColor Green

# 3. POST hop le -> 201
$postRes = curl.exe -s -i -X POST "$hostUrl/api/assignments/1/submissions" `
    -H "Content-Type: application/json" `
    -d '{\"studentCode\":\"SV001\",\"repositoryUrl\":\"https://example.com/sv001/signalr\"}'

$r3 = if ($postRes -match "HTTP/1.1 (\d+)") { $matches[1] } else { "---" }
$subId = if ($postRes -match '"id":"([^"]+)"') { $matches[1] } else { "" }
Write-Host "Ca 3: POST /api/assignments/1/submissions  -> $r3 (Mong doi: 201)" -ForegroundColor Green
Write-Host "     -> Tao luot nop ID: $subId" -ForegroundColor Gray

# 4. GET luot nop sau khi tao
$r4 = curl.exe -s -o /dev/null -w "%{http_code}" "$hostUrl/api/submissions/$subId"
Write-Host "Ca 4: GET /api/submissions/{id}            -> $r4 (Mong doi: 200)" -ForegroundColor Green

# 5. POST thieu du lieu -> 400
$r5 = curl.exe -s -o /dev/null -w "%{http_code}" -X POST "$hostUrl/api/assignments/1/submissions" `
    -H "Content-Type: application/json" -d '{\"studentCode\":\"\",\"repositoryUrl\":\"\"}'
Write-Host "Ca 5: POST thieu du lieu                   -> $r5 (Mong doi: 400)" -ForegroundColor Green

# 6. POST bai 99 -> 404
$r6 = curl.exe -s -o /dev/null -w "%{http_code}" -X POST "$hostUrl/api/assignments/99/submissions" `
    -H "Content-Type: application/json" -d '{\"studentCode\":\"SV001\",\"repositoryUrl\":\"https://example.com\"}'
Write-Host "Ca 6: POST bai 99 khong ton tai            -> $r6 (Mong doi: 404)" -ForegroundColor Green

# 7. GET id khong ton tai -> 404
$r7 = curl.exe -s -o /dev/null -w "%{http_code}" "$hostUrl/api/submissions/00000000-0000-0000-0000-000000000000"
Write-Host "Ca 7: GET id khong ton tai                 -> $r7 (Mong doi: 404)" -ForegroundColor Green

# 8. Bat tay SignalR negotiate -> 200
$r8 = curl.exe -s -o /dev/null -w "%{http_code}" -X POST "$hostUrl/hubs/submissions/negotiate?negotiateVersion=1"
Write-Host "Ca 8: POST /hubs/submissions/negotiate     -> $r8 (Mong doi: 200)" -ForegroundColor Green

# 9. Sai duong dan hub -> 404
$r9 = curl.exe -s -o /dev/null -w "%{http_code}" -X POST "$hostUrl/hub/submissions/negotiate?negotiateVersion=1"
Write-Host "Ca 9: POST /hub/submissions/negotiate (sai)-> $r9 (Mong doi: 404)" -ForegroundColor Green

Write-Host "`nHoan thanh kiem tra!" -ForegroundColor Cyan
