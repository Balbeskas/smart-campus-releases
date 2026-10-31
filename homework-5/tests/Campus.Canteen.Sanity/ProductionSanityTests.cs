using System.Net;
using System.Net.Http.Json;
using Campus.Canteen.Payments.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Campus.Canteen.Sanity;

/// <summary>
/// Production Sanity-тест релиза v1.9.0 (CAMPUS-104).
/// Сразу после выката проводит боевой сценарий «оплата обеда на 1 рубль» с технической карты,
/// проверяет, что сумма сохранилась и читается через новую колонку amount,
/// и в блоке finally ВСЕГДА выполняет очистку: возврат (Refund) + удаление технического чека.
///
/// Против боевого/стендового сервиса:  $env:CAMPUS_SANITY_URL = "http://localhost:5104"; dotnet test
/// Без переменной сервис поднимается в памяти через WebApplicationFactory.
/// </summary>
[Trait("Category", "ProductionSanity")]
public class ProductionSanityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SanityCard = CheckoutController.SanityCardPrefix + "0001";
    private readonly HttpClient _http;
    private readonly ITestOutputHelper _out;

    public ProductionSanityTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _out = output;
        var url = Environment.GetEnvironmentVariable("CAMPUS_SANITY_URL");
        _http = string.IsNullOrWhiteSpace(url)
            ? factory.CreateClient()
            : new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
        _out.WriteLine($"Цель sanity-проверки: {(_http.BaseAddress?.ToString() ?? "in-memory")}");
    }

    [Fact]
    public async Task Payment_of_one_ruble_is_stored_in_amount_and_cleaned_up()
    {
        Guid? checkId = null;
        try
        {
            // 1. Боевой сценарий: оплата обеда на 1 рубль технической картой
            var pay = await _http.PostAsJsonAsync("api/v1/checks",
                new PayRequest(SanityCard, "SANITY-CASHIER", "Sanity: чай", 1.00m));
            Assert.Equal(HttpStatusCode.Created, pay.StatusCode);
            var created = await pay.Content.ReadFromJsonAsync<CheckDto>();
            checkId = created!.Id;
            _out.WriteLine($"Оплата: чек {checkId}, сумма {created.Amount} руб.");

            // 2. Чтение чека: сумма читается из новой колонки amount
            var read = await _http.GetFromJsonAsync<CheckDto>($"api/v1/checks/{checkId}");
            Assert.Equal(1.00m, read!.Amount);
            Assert.Equal("Paid", read.Status);
        }
        finally
        {
            // 3. Cleanup выполняется даже при падении проверок: деньги и данные не остаются в проде
            if (checkId is not null)
            {
                var refund = await _http.PostAsync($"api/v1/checks/{checkId}/refund", null);
                var delete = await _http.DeleteAsync($"api/v1/checks/{checkId}");
                _out.WriteLine($"Cleanup: refund={(int)refund.StatusCode}, delete={(int)delete.StatusCode}");
                Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            }
        }

        // 4. Технический чек действительно удален
        var after = await _http.GetAsync($"api/v1/checks/{checkId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Real_student_check_cannot_be_deleted_by_cleanup()
    {
        var pay = await _http.PostAsJsonAsync("api/v1/checks",
            new PayRequest("STU-240411", "CASH-02", "Компот", 45m));
        var created = await pay.Content.ReadFromJsonAsync<CheckDto>();
        await _http.PostAsync($"api/v1/checks/{created!.Id}/refund", null);

        var delete = await _http.DeleteAsync($"api/v1/checks/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(5000.01)]
    public async Task Invalid_amount_is_rejected(decimal amount)
    {
        var pay = await _http.PostAsJsonAsync("api/v1/checks",
            new PayRequest(SanityCard, "SANITY-CASHIER", "Sanity", amount));
        Assert.Equal(HttpStatusCode.BadRequest, pay.StatusCode);
    }
}
