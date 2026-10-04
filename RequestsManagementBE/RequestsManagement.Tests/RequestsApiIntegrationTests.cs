using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.DAL.Context;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Common;
using RequestsManagement.DTO.Enums;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.Tests;

/// <summary>
/// Integration Tests - בודקים את ה-API השלם מקצה לקצה (HTTP + DB).
/// IClassFixture משתף את ה-factory בין כל הבדיקות במחלקה.
/// </summary>
public class RequestsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>הזרקת פנייה ישירות ל-DB והחזרת ה-Id.</summary>
    private long SeedRequest(RequestStatus status = RequestStatus.New)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = new Request
        {
            Title = "פנייה לבדיקה",
            OrganizationName = "ארגון בדיקה",
            Status = status,
            Priority = RequestPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Requests.Add(request);
        db.SaveChanges();
        return request.Id;
    }

    [Fact]
    public async Task GetRequests_ReturnsPagedResult()
    {
        // Arrange
        SeedRequest();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/requests?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResultDTO<RequestListItemDTO>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_ReturnsOk()
    {
        // Arrange
        var id = SeedRequest(RequestStatus.New);
        var client = _factory.CreateClient();

        // קוראים קודם כדי לקבל RowVersion עדכני
        var getResp = await client.GetAsync($"/api/requests?pageSize=100");
        var list = await getResp.Content.ReadFromJsonAsync<PagedResultDTO<RequestListItemDTO>>();
        var item = list!.Items.First(r => r.Id == id);

        var dto = new UpdateStatusDTO
        {
            NewStatus = RequestStatus.InProgress,
            RowVersion = item.RowVersion,
            ChangedBy = "בודק",
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/requests/{id}/status", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentRequest_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        var dto = new UpdateStatusDTO
        {
            NewStatus = RequestStatus.InProgress,
            RowVersion = Convert.ToBase64String(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }),
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/requests/999999/status", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_ConcurrentUpdate_SecondReturns409Conflict()
    {
        // Arrange - זה התרחיש הקריטי של Optimistic Concurrency
        var id = SeedRequest(RequestStatus.New);
        var client = _factory.CreateClient();

        var getResp = await client.GetAsync("/api/requests?pageSize=100");
        var list = await getResp.Content.ReadFromJsonAsync<PagedResultDTO<RequestListItemDTO>>();
        var item = list!.Items.First(r => r.Id == id);
        var staleRowVersion = item.RowVersion; // שני "המשתמשים" מחזיקים גרסה זו

        // עדכון ראשון (דנה) - מצליח, משנה את ה-RowVersion ב-DB
        var firstUpdate = new UpdateStatusDTO
        {
            NewStatus = RequestStatus.InProgress,
            RowVersion = staleRowVersion,
            ChangedBy = "דנה",
        };
        var firstResp = await client.PatchAsJsonAsync($"/api/requests/{id}/status", firstUpdate);
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // עדכון שני (יוסי) - עם ה-RowVersion הישן. אמור להיכשל ב-409.
        var secondUpdate = new UpdateStatusDTO
        {
            NewStatus = RequestStatus.Waiting,
            RowVersion = staleRowVersion, // הגרסה הישנה!
            ChangedBy = "יוסי",
        };
        var secondResp = await client.PatchAsJsonAsync($"/api/requests/{id}/status", secondUpdate);

        // Assert - העדכון המתחרה נדחה
        secondResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetHistory_AfterUpdate_ContainsChange()
    {
        // Arrange
        var id = SeedRequest(RequestStatus.New);
        var client = _factory.CreateClient();

        var getResp = await client.GetAsync("/api/requests?pageSize=100");
        var list = await getResp.Content.ReadFromJsonAsync<PagedResultDTO<RequestListItemDTO>>();
        var item = list!.Items.First(r => r.Id == id);

        await client.PatchAsJsonAsync($"/api/requests/{id}/status", new UpdateStatusDTO
        {
            NewStatus = RequestStatus.InProgress,
            RowVersion = item.RowVersion,
            ChangedBy = "בודק",
        });

        // Act
        var historyResp = await client.GetAsync($"/api/requests/{id}/history");

        // Assert
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResp.Content.ReadFromJsonAsync<List<StatusHistoryDTO>>();
        history.Should().Contain(h => h.NewStatus == RequestStatus.InProgress);
    }
}
