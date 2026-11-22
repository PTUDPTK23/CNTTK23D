using System.Net.Http.Json;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5227") };

while (true)
{
    Console.WriteLine("\n=== QUAN LY TON KHO (CLIENT) ===");
    Console.WriteLine("1. Xem danh sach ton kho");
    Console.WriteLine("2. Xem chi tiet theo ID");
    Console.WriteLine("3. Them mat hang moi");
    Console.WriteLine("4. Nhap / Xuat kho");
    Console.WriteLine("0. Thoat");
    Console.Write("Chon: ");

    var choice = Console.ReadLine();
    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1":
                var items = await client.GetFromJsonAsync<List<InventoryItem>>("/inventory");
                items?.ForEach(i => Console.WriteLine($"[{i.Id}] {i.Sku} - {i.ProductName} | {i.Warehouse} | Ton: {i.Quantity}"));
                break;
            case "2":
                Console.Write("Nhap ID: ");
                int id = int.Parse(Console.ReadLine() ?? "0");
                var res = await client.GetAsync($"/inventory/{id}");
                Console.WriteLine(res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : "Khong tim thay!");
                break;
            case "3":
                Console.Write("Ma SKU: "); string sku = Console.ReadLine() ?? "";
                Console.Write("Ten SP: "); string name = Console.ReadLine() ?? "";
                Console.Write("Ten Kho: "); string wh = Console.ReadLine() ?? "";
                Console.Write("So luong: "); int qty = int.Parse(Console.ReadLine() ?? "0");
                var postRes = await client.PostAsJsonAsync("/inventory", new { Sku = sku, ProductName = name, Warehouse = wh, Quantity = qty });
                Console.WriteLine(postRes.IsSuccessStatusCode ? "Them thanh cong!" : "Loi!");
                break;
            case "4":
                Console.Write("Nhap ID SP: "); int adjId = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("So luong tang/giam (VD: 10 hoac -5): "); int chg = int.Parse(Console.ReadLine() ?? "0");
                var putRes = await client.PutAsJsonAsync($"/inventory/{adjId}/adjust", new { QuantityChange = chg });
                Console.WriteLine(putRes.IsSuccessStatusCode ? "Cap nhat thanh cong!" : "Loi!");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Loi ket noi Server: {ex.Message}");
    }
}

record InventoryItem(int Id, string Sku, string ProductName, string Warehouse, int Quantity);
