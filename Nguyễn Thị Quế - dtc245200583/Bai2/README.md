# EduSubmit - BÃ i 2 RESTful API

**Sinh viÃªn:** Nguyá»…n Thá»‹ Quáº¿
**MSSV:** DTC245200583
**Ná»n táº£ng:** .NET 8, ASP.NET Core Minimal API, HttpClient

> ÄÃ¢y lÃ  bá»™ mÃ£ nguá»“n EduSubmit Ä‘Æ°á»£c dá»±ng theo phá»¥ lá»¥c hÆ°á»›ng dáº«n BÃ i 2. TrÆ°á»›c khi ná»™p, cáº§n cháº¡y thá»­ trÃªn mÃ¡y cÃ¡ nhÃ¢n, thay URL kho mÃ£ nguá»“n máº«u báº±ng URL repository tháº­t vÃ  bá»• sung áº£nh chá»¥p káº¿t quáº£ tháº­t vÃ o `docs/images/`.

## 1. Cáº¥u trÃºc thÆ° má»¥c

```text
Bai2/
â”œâ”€â”€ EduSubmit.sln
â”œâ”€â”€ .gitignore
â”œâ”€â”€ README.md
â”œâ”€â”€ requests.http
â””â”€â”€ src/
    â”œâ”€â”€ Submission.Api/
    â”‚   â”œâ”€â”€ Submission.Api.csproj
    â”‚   â””â”€â”€ Program.cs
    â””â”€â”€ Submission.Client/
        â”œâ”€â”€ Submission.Client.csproj
        â””â”€â”€ Program.cs
```

## 2. YÃªu cáº§u

- CÃ i .NET SDK 8.0 trá»Ÿ lÃªn.
- Má»Ÿ terminal táº¡i thÆ° má»¥c chá»©a `EduSubmit.sln`.

Kiá»ƒm tra SDK:

```powershell
dotnet --list-sdks
```
## 3. Build solution

Sá»­ dá»¥ng solution Ä‘á»‹nh dáº¡ng `.sln` Ä‘á»ƒ tÆ°Æ¡ng thÃ­ch vá»›i .NET SDK 8:

```powershell
dotnet restore .\EduSubmit.sln
dotnet build .\EduSubmit.sln

Káº¿t quáº£ mong Ä‘á»£i: `Build succeeded`, khÃ´ng cÃ³ lá»—i.

## 4. Cháº¡y API

Má»Ÿ Terminal 1 táº¡i thÆ° má»¥c gá»‘c:

```powershell
dotnet run --project .\src\Submission.Api\Submission.Api.csproj --urls http://localhost:5000
```

Giá»¯ terminal nÃ y má»Ÿ. Kiá»ƒm tra API báº±ng Terminal 2:

```powershell
curl.exe -i http://localhost:5000/
curl.exe -i http://localhost:5000/api/assignments
```

API sá»­ dá»¥ng dá»¯ liá»‡u trong RAM. Khi khá»Ÿi Ä‘á»™ng láº¡i API, danh sÃ¡ch lÆ°á»£t ná»™p Ä‘Æ°á»£c táº¡o trÆ°á»›c Ä‘Ã³ sáº½ máº¥t.

## 5. Cháº¡y Client

Má»Ÿ Terminal 2 táº¡i thÆ° má»¥c gá»‘c:

```powershell
dotnet run --project .\src\Submission.Client\Submission.Client.csproj
```

Client thá»±c hiá»‡n Ä‘á»§ 4 náº¥c:

1. Liá»‡t kÃª cÃ¡c bÃ i táº­p Ä‘ang má»Ÿ.
2. Gá»­i POST táº¡o lÆ°á»£t ná»™p.
3. Äá»c láº¡i lÆ°á»£t ná»™p báº±ng URI nháº­n tá»« header `Location`.
4. Cá»‘ Ã½ gá»­i dá»¯ liá»‡u sai, kiá»ƒm tra mÃ£ HTTP trÆ°á»›c khi Ä‘á»c lá»—i; chÆ°Æ¡ng trÃ¬nh khÃ´ng bá»‹ dá»«ng Ä‘á»™t ngá»™t vÃ¬ lá»—i 400.

CÃ³ thá»ƒ truyá»n Ä‘á»‹a chá»‰ API khÃ¡c qua tham sá»‘ dÃ²ng lá»‡nh:

```powershell
dotnet run --project .\src\Submission.Client\Submission.Client.csproj -- http://localhost:5000
```

## 6. Há»£p Ä‘á»“ng API

| PhÆ°Æ¡ng thá»©c | URI | ThÃ nh cÃ´ng | Lá»—i |
|---|---|---|---|
| GET | `/api/assignments` | 200 danh sÃ¡ch bÃ i táº­p | â€” |
| GET | `/api/assignments/{id}` | 200 chi tiáº¿t bÃ i táº­p | 404 `ASSIGNMENT_NOT_FOUND` |
| POST | `/api/assignments/{id}/submissions` | 201 + `Location` | 400 `INVALID_SUBMISSION`, 400 `ASSIGNMENT_CLOSED`, 404 `ASSIGNMENT_NOT_FOUND` |
| GET | `/api/submissions/{submissionId}` | 200 lÆ°á»£t ná»™p | 404 `SUBMISSION_NOT_FOUND` |

CÃ¡c lá»—i do handler chá»§ Ä‘á»™ng tráº£ vá» sá»­ dá»¥ng cÃ¹ng cáº¥u trÃºc:

```json
{
  "code": "INVALID_SUBMISSION",
  "message": "Thieu ma sinh vien hoac kho ma nguon."
}
```

## 7. Cháº¡y requests.http

Má»Ÿ `requests.http` báº±ng VS Code cÃ³ REST Client extension. Khá»Ÿi Ä‘á»™ng API trÆ°á»›c, sau Ä‘Ã³ báº¥m **Send Request** theo thá»© tá»± tá»« 1 Ä‘áº¿n 7. Request sá»‘ 5 láº¥y `id` Ä‘á»™ng tá»« pháº£n há»“i cá»§a request sá»‘ 4.

## 8. Báº£ng phÃ¡ thá»­ theo 8 ca trong tÃ i liá»‡u hÆ°á»›ng dáº«n

| Ca | Thá»­ nghiá»‡m | MÃ£ mong Ä‘á»£i | NÆ¡i tráº£ lá»i |
|---|---|---:|---|
| 5a | POST bÃ i 1 vá»›i body há»£p lá»‡ | 201 + Location | Handler |
| 5b | POST bÃ i 1 vá»›i body rá»—ng | 400 `INVALID_SUBMISSION` | Handler |
| 5c | POST bÃ i 999 | 404 `ASSIGNMENT_NOT_FOUND` | Handler |
| 5d | Bá» header `Content-Type` khi gá»­i JSON | 415, body rá»—ng | Framework |
| 5e | Gá»­i JSON sai cÃº phÃ¡p | 400, body vÄƒn báº£n | Framework |
| 5f | Body thiáº¿u `repositoryUrl` | 400 `INVALID_SUBMISSION` | Handler |
| 5g | PUT `/api/assignments/1` | 405, `Allow: GET` | Framework |
| 5h | POST vÃ o bÃ i táº­p Ä‘Ã£ Ä‘Ã³ng (ID 3) | 400 `ASSIGNMENT_CLOSED` | Handler |

LÆ°u Ã½: `requests.http` cÃ³ 7 request theo checklist ná»™p bÃ i. Báº£ng trÃªn ghi Ä‘á»§ 8 tÃ¬nh huá»‘ng phÃ¡ thá»­; cÃ³ thá»ƒ thá»±c hiá»‡n ca 5d, 5e, 5g báº±ng cÃ¡ch táº¡o request thá»­ bá»• sung hoáº·c dÃ¹ng lá»‡nh HTTP riÃªng. Ba ca do framework tráº£ lá»i khÃ´ng nháº¥t thiáº¿t cÃ³ `ApiError`.

## 9. Ba cÃ¢u há»i giáº£i thÃ­ch

### CÃ¢u 1. VÃ¬ sao URI dÃ¹ng danh tá»«, khÃ´ng dÃ¹ng Ä‘á»™ng tá»«?

URI dÃ¹ng Ä‘á»ƒ Ä‘á»‹nh danh tÃ i nguyÃªn. HÃ nh Ä‘á»™ng Ä‘Æ°á»£c thá»ƒ hiá»‡n báº±ng phÆ°Æ¡ng thá»©c HTTP. VÃ­ dá»¥ `POST /api/assignments/1/submissions` cÃ³ nghÄ©a lÃ  táº¡o má»™t lÆ°á»£t ná»™p thuá»™c bÃ i táº­p sá»‘ 1. KhÃ´ng cáº§n Ä‘áº·t URI kiá»ƒu `/submitAssignment`.

### CÃ¢u 2. Stateless thá»ƒ hiá»‡n á»Ÿ Ä‘Ã¢u?

Má»—i request tá»± mang Ä‘á»§ thÃ´ng tin cáº§n thiáº¿t Ä‘á»ƒ server xá»­ lÃ½. VÃ­ dá»¥, `assignmentId` náº±m trÃªn URI, cÃ²n `studentCode` vÃ  `repositoryUrl` náº±m trong JSON body. Server khÃ´ng cáº§n dá»±a vÃ o tráº¡ng thÃ¡i phiÃªn lÃ m viá»‡c Ä‘Æ°á»£c lÆ°u tá»« request trÆ°á»›c cá»§a client.

### CÃ¢u 3. VÃ¬ sao má»—i bÃªn khai bÃ¡o DTO riÃªng?

Client vÃ  API lÃ  hai chÆ°Æ¡ng trÃ¬nh Ä‘á»™c láº­p. DTO lÃ  mÃ´ táº£ cáº¥u trÃºc dá»¯ liá»‡u trao Ä‘á»•i qua JSON; hai bÃªn chá»‰ cáº§n thá»‘ng nháº¥t tÃªn trÆ°á»ng vÃ  kiá»ƒu dá»¯ liá»‡u trong há»£p Ä‘á»“ng. VÃ¬ váº­y, khÃ´ng cáº§n táº¡o project dÃ¹ng chung hoáº·c tham chiáº¿u trá»±c tiáº¿p class C# cá»§a bÃªn cÃ²n láº¡i.

## 10. áº¢nh minh chá»©ng cáº§n bá»• sung

Táº¡o thÆ° má»¥c `docs/images/` vÃ  lÆ°u áº£nh chá»¥p tháº­t cá»§a cÃ¡c káº¿t quáº£ sau:

1. `01-build-succeeded.png`: cá»­a sá»• terminal cháº¡y `dotnet build`.
2. `02-api-assignments.png`: API tráº£ 200 vÃ  danh sÃ¡ch bÃ i táº­p.
3. `03-client-four-steps.png`: Client cháº¡y Ä‘á»§ bá»‘n náº¥c.
4. `04-post-created.png`: POST há»£p lá»‡ tráº£ 201 vÃ  cÃ³ `Location`.
5. `05-invalid-submission.png`: POST dá»¯ liá»‡u sai tráº£ 400 cÃ¹ng `ApiError`.

**KhÃ´ng dÃ¹ng áº£nh minh há»a hoáº·c áº£nh dá»±ng thay cho káº¿t quáº£ cháº¡y tháº­t.** Sau khi chá»¥p, chÃ¨n áº£nh vÃ o README hoáº·c Ä‘áº·t cÃ¡c áº£nh trong thÆ° má»¥c `docs/images/`.

## 11. Vá»‹ trÃ­ bÃ i ná»™p trÃªn GitHub

BÃ i 2 Ä‘Æ°á»£c Ä‘áº·t trong repository theo cáº¥u trÃºc:

```text
Nguyá»…n Thá»‹ Quáº¿ - dtc245200583/
â””â”€â”€ Bai2/
    â”œâ”€â”€ CatalogApi/
    â”œâ”€â”€ docs/
    â”‚   â””â”€â”€ images/
    â”œâ”€â”€ src/
    â”‚   â”œâ”€â”€ Submission.Api/
    â”‚   â””â”€â”€ Submission.Client/
    â”œâ”€â”€ EduSubmit.sln
    â”œâ”€â”€ EduSubmit.slnx
    â”œâ”€â”€ global.json
    â”œâ”€â”€ README.md
    â””â”€â”€ requests.http
**ThÃ´ng tin cáº­p nháº­t bÃ i ná»™p:**
- Repository: https://github.com/Dtc245200583/CNTTK23D
- Branch: `patch-1`
- Pull Request: https://github.com/PTUDPTK23/CNTTK23D/pull/70

BÃ i 2 Ä‘Æ°á»£c bá»• sung vÃ o Pull Request hiá»‡n cÃ³, khÃ´ng táº¡o Pull Request má»›i.
