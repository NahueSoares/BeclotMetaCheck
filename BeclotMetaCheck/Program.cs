using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Usar CORS
app.UseCors();

// CONFIGURACIÓN PERSONAL
string storeHash = "hyg5rc5lml";
string token = "ohsekvnr5ucoqya6o2bzmbom26zytfm";

// RUTA: /api/check-payment-access/{customerId}
app.MapGet("/api/check-payment-access/{customerId:int}", async (int customerId) =>
{
    var url = $"https://api.bigcommerce.com/stores/{storeHash}/v3/customers/{customerId}/metafields";

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("X-Auth-Token", token); // CORREGIDO
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var response = await http.GetAsync(url);
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.Problem($"No se pudo consultar BigCommerce: {response.StatusCode}\n{errorContent}");
    }

    var json = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    var value = root.GetProperty("data")
        .EnumerateArray()
        .FirstOrDefault(m =>
            m.GetProperty("namespace").GetString() == "payment_options" &&
            m.GetProperty("key").GetString() == "allow_check_payment"
        );

    var allow = value.ValueKind != JsonValueKind.Undefined &&
                value.TryGetProperty("value", out var valProp) &&
                valProp.GetString()?.ToLower() == "true";

    return Results.Ok(new { allowCheck = allow });
});

app.Run();