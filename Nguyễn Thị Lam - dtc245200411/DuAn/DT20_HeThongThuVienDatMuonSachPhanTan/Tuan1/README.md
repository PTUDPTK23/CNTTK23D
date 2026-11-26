# He thong thu vien va dat muon sach phan tan

Du an minh hoa .NET 10 Minimal API voi RabbitMQ, PostgreSQL va Reservation Worker.

## Kien truc

```text
Library.Client -> Library API -> RabbitMQ -> Reservation Worker -> PostgreSQL
                      |                                  |
                      +-------------- PostgreSQL --------+
```

## Thanh phan

- `Library.Api`: REST API, EF Core/PostgreSQL va nguoi gui yeu cau vao RabbitMQ.
- `Reservation.Worker`: worker doc hang doi, kiem tra AvailableCopies va cap nhat trang thai.
- `Library.Contracts`: hop dong thong diep dat muon.
- `Library.Client`: console client de demo cac API chinh.
- PostgreSQL: luu sach, nguoi dung va reservation.
- RabbitMQ: xu ly yeu cau dat muon bat dong bo.

## API

- `GET /health`
- `GET /books`
- `GET /books/{id}`
- `POST /reservations`
- `GET /reservations/{id}`
- `GET /users/{userId}/reservations`
- `POST /reservations/{id}/borrow`
- `POST /reservations/{id}/return`

Create reservation request:

```json
{
  "userId": "u1",
  "bookId": "11111111-1111-1111-1111-111111111111"
}
```

## Chay bang Docker

Docker Desktop phai dang chay.

```powershell
docker compose up --build
```

Sau khi cac service san sang:

- API: http://localhost:8080
- Swagger/OpenAPI: khong bat buoc cho Minimal API; du dung API bang curl hoac console client.
- RabbitMQ management: http://localhost:15672 (guest/guest)
- PostgreSQL: localhost:5432 (library/library)

Chay console client:

```powershell
dotnet run --project Library.Client -- http://localhost:8080
```

Vi du test:

```powershell
curl http://localhost:8080/books
curl -X POST http://localhost:8080/reservations -H "Content-Type: application/json" -d "{\"userId\":\"u1\",\"bookId\":\"11111111-1111-1111-1111-111111111111\"}"
curl http://localhost:8080/users/u1/reservations
```

## Chay rieng tren may

Can PostgreSQL va RabbitMQ dang chay tai localhost:

```powershell
dotnet run --project Library.Api
dotnet run --project Reservation.Worker
dotnet run --project Library.Client
```

Dung bien moi truong hoac `appsettings.json` de thay doi connection string, RabbitMQ va URLs.

## Cau hinh

Tat ca cau hinh phu thuoc moi truong nam trong:

- `ConnectionStrings:LibraryDatabase`
- `RabbitMQ:HostName`
- `RabbitMQ:Port`
- `RabbitMQ:UserName`
- `RabbitMQ:Password`

Trong Docker Compose, cac gia tri tren duoc truyen qua environment variables.

## Co che phuc hoi

- Worker tu dong thu lai khi RabbitMQ hoac PostgreSQL chua san sang.
- RabbitMQ su dung persistent queue va acknowledgement.
- Worker xu ly idempotent: yeu cau da khong con `Pending` se khong bi xu ly lai.
- Correlation ID duoc truyen qua message properties va application log.
