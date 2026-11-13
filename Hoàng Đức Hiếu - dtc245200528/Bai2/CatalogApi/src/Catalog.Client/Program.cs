using System.Net;
using System.Net.Http.Json;
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5100";
using var http = new HttpClient {
BaseAddress = new Uri(apiBaseUrl)
};
