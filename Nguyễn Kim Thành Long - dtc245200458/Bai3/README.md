# BĂ¡o CĂ¡o Thá»±c HĂ nh BĂ i 3: Giao Tiáº¿p Thá»i Gian Thá»±c Vá»›i SignalR
**MĂ´n há»c:** PhĂ¡t triá»ƒn á»©ng dá»¥ng phĂ¢n tĂ¡n  
**Giáº£ng viĂªn:** TS. DÆ°Æ¡ng Thu MĂ¢y  
**Sinh viĂªn thá»±c hiá»‡n:** [Há» tĂªn - MSSV]  
**MĂ£ lá»›p / NgĂ nh:** CĂ´ng nghá»‡ ThĂ´ng tin  

---

## Má»¤C Lá»¤C
1. [Tráº£ lá»i 3 cĂ¢u há»i Ä‘áº§u giá»](#pháº§n-0-tráº£-lá»i-3-cĂ¢u-há»i-Ä‘áº§u-giá»)
2. [Pháº§n 2: Ká»¹ thuáº­t láº­p trĂ¬nh cÆ¡ báº£n - PingSignalR](#pháº§n-2-ká»¹-thuáº­t-láº­p-trĂ¬nh-cÆ¡-báº£n-vá»›i-signalr-pingsignalr)
3. [Pháº§n 3: EduSubmit nháº­n thĂ´ng bĂ¡o tráº¡ng thĂ¡i thá»i gian thá»±c](#pháº§n-3-edusubmit-nháº­n-thĂ´ng-bĂ¡o-tráº¡ng-thĂ¡i-thá»i-gian-thá»±c)
   - [Báº£n thiáº¿t káº¿ kĂªnh thá»i gian thá»±c](#1-báº£n-thiáº¿t-káº¿-kĂªnh-thá»i-gian-thá»±c-4-cĂ¢u-há»i-khung)
   - [BÆ°á»›c 1: Táº¡o solution vĂ  hai project](#bÆ°á»›c-1-táº¡o-solution-realtimesubmissionsystem-vĂ -cĂ i-gĂ³i-signalr-client)
   - [BÆ°á»›c 2: XĂ¢y dá»±ng REST API, UpdatedAt vĂ  khĂ³a Ä‘á»“ng bá»™](#bÆ°á»›c-2-xĂ¢y-dá»±ng-rest-api-updatedat-vĂ -khoĂ¡-dá»¯-liá»‡u-báº£o-vá»‡)
   - [BÆ°á»›c 3: Triá»ƒn khai SubmissionHub vĂ  nhĂ³m](#bÆ°á»›c-3-triá»ƒn-khai-submissionhub-vĂ -quáº£n-lĂ½-nhĂ³m-theosubmissionid)
   - [BÆ°á»›c 4: TĂ¡c vá»¥ cháº¥m ná»n vĂ  IHubContext](#bÆ°á»›c-4-tĂ¡c-vá»¥-cháº¥m-ná»n-vá»›i-ihubcontext-vĂ -simulategradingasync)
   - [BÆ°á»›c 5: MĂ¡y khĂ¡ch SignalR vĂ  chuĂ´ng TaskCompletionSource](#bÆ°á»›c-5-mĂ¡y-khĂ¡ch-signalr-vĂ -chuĂ´ng-taskcompletionsource)
   - [BÆ°á»›c 6: Luá»“ng ná»™p bĂ i vĂ  nháº­n káº¿t quáº£ thá»i gian thá»±c](#bÆ°á»›c-6-luá»“ng-hoĂ n-chá»‰nh-post--watch--chá»-graded--get-láº¡i)
   - [BÆ°á»›c 7: 5 thĂ­ nghiá»‡m phĂ¢n tĂ¡n vĂ  phĂ¢n tĂ­ch](#bÆ°á»›c-7-5-thĂ­-nghiá»‡m-phĂ¢n-tĂ¡n-vĂ -bĂ i-há»c-káº¿t-ná»‘i)
   - [BÆ°á»›c 8: Kiá»ƒm thá»­ vá»›i requests.http (9 ca)](#bÆ°á»›c-8-kiá»ƒm-thá»­-há»‡-thá»‘ng-vá»›i-requestshttp-Ä‘á»§-9-ca)
4. [Danh sĂ¡ch commit gá»£i Ă½](#danh-sĂ¡ch-commit-gá»£i-Ă½-tá»‘i-thiá»ƒu-8-commits)
5. [Checklist trÆ°á»›c khi má»Ÿ Pull Request](#checklist-trÆ°á»›c-khi-má»Ÿ-pull-request)
6. [HÆ°á»›ng dáº«n chá»¥p áº£nh minh chá»©ng](#hÆ°á»›ng-dáº«n-chá»¥p-áº£nh-minh-chá»©ng-chuáº©n-quy-Ä‘á»‹nh)

---

## PHáº¦N 0: TRáº¢ Lá»œI 3 CĂ‚U Há»I Äáº¦U GIá»œ

### CĂ¢u 1: MĂ¡y chá»§ cĂ³ thá»ƒ chá»§ Ä‘á»™ng bĂ¡o cho mĂ¡y khĂ¡ch mĂ  khĂ´ng cáº§n mĂ¡y khĂ¡ch há»i khĂ´ng?
> **Tráº£ lá»i:**  
> **CĂ³ thá»ƒ Ä‘Æ°á»£c**, náº¿u mĂ¡y khĂ¡ch chá»§ Ä‘á»™ng má»Ÿ vĂ  duy trĂ¬ sáºµn má»™t káº¿t ná»‘i hai chiá»u lĂ¢u dĂ i (persistent connection) lĂªn mĂ¡y chá»§ thĂ´ng qua giao thá»©c **WebSocket** (Ä‘Æ°á»£c Ä‘Ă³ng gĂ³i vĂ  quáº£n lĂ½ cáº¥p cao bá»Ÿi thÆ° viá»‡n **SignalR**).  
> Khi cĂ³ sá»± kiá»‡n phĂ¡t sinh (vĂ­ dá»¥: tráº¡ng thĂ¡i bĂ i ná»™p thay Ä‘á»•i), mĂ¡y chá»§ cĂ³ thá»ƒ chá»§ Ä‘á»™ng Ä‘áº©y dá»¯ liá»‡u (push) vá» mĂ¡y khĂ¡ch thĂ´ng qua `Clients.Group(...).SendAsync(...)`. TrÆ°á»ng há»£p phĂ¡t sinh sá»± kiá»‡n tá»« bĂªn ngoĂ i Hub (nhÆ° tĂ¡c vá»¥ ná»n Background Task hoáº·c Controller/Endpoint Minimal API), mĂ¡y chá»§ sá»­ dá»¥ng **`IHubContext<THub>`** Ä‘Æ°á»£c tiĂªm phá»¥ thuá»™c (Dependency Injection) Ä‘á»ƒ phĂ¡t loa thĂ´ng bĂ¡o.

### CĂ¢u 2: ÄĂ£ cĂ³ REST rá»“i, vĂ¬ sao cĂ²n cáº§n thĂªm SignalR? Hai thá»© chia viá»‡c tháº¿ nĂ o?
> **Tráº£ lá»i:**  
> **REST** vĂ  **SignalR** khĂ´ng thay tháº¿ nhau mĂ  phá»‘i há»£p bá»• trá»£ nhau theo nguyĂªn táº¯c:  
> - **REST (YĂªu cáº§u - Pháº£n há»“i / Request-Response):** ÄĂ³ng vai trĂ² lĂ  **nguá»“n sá»± tháº­t duy nháº¥t (Single Source of Truth)**. PhĂ¹ há»£p nháº¥t cho viá»‡c táº¡o tĂ i nguyĂªn má»›i (`POST` tráº£ vá» mĂ£ tráº¡ng thĂ¡i rĂµ rĂ ng `201 Created` kĂ¨m `Location`), Ä‘á»c dá»¯ liá»‡u danh má»¥c tÄ©nh (`GET`), vĂ  Ä‘á»c tráº¡ng thĂ¡i bá»n vá»¯ng Ä‘Æ°á»£c lÆ°u trá»¯ trong cÆ¡ sá»Ÿ dá»¯ liá»‡u/bá»™ nhá»›.
> - **SignalR (Äáº©y dá»¯ liá»‡u / Push notification):** ÄĂ³ng vai trĂ² lĂ  **kĂªnh thĂ´ng bĂ¡o tá»©c thá»i (Event-driven Notification)**. GiĂºp mĂ¡y chá»§ chá»§ Ä‘á»™ng bĂ¡o tin cho mĂ¡y khĂ¡ch ngay khi tráº¡ng thĂ¡i tĂ i nguyĂªn thay Ä‘á»•i (`processing`, `graded`) mĂ  khĂ´ng báº¯t mĂ¡y khĂ¡ch pháº£i liĂªn tá»¥c gá»­i request thÄƒm dĂ² Ä‘á»‹nh ká»³ (**polling** lĂ m lĂ£ng phĂ­ bÄƒng thĂ´ng vĂ  tĂ i nguyĂªn CPU máº¡ng).  
> **Quy táº¯c phĂ¢n chia:** DĂ¹ng REST Ä‘á»ƒ táº¡o vĂ  Ä‘á»c tĂ i nguyĂªn; dĂ¹ng SignalR Ä‘á»ƒ Ä‘áº©y thĂ´ng bĂ¡o tráº¡ng thĂ¡i tá»©c thĂ¬.

### CĂ¢u 3: Náº¿u Ä‘á»©t máº¡ng Ä‘Ăºng lĂºc mĂ¡y chá»§ bĂ¡o "Ä‘Ă£ cháº¥m xong" (graded) thĂ¬ sao?
> **Tráº£ lá»i:**  
> ThĂ´ng Ä‘iá»‡p SignalR phĂ¡t Ä‘i qua máº¡ng trong lĂºc mĂ¡y khĂ¡ch máº¥t káº¿t ná»‘i sáº½ **bá»‹ máº¥t vÄ©nh viá»…n** do SignalR khĂ´ng máº·c Ä‘á»‹nh lÆ°u trá»¯ bá»™ Ä‘á»‡m tin nháº¯n hĂ ng Ä‘á»£i (message queue/replay) cho cĂ¡c client bá»‹ ngáº¯t káº¿t ná»‘i.  
> Tuy nhiĂªn, nhá» kiáº¿n trĂºc phĂ¢n táº§ng Ä‘Ăºng Ä‘áº¯n:  
> 1. Khi cĂ³ máº¡ng trá»Ÿ láº¡i, tĂ­nh nÄƒng `WithAutomaticReconnect()` cá»§a SignalR Client sáº½ tá»± Ä‘á»™ng ná»‘i láº¡i vá»›i má»™t `ConnectionId` má»›i.  
> 2. MĂ¡y khĂ¡ch sáº½ gia nháº­p láº¡i nhĂ³m vĂ  **gá»i láº¡i REST API (`GET /api/submissions/{id}`)** Ä‘á»ƒ Ä‘á»c tráº¡ng thĂ¡i hiá»‡n thá»i cá»§a bĂ i ná»™p tá»« "nguá»“n sá»± tháº­t".  
> **Kháº©u hiá»‡u cá»‘t lĂµi:** *"Push Ä‘á»ƒ nhanh, REST Ä‘á»ƒ cháº¯c"*.

---

## PHáº¦N 2: Ká»¸ THUáº¬T Láº¬P TRĂŒNH CÆ  Báº¢N Vá»I SIGNALR (PingSignalR)

### BÆ°á»›c 1: Táº¡o solution vĂ  cĂ i gĂ³i SignalR Client
![build](anh/buoc1-build.png)
- **Tháº¥y:** ThĂªm gĂ³i `Microsoft.AspNetCore.SignalR.Client` vĂ o `Ping.Client.csproj` vĂ  `dotnet build` bĂ¡o `Build succeeded, 0 Error(s)`.
- **Giáº£i thĂ­ch:** Thiáº¿t láº­p cáº¥u trĂºc solution gá»“m mĂ¡y chá»§ Minimal API `Ping.Api` vĂ  mĂ¡y khĂ¡ch console `Ping.Client`.

### BÆ°á»›c 2: Báº¯t tay káº¿t ná»‘i (Negotiate)
![negotiate](anh/buoc2-negotiate.png)
- **Tháº¥y:** Gá»i `POST /hubs/ping/negotiate?negotiateVersion=1` tráº£ vá» `connectionId`, `connectionToken` vĂ  3 cÆ¡ cháº¿ truyá»n táº£i dá»± phĂ²ng (`WebSockets`, `ServerSentEvents`, `LongPolling`). Gá»i `GET /hubs/ping` khĂ´ng cĂ³ tham sá»‘ tráº£ vá» `400 Bad Request (Connection ID required)`.
- **Giáº£i thĂ­ch:** SignalR luĂ´n thá»±c hiá»‡n bÆ°á»›c báº¯t tay HTTP negotiate trÆ°á»›c khi quyáº¿t Ä‘á»‹nh nĂ¢ng cáº¥p lĂªn giao thá»©c káº¿t ná»‘i tá»‘i Æ°u nháº¥t mĂ  client/server cĂ¹ng há»— trá»£.

### BÆ°á»›c 3â€“4: PhÆ°Æ¡ng thá»©c Echo
![echo](anh/buoc4-echo.png)
- **Tháº¥y:** MĂ¡y chá»§ cháº¡y, mĂ¡y khĂ¡ch gá»i `connection.InvokeAsync("Echo", ...)` vĂ  in káº¿t quáº£ `[Echo] Echo: Xin chao SignalR tu An`.
- **Giáº£i thĂ­ch:** Hai chiá»u trĂªn cĂ¹ng má»™t káº¿t ná»‘i: mĂ¡y khĂ¡ch gá»­i lá»‡nh qua `InvokeAsync`, mĂ¡y chá»§ xá»­ lĂ½ vĂ  pháº£n há»“i riĂªng cho káº¿t ná»‘i Ä‘Ă³ báº±ng `Clients.Caller.SendAsync("EchoResponse", ...)`.

### BÆ°á»›c 5: PhĂ¡t rá»™ng (Broadcast) vĂ  NhĂ³m (Group)
![nhom](anh/buoc5-nhom.png)
- **Tháº¥y:** Hai mĂ¡y khĂ¡ch An vĂ  BĂ¬nh cĂ¹ng tham gia nhĂ³m `lop-hoc-01`. Cá»­a sá»• An nháº­n Ä‘Æ°á»£c tin `[Group]` vĂ  `[Broadcast]` cá»§a BĂ¬nh gá»­i sang, nhÆ°ng hoĂ n toĂ n khĂ´ng nháº­n Ä‘Æ°á»£c tin `[Echo]` cá»§a BĂ¬nh.
- **Giáº£i thĂ­ch:** `Clients.Caller` chá»‰ gá»­i cho ngÆ°á»i gá»i, `Clients.Group` gá»­i cho cĂ¡c thĂ nh viĂªn trong nhĂ³m, cĂ²n `Clients.All` phĂ¡t rá»™ng cho má»i káº¿t ná»‘i Ä‘ang má»Ÿ.

### BÆ°á»›c 6: VĂ²ng Ä‘á»i káº¿t ná»‘i vĂ  tá»± Ä‘á»™ng káº¿t ná»‘i láº¡i
![noilai](anh/buoc6-noilai.png)
- **Tháº¥y:** Khi táº¯t mĂ¡y chá»§, mĂ¡y khĂ¡ch in `[Lifecycle] Dang ket noi lai...`. Khi mĂ¡y chá»§ báº­t láº¡i, mĂ¡y khĂ¡ch in `[Lifecycle] Da ket noi lai` vá»›i má»™t `ConnectionId` hoĂ n toĂ n má»›i.
- **Giáº£i thĂ­ch:** `WithAutomaticReconnect()` quáº£n lĂ½ lá»‹ch trĂ¬nh thá»­ láº¡i (0s, 2s, 10s, 30s). Khi ná»‘i láº¡i thĂ nh cĂ´ng, mĂ¡y chá»§ cáº¥p mĂ£ káº¿t ná»‘i má»›i, do Ä‘Ă³ cĂ¡c nhĂ³m cÅ© gáº¯n vá»›i mĂ£ káº¿t ná»‘i trÆ°á»›c Ä‘Ă³ sáº½ bá»‹ huá»·.

### BÆ°á»›c 7: Ba thĂ­ nghiá»‡m lá»—i cá»‘ Ă½ (PingSignalR)
- **ThĂ­ nghiá»‡m 1 (MĂ¡y chá»§ chÆ°a cháº¡y):** ![loi1](anh/buoc7-loi1.png)  
  *Tháº¥y:* `HttpRequestException: Connection refused (localhost:5200)`.  
  *Giáº£i thĂ­ch:* `WithAutomaticReconnect` chá»‰ kĂ­ch hoáº¡t khi Ä‘Ă£ má»Ÿ káº¿t ná»‘i thĂ nh cĂ´ng trÆ°á»›c Ä‘Ă³; náº¿u láº§n báº¯t tay Ä‘áº§u tiĂªn tháº¥t báº¡i, `StartAsync` nĂ©m ngoáº¡i lá»‡ ngay láº­p tá»©c.
- **ThĂ­ nghiá»‡m 2 (Sai Ä‘Æ°á»ng dáº«n /hub/ping):** ![loi2](anh/buoc7-loi2.png)  
  *Tháº¥y:* MĂ¡y khĂ¡ch nháº­n lá»—i HTTP `404 Not Found`.  
  *Giáº£i thĂ­ch:* ÄÆ°á»ng dáº«n phĂ­a client truyá»n vĂ o `WithUrl` pháº£i khá»›p tuyá»‡t Ä‘á»‘i vá»›i endpoint Ä‘Ă£ cáº¥u hĂ¬nh táº¡i `MapHub`.
- **ThĂ­ nghiá»‡m 3 (Sai tĂªn thĂ´ng Ä‘iá»‡p EchoRespone):** ![loi3](anh/buoc7-loi3.png)  
  *Tháº¥y:* ChÆ°Æ¡ng trĂ¬nh cháº¡y im láº·ng, khĂ´ng cĂ³ lá»—i runtime nhÆ°ng khĂ´ng bao giá» in ra káº¿t quáº£ Echo.  
  *Giáº£i thĂ­ch:* Chiá»u Ä‘áº©y thĂ´ng Ä‘iá»‡p tá»« server xuá»‘ng client Ä‘á»‹nh tuyáº¿n theo tĂªn string; lá»‡ch tĂªn sá»± kiá»‡n khiáº¿n client Ă¢m tháº§m bá» qua.

---

## PHáº¦N 3: EDUSUBMIT NHáº¬N THĂ”NG BĂO TRáº NG THĂI THá»œI GIAN THá»°C

### 1. Báº£n thiáº¿t káº¿ kĂªnh thá»i gian thá»±c (4 cĂ¢u há»i khung)
TrÆ°á»›c khi láº­p trĂ¬nh, há»‡ thá»‘ng Ä‘Æ°á»£c thiáº¿t káº¿ theo khung 4 cĂ¢u há»i thá»i gian thá»±c:
1. **Sá»± kiá»‡n nĂ o kĂ­ch hoáº¡t thĂ´ng Ä‘iá»‡p?**  
   Má»—i khi tráº¡ng thĂ¡i lÆ°á»£t ná»™p bĂ i thay Ä‘á»•i trong tiáº¿n trĂ¬nh cháº¥m: `received` $\rightarrow$ `processing` $\rightarrow$ `graded`.
2. **Gá»­i cho ai?**  
   Chá»‰ gá»­i cho mĂ¡y khĂ¡ch Ä‘ang theo dĂµi Ä‘Ăºng lÆ°á»£t ná»™p Ä‘Ă³ thĂ´ng qua nhĂ³m `Clients.Group("submission:{id}")`. Tuyá»‡t Ä‘á»‘i khĂ´ng dĂ¹ng `Clients.All` Ä‘á»ƒ Ä‘áº£m báº£o tĂ­nh riĂªng tÆ° cá»§a sinh viĂªn vĂ  tiáº¿t kiá»‡m bÄƒng thĂ´ng máº¡ng.
3. **Gá»­i kĂ¨m dá»¯ liá»‡u gĂ¬ (tá»‘i thiá»ƒu)?**  
   Dá»¯ liá»‡u tá»‘i thiá»ƒu cáº§n thiáº¿t Ä‘á»ƒ cáº­p nháº­t giao diá»‡n mĂ¡y khĂ¡ch:
   ```csharp
   record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
   ```
4. **Máº¥t káº¿t ná»‘i thĂ¬ láº¥y láº¡i tráº¡ng thĂ¡i tháº¿ nĂ o?**  
   Khi máº¥t káº¿t ná»‘i hoáº·c ná»‘i láº¡i, mĂ¡y khĂ¡ch gá»i láº¡i REST API: `GET /api/submissions/{id}` Ä‘á»ƒ Ä‘á»c tráº¡ng thĂ¡i chĂ­nh xĂ¡c nháº¥t Ä‘Æ°á»£c lÆ°u trá»¯ táº¡i mĂ¡y chá»§ (REST lĂ  nguá»“n sá»± tháº­t).

---

### BÆ°á»›c 1: Táº¡o solution RealtimeSubmissionSystem vĂ  cĂ i gĂ³i SignalR Client
![buoc1-build](anh/edusubmit-buoc1-build.png)
- **Tháº¥y:** Solution `RealtimeSubmissionSystem` Ä‘Æ°á»£c táº¡o cĂ¹ng 2 dá»± Ă¡n `Submission.RealtimeApi` (Web Minimal API) vĂ  `Submission.RealtimeClient` (Console). CĂ i Ä‘áº·t gĂ³i `Microsoft.AspNetCore.SignalR.Client` phiĂªn báº£n `10.0.15` cho client. Lá»‡nh `dotnet build` tráº£ vá»: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- **Giáº£i thĂ­ch:** Khá»Ÿi táº¡o cáº¥u trĂºc dá»± Ă¡n phĂ¢n tĂ¡n chuáº©n gá»“m mĂ¡y chá»§ API vĂ  mĂ¡y khĂ¡ch. SignalR phĂ­a mĂ¡y chá»§ Ä‘Ă£ cĂ³ sáºµn trong `Microsoft.AspNetCore.App`, cĂ²n phĂ­a console client cáº§n cĂ i thĂªm thÆ° viá»‡n mĂ¡y khĂ¡ch Ä‘á»™c láº­p.

---

### BÆ°á»›c 2: XĂ¢y dá»±ng REST API, UpdatedAt vĂ  khoĂ¡ dá»¯ liá»‡u báº£o vá»‡
![edusubmit-post-201](anh/edusubmit-post-201.png)
- **Tháº¥y:** Gá»­i yĂªu cáº§u `POST /api/assignments/1/submissions` vá»›i payload ná»™p bĂ i há»£p lá»‡. MĂ¡y chá»§ tráº£ vá» mĂ£ HTTP `201 Created` kĂ¨m header `Location: /api/submissions/{id}`, thĂ¢n pháº£n há»“i chá»©a tráº¡ng thĂ¡i ban Ä‘áº§u `"status": "received"` vĂ  trÆ°á»ng thá»i gian `"updatedAt"`.
- **Giáº£i thĂ­ch:** REST Ä‘áº£m nháº­n viá»‡c táº¡o tĂ i nguyĂªn má»›i an toĂ n vĂ  cĂ³ tĂ­nh idempotent. Danh sĂ¡ch bĂ i ná»™p trong bá»™ nhá»› mĂ¡y chá»§ Ä‘Æ°á»£c báº£o vá»‡ báº±ng cÆ¡ cháº¿ Ä‘á»“ng bá»™ `lock (submissionsLock)` Ä‘á»ƒ Ä‘áº£m báº£o thread-safe khi nhiá»u request HTTP vĂ  tĂ¡c vá»¥ ná»n cháº¥m bĂ i cĂ¹ng lĂºc truy xuáº¥t vĂ  cáº­p nháº­t.

---

### BÆ°á»›c 3: Triá»ƒn khai SubmissionHub vĂ  quáº£n lĂ½ nhĂ³m theo submission:{id}
![edusubmit-negotiate](anh/edusubmit-negotiate.png)
- **Tháº¥y:** Gá»­i `POST http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1` tráº£ vá» mĂ£ HTTP `200 OK` kĂ¨m `connectionId` vĂ  danh sĂ¡ch giao thá»©c há»— trá»£ (`WebSockets`, `ServerSentEvents`, `LongPolling`). Trong khi Ä‘Ă³, náº¿u gá»i sai tuyáº¿n `/hub/submissions/...` sáº½ láº­p tá»©c nháº­n `404 Not Found`.
- **Giáº£i thĂ­ch:** MĂ¡y chá»§ Ä‘Äƒng kĂ½ dá»‹ch vá»¥ `builder.Services.AddSignalR()` vĂ  Ă¡nh xáº¡ tuyáº¿n `app.MapHub<SubmissionHub>("/hubs/submissions")`. Hub má»ng cung cáº¥p phÆ°Æ¡ng thá»©c `WatchSubmission(submissionId)` Ä‘á»ƒ Ä‘Æ°a káº¿t ná»‘i cá»§a client vĂ o nhĂ³m riĂªng `submission:{id}` thĂ´ng qua lá»›p dĂ¹ng chung `SubmissionGroups`.

---

### BÆ°á»›c 4: TĂ¡c vá»¥ cháº¥m ná»n vá»›i IHubContext vĂ  SimulateGradingAsync
![edusubmit-processing-graded](anh/edusubmit-processing-graded.png)
- **Tháº¥y:** Sau khi `POST` tráº£ vá» `201 Created`, gá»i `GET /api/submissions/{id}` ngay láº­p tá»©c tháº¥y `status: received`; sau ~1,5 giĂ¢y gá»i láº¡i tháº¥y `status: processing`; sau ~4 giĂ¢y gá»i láº¡i tháº¥y `status: graded` vĂ  `updatedAt` Ä‘Æ°á»£c cáº­p nháº­t tÄƒng dáº§n.
- **Giáº£i thĂ­ch:** Endpoint `POST` tiĂªm `IHubContext<SubmissionHub>` tá»« DI container vĂ  khá»Ÿi cháº¡y tĂ¡c vá»¥ cháº¥m bĂ i báº¥t Ä‘á»“ng bá»™ á»Ÿ ná»n khĂ´ng chá» Ä‘á»£i (`_ = SimulateGradingAsync(...)`). HĂ m `UpdateStatusAsync` thá»±c hiá»‡n Ä‘Ăºng nguyĂªn táº¯c: Cáº­p nháº­t dá»¯ liá»‡u vĂ o danh sĂ¡ch trÆ°á»›c (LÆ°u trÆ°á»›c) $\rightarrow$ táº¡o `SubmissionStatusMessage` $\rightarrow$ Ä‘áº©y sá»± kiá»‡n `SubmissionStatusChanged` tá»›i Ä‘Ăºng nhĂ³m qua `hubContext.Clients.Group(...)`.

---

### BÆ°á»›c 5: MĂ¡y khĂ¡ch SignalR vĂ  chuĂ´ng TaskCompletionSource
![edusubmit-signalr-connected](anh/edusubmit-signalr-connected.png)
- **Tháº¥y:** MĂ¡y khĂ¡ch khá»Ÿi Ä‘á»™ng, káº¿t ná»‘i tá»›i hub SignalR vĂ  in ra mĂ n hĂ¬nh dĂ²ng chá»¯: `Da ket noi SignalR.`.
- **Giáº£i thĂ­ch:** MĂ¡y khĂ¡ch khá»Ÿi táº¡o Ä‘á»“ng thá»i hai kĂªnh: kĂªnh 1 lĂ  `HttpClient` cho REST API vĂ  kĂªnh 2 lĂ  `HubConnection` cho SignalR. Äá»‘i tÆ°á»£ng `TaskCompletionSource` Ä‘Æ°á»£c khá»Ÿi táº¡o nhÆ° má»™t "cĂ¡i chuĂ´ng bĂ¡o", sáº½ kĂ­ch hoáº¡t `TrySetResult()` khi sá»± kiá»‡n `SubmissionStatusChanged` cĂ³ tráº¡ng thĂ¡i `"graded"`.

---

### BÆ°á»›c 6: Luá»“ng hoĂ n chá»‰nh: POST $\rightarrow$ Watch $\rightarrow$ Chá» graded $\rightarrow$ GET láº¡i
![edusubmit-full-flow](anh/edusubmit-full-flow.png)
- **Tháº¥y:** MĂ n hĂ¬nh console cá»§a mĂ¡y khĂ¡ch in chĂ­nh xĂ¡c Ä‘á»§ 8 dĂ²ng theo Ä‘Ăºng trĂ¬nh tá»±:
  ```text
  Da ket noi SignalR.
  Chon bai tap: 1 - Bai tap SignalR
  Da tao luot nop: 977c86bf-98e2-45d1-8a59-e2ec1a320223
  Trang thai REST ban dau: received
  Dang cho thong bao trang thai tu may chu...
  Thong bao SignalR: 977c86bf-98e2-45d1-8a59-e2ec1a320223 -> processing luc 10:25:20
  Thong bao SignalR: 977c86bf-98e2-45d1-8a59-e2ec1a320223 -> graded luc 10:25:22
  Trang thai doc lai bang REST: graded
  ```
  PhĂ­a terminal mĂ¡y chá»§ in log:
  ```text
  [Hub] n-8YllIH theo doi submission:977c86bf-98e2-45d1-8a59-e2ec1a320223
  [Push] submission:977c86bf-98e2-45d1-8a59-e2ec1a320223 -> processing
  [Push] submission:977c86bf-98e2-45d1-8a59-e2ec1a320223 -> graded
  ```
- **Giáº£i thĂ­ch:** Luá»“ng hoáº¡t Ä‘á»™ng chuáº©n xĂ¡c: ÄÄƒng kĂ½ nghe `On` $\rightarrow$ má»Ÿ káº¿t ná»‘i `StartAsync` $\rightarrow$ gá»i REST `POST` táº¡o bĂ i ná»™p $\rightarrow$ gá»i `WatchSubmission` gia nháº­p nhĂ³m $\rightarrow$ mĂ¡y chá»§ cháº¥m xong Ä‘áº©y `processing` vĂ  `graded` cĂ¡ch nhau 2 giĂ¢y $\rightarrow$ chuĂ´ng `TaskCompletionSource` reo $\rightarrow$ gá»i láº¡i REST `GET` xĂ¡c nháº­n tráº¡ng thĂ¡i cuá»‘i cĂ¹ng vĂ  chÆ°Æ¡ng trĂ¬nh tá»± Ä‘á»™ng thoĂ¡t.

---

### BÆ°á»›c 7: 5 ThĂ­ nghiá»‡m phĂ¢n tĂ¡n vĂ  bĂ i há»c káº¿t ná»‘i

#### áº¢nh minh chá»©ng tá»«ng thĂ­ nghiá»‡m:
- **ThĂ­ nghiá»‡m 1:** ![thunghiem1](anh/edusubmit-thunghiem1.png)
- **ThĂ­ nghiá»‡m 2:** ![thunghiem2](anh/edusubmit-thunghiem2.png)
- **ThĂ­ nghiá»‡m 3:** ![thunghiem3](anh/edusubmit-thunghiem3.png)
- **ThĂ­ nghiá»‡m 4:** ![thunghiem4](anh/edusubmit-thunghiem4.png)
- **ThĂ­ nghiá»‡m 5:** ![thunghiem5](anh/edusubmit-thunghiem5.png)

#### Báº£ng tá»•ng há»£p chi tiáº¿t 5 thĂ­ nghiá»‡m:

| STT | ThĂ­ nghiá»‡m | Dá»± Ä‘oĂ¡n | Káº¿t quáº£ thá»±c táº¿ | Giáº£i thĂ­ch chi tiáº¿t |
|:---:|:---|:---|:---|:---|
| **1** | **Cháº¡y 2 mĂ¡y khĂ¡ch SV001 vĂ  SV002 cĂ¹ng lĂºc** | Hai mĂ¡y khĂ¡ch cháº¡y song song Ä‘á»™c láº­p, má»—i mĂ¡y chá»‰ nháº­n thĂ´ng bĂ¡o vá» bĂ i ná»™p cá»§a chĂ­nh mĂ¬nh, khĂ´ng tháº¥y tin bĂ i cá»§a sinh viĂªn khĂ¡c. | Má»—i mĂ¡y khĂ¡ch táº¡o mĂ£ bĂ i ná»™p riĂªng vĂ  chá»‰ nháº­n Ä‘Ăºng 2 thĂ´ng bĂ¡o `processing` & `graded` tÆ°Æ¡ng á»©ng vá»›i ID cá»§a mĂ¬nh. | NhĂ³m `submission:{id}` do mĂ¡y chá»§ quáº£n lĂ½ cĂ´ láº­p kĂªnh gá»­i nháº­n. Client chá»‰ gia nháº­p nhĂ³m bĂ i cá»§a mĂ¬nh nĂªn luá»“ng thĂ´ng bĂ¡o hoĂ n toĂ n riĂªng tÆ° vĂ  tiáº¿t kiá»‡m bÄƒng thĂ´ng. |
| **2** | **Cháº¡y mĂ¡y khĂ¡ch khi mĂ¡y chá»§ chÆ°a cháº¡y** | MĂ¡y khĂ¡ch khĂ´ng thá»ƒ báº¯t tay káº¿t ná»‘i vĂ  phĂ¡t sinh lá»—i ngoáº¡i lá»‡ ngay khi khá»Ÿi Ä‘á»™ng. | NĂ©m ngoáº¡i lá»‡ `HttpRequestException: Connection refused (localhost:5000)` ngay táº¡i cĂ¢u lá»‡nh `await connection.StartAsync()`. | `WithAutomaticReconnect` chá»‰ phĂ¡t huy tĂ¡c dá»¥ng khĂ´i phá»¥c cĂ¡c káº¿t ná»‘i **Ä‘Ă£ tá»«ng thĂ nh cĂ´ng** trÆ°á»›c Ä‘Ă³. Náº¿u láº§n báº¯t tay (negotiate) Ä‘áº§u tiĂªn tháº¥t báº¡i, client nĂ©m ngoáº¡i lá»‡ dá»«ng ngay. |
| **3** | **Sá»­a WithUrl thĂ nh `/hub/submissions` (thiáº¿u 's')** | MĂ¡y chá»§ khĂ´ng tĂ¬m tháº¥y endpoint tÆ°Æ¡ng á»©ng vĂ  tráº£ vá» mĂ£ lá»—i HTTP 404. | NĂ©m ngoáº¡i lá»‡ `HttpRequestException: Response status code does not indicate success: 404 (Not Found)`. | Lá»—i phĂ¡t sinh ngay táº¡i bÆ°á»›c gá»­i HTTP POST negotiate ban Ä‘áº§u. ÄÆ°á»ng dáº«n cáº¥u hĂ¬nh á»Ÿ client báº¯t buá»™c pháº£i khá»›p chĂ­nh xĂ¡c tá»«ng kĂ½ tá»± vá»›i `MapHub` phĂ­a mĂ¡y chá»§. |
| **4** | **Sá»­a sá»± kiá»‡n thĂ nh `On("SubmissionStatusChange")` (thiáº¿u 'd')** | MĂ¡y khĂ¡ch khĂ´ng nháº­n Ä‘Æ°á»£c thĂ´ng bĂ¡o tráº¡ng thĂ¡i nĂ o tá»« SignalR, chá» háº¿t 10 giĂ¢y timeout rá»“i má»›i Ä‘á»c REST. | KhĂ´ng cĂ³ lá»—i runtime nĂ o; mĂ¡y khĂ¡ch khĂ´ng in Ä‘Æ°á»£c 2 dĂ²ng `Thong bao SignalR`; sau 10 giĂ¢y in `Qua thoi gian cho thong bao graded.`; cuá»‘i cĂ¹ng Ä‘á»c REST váº«n ra `graded`. | Sai tĂªn sá»± kiá»‡n á»Ÿ chiá»u Ä‘áº©y xuá»‘ng (Hub $\rightarrow$ Client) sáº½ bá»‹ client im láº·ng bá» qua. Tuy nhiĂªn REST váº«n Ä‘á»c Ä‘Ăºng tráº¡ng thĂ¡i vĂ¬ REST lĂ  nguá»“n sá»± tháº­t ("push Ä‘á»ƒ nhanh, REST Ä‘á»ƒ cháº¯c"). |
| **5** | **Chá» 4 giĂ¢y rá»“i má»›i gá»i `WatchSubmission`** | TĂ¡c vá»¥ cháº¥m ná»n Ä‘Ă£ hoĂ n táº¥t trÆ°á»›c khi client ká»‹p vĂ o nhĂ³m theo dĂµi, dáº«n tá»›i lá»¡ máº¥t toĂ n bá»™ sá»± kiá»‡n SignalR. | MĂ¡y khĂ¡ch khĂ´ng nháº­n Ä‘Æ°á»£c thĂ´ng Ä‘iá»‡p SignalR nĂ o; in `Qua thoi gian cho thong bao graded.`; nhÆ°ng lá»‡nh `GET` REST sau Ä‘Ă³ váº«n Ä‘á»c vá» `graded`. | SignalR khĂ´ng lÆ°u trá»¯ bá»™ Ä‘á»‡m sá»± kiá»‡n quĂ¡ khá»© cho káº¿t ná»‘i má»›i vĂ o nhĂ³m. Gia nháº­p nhĂ³m muá»™n Ä‘á»“ng nghÄ©a vá»›i viá»‡c lá»¡ tin, nhÆ°ng REST luĂ´n tráº£ vá» tráº¡ng thĂ¡i chĂ­nh xĂ¡c nháº¥t táº¡i thá»i Ä‘iá»ƒm truy váº¥n. |

---

### BÆ°á»›c 8: Kiá»ƒm thá»­ há»‡ thá»‘ng vá»›i requests.http (Äá»§ 9 ca)
File `RealtimeSubmissionSystem/requests.http` kiá»ƒm thá»­ Ä‘áº§y Ä‘á»§ 9 ca theo Ä‘Ăºng yĂªu cáº§u Ä‘á» bĂ i:

| Ca | PhÆ°Æ¡ng thá»©c & Tuyáº¿n | Má»¥c Ä‘Ă­ch | MĂ£ mong Ä‘á»£i | Káº¿t quáº£ kiá»ƒm tra |
|:---:|:---|:---|:---:|:---:|
| **1** | `GET /` | Kiá»ƒm tra tráº¡ng thĂ¡i mĂ¡y chá»§ Ä‘ang hoáº¡t Ä‘á»™ng | `200 OK` | Äáº¡t |
| **2** | `GET /api/assignments` | Láº¥y danh sĂ¡ch bĂ i táº­p kháº£ dá»¥ng | `200 OK` | Äáº¡t |
| **3** | `POST /api/assignments/1/submissions` | Táº¡o lÆ°á»£t ná»™p bĂ i há»£p lá»‡ | `201 Created` | Äáº¡t (KĂ¨m Location header) |
| **4** | `GET /api/submissions/{id}` | Äá»c láº¡i tráº¡ng thĂ¡i bĂ i ná»™p (tháº¥y Ä‘á»•i dáº§n) | `200 OK` | Äáº¡t (`received` $\rightarrow$ `processing` $\rightarrow$ `graded`) |
| **5** | `POST /api/assignments/1/submissions` | Gá»­i payload thiáº¿u dá»¯ liá»‡u (rá»—ng) | `400 Bad Request` | Äáº¡t (`INVALID_SUBMISSION`) |
| **6** | `POST /api/assignments/999/submissions` | Ná»™p bĂ i cho mĂ£ bĂ i táº­p khĂ´ng tá»“n táº¡i | `404 Not Found` | Äáº¡t (`ASSIGNMENT_NOT_FOUND`) |
| **7** | `GET /api/submissions/{guid-khong-ton-tai}` | Äá»c lÆ°á»£t ná»™p vá»›i GUID khĂ´ng cĂ³ trong há»‡ thá»‘ng | `404 Not Found` | Äáº¡t (`SUBMISSION_NOT_FOUND`) |
| **8** | `POST /hubs/submissions/negotiate?negotiateVersion=1` | Báº¯t tay káº¿t ná»‘i SignalR há»£p lá»‡ | `200 OK` | Äáº¡t (Tráº£ vá» `connectionId`) |
| **9** | `POST /hub/submissions/negotiate?negotiateVersion=1` | Báº¯t tay sai Ä‘Æ°á»ng dáº«n Hub | `404 Not Found` | Äáº¡t |

---

## DANH SĂCH COMMIT Gá»¢I Ă (Tá»‘i thiá»ƒu 8 commits)
Äá»ƒ Ä‘áº¡t Ä‘iá»ƒm tá»‘i Ä‘a tiĂªu chĂ­ Git & Pull Request, thá»±c hiá»‡n commit theo tá»«ng má»‘c tiáº¿n Ä‘á»™:
1. `git commit -m "Bai3 B1: tao solution RealtimeSubmissionSystem"`
2. `git commit -m "Bai3 B2: trien khai REST API voi submissionsLock va UpdatedAt"`
3. `git commit -m "Bai3 B3: them SubmissionHub va quan ly nhom submission:{id}"`
4. `git commit -m "Bai3 B4: tich hop IHubContext va SimulateGradingAsync"`
5. `git commit -m "Bai3 B5: xay dung Submission.RealtimeClient voi TaskCompletionSource"`
6. `git commit -m "Bai3 B6: hoan thien luong full-flow nhan ket qua graded va doc lai REST"`
7. `git commit -m "Bai3 B7: thuc hien 5 thi nghiem phan tan va phan tich ket qua"`
8. `git commit -m "Bai3 B8: bo sung requests.http du 9 ca va cap nhat README.md hoan chinh"`

---

## CHECKLIST TRÆ¯á»C KHI Má» PULL REQUEST
- [x] Lá»‡nh `dotnet build` cáº£ 2 solution (`PingSignalR` vĂ  `RealtimeSubmissionSystem`) thĂ nh cĂ´ng vá»›i `0 Error(s), 0 Warning(s)`.
- [x] ÄĂ£ cáº¥u hĂ¬nh `.gitignore` Ä‘áº§y Ä‘á»§, tuyá»‡t Ä‘á»‘i khĂ´ng Ä‘Æ°a cĂ¡c thÆ° má»¥c `bin/` vĂ  `obj/` vĂ o Git.
- [x] File `requests.http` cĂ³ Ä‘áº§y Ä‘á»§ 9 ca kiá»ƒm thá»­ vĂ  Ä‘á»u tráº£ vá» Ä‘Ăºng mĂ£ tráº¡ng thĂ¡i HTTP mong Ä‘á»£i.
- [x] Console mĂ¡y khĂ¡ch in Ä‘áº§y Ä‘á»§ 8 dĂ²ng káº¿t quáº£, nháº­n Ä‘Æ°á»£c tráº¡ng thĂ¡i `graded` vĂ  tá»± Ä‘á»™ng káº¿t thĂºc chÆ°Æ¡ng trĂ¬nh.
- [x] README.md tráº£ lá»i Ä‘áº§y Ä‘á»§ 3 cĂ¢u há»i Ä‘áº§u giá» vĂ  cĂ³ báº£ng phĂ¢n tĂ­ch 5 thĂ­ nghiá»‡m chi tiáº¿t (Dá»± Ä‘oĂ¡n, Thá»±c táº¿, Giáº£i thĂ­ch).
- [x] Lá»‹ch sá»­ Git cĂ³ tá»‘i thiá»ƒu 8 commit tÆ°Æ¡ng á»©ng vá»›i tá»«ng bÆ°á»›c thá»±c hiá»‡n.
- [x] TiĂªu Ä‘á» Pull Request Ä‘áº·t Ä‘Ăºng chuáº©n quy Ä‘á»‹nh: `Bai3 - MSSV - Há» tĂªn`.

---

## HÆ¯á»NG DáºªN CHá»¤P áº¢NH MINH CHá»¨NG CHUáº¨N QUY Äá»NH
Khi chá»¥p áº£nh lÆ°u vĂ o thÆ° má»¥c `anh/`, sinh viĂªn cáº§n lÆ°u Ă½ cĂ¡c quy táº¯c cá»§a giáº£ng viĂªn:
- **NguyĂªn táº¯c áº£nh há»£p lá»‡:** Chá»¥p báº±ng `Win + Shift + S`, tháº¥y rĂµ cáº£ dĂ²ng lá»‡nh Ä‘Ă£ gĂµ vĂ  káº¿t quáº£ in ra trong cĂ¹ng má»™t áº£nh, nhĂ¬n rĂµ Ä‘Æ°á»ng dáº«n thÆ° má»¥c bĂ i lĂ m cá»§a sinh viĂªn trĂªn terminal nháº¯c lá»‡nh.
- **Danh sĂ¡ch tĂªn tá»‡p áº£nh báº¯t buá»™c:**
  1. `buoc1-build.png`: MĂ n hĂ¬nh `dotnet build` cáº£ solution bĂ¡o `0 Error(s)`.
  2. `edusubmit-post-201.png`: Cháº¡y curl gá»­i POST táº¡o bĂ i ná»™p nháº­n `201 Created`.
  3. `edusubmit-negotiate.png`: Cháº¡y curl POST gá»i `negotiate` nháº­n `connectionId`.
  4. `edusubmit-processing-graded.png`: Cháº¡y curl GET liĂªn tiáº¿p tháº¥y tráº¡ng thĂ¡i Ä‘á»•i tá»« `received` $\rightarrow$ `processing` $\rightarrow$ `graded`.
  5. `edusubmit-signalr-connected.png`: Cháº¡y mĂ¡y khĂ¡ch tháº¥y dĂ²ng `Da ket noi SignalR.`.
  6. `edusubmit-full-flow.png`: Cháº¡y luá»“ng Ä‘áº§y Ä‘á»§ tháº¥y Ä‘á»§ 8 dĂ²ng in cá»§a mĂ¡y khĂ¡ch vĂ  log cá»§a mĂ¡y chá»§.
  7. `edusubmit-thunghiem1.png` Ä‘áº¿n `edusubmit-thunghiem5.png`: Káº¿t quáº£ mĂ n hĂ¬nh tÆ°Æ¡ng á»©ng vá»›i 5 thĂ­ nghiá»‡m á»Ÿ BÆ°á»›c 7.


