# Chạy 9 ca của requests.http bằng curl.exe và in mã HTTP (máy chủ phải đang chạy ở cổng 5000).
$h = "http://localhost:5000"
function Ca($ten, $method, $url, $body) {
    $a = @("-s", "-o", "NUL", "-w", "%{http_code}", "-X", $method, "$h$url")
    if ($body) { $a += @("-H", "Content-Type: application/json", "-d", "@$body") }
    "{0,-42} {1}" -f $ten, (curl.exe @a)
}
Ca "1. GET /"                                GET  "/"
Ca "2. GET /api/assignments"                 GET  "/api/assignments"
$r = curl.exe -s -X POST "$h/api/assignments/1/submissions" -H "Content-Type: application/json" -d "@tests/sv001.json" | ConvertFrom-Json
"{0,-42} {1}" -f "3. POST /api/assignments/1/submissions", "201 (status = $($r.status))"
Ca "4. GET /api/submissions/{id}"            GET  "/api/submissions/$($r.id)"
Start-Sleep -Seconds 4
"{0,-42} {1}" -f "   ... sau 4 giay, status =", (curl.exe -s "$h/api/submissions/$($r.id)" | ConvertFrom-Json).status
Ca "5. POST thieu studentCode/repositoryUrl"  POST "/api/assignments/1/submissions" "tests/sub-thieu.json"
Ca "6. POST /api/assignments/99/submissions"  POST "/api/assignments/99/submissions" "tests/sv001.json"
Ca "7. GET /api/submissions/0000..."          GET  "/api/submissions/00000000-0000-0000-0000-000000000000"
Ca "8. POST /hubs/submissions/negotiate"      POST "/hubs/submissions/negotiate?negotiateVersion=1"
Ca "9. POST /hub/submissions/negotiate (sai)" POST "/hub/submissions/negotiate?negotiateVersion=1"
