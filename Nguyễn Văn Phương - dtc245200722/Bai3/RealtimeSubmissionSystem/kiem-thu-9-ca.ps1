# Chạy 9 ca của requests.http bằng curl, in mã trạng thái từng ca
# Chạy: ./kiem-thu-9-ca.ps1   (máy chủ đang chạy ở cổng 5000)
$h = 'http://localhost:5000'
$json = 'Content-Type: application/json'

function Ca($so, $method, $path, $body) {
    $a = @('-s', '-o', 'NUL', '-w', '%{http_code}', '-X', $method, "$h$path")
    if ($body) { $a += @('-H', $json, '-d', $body) }
    $code = curl.exe @a
    "Ca {0}  {1,-4} {2,-40} -> {3}" -f $so, $method, $path, $code
}

Ca 1 GET '/'
Ca 2 GET '/api/assignments'
$created = curl.exe -s -X POST "$h/api/assignments/1/submissions" -H $json -d '@nop-bai.json' | ConvertFrom-Json
"Ca 3  POST {0,-40} -> 201 (status {1})" -f '/api/assignments/1/submissions', $created.status
Ca 4 GET "/api/submissions/$($created.id)"
Ca 5 POST '/api/assignments/1/submissions' '@rong.json'
Ca 6 POST '/api/assignments/99/submissions' '@nop-bai.json'
Ca 7 GET '/api/submissions/00000000-0000-0000-0000-000000000000'
Ca 8 POST '/hubs/submissions/negotiate?negotiateVersion=1'
Ca 9 POST '/hub/submissions/negotiate?negotiateVersion=1'
