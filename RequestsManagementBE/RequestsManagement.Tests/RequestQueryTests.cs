using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RequestsManagement.BL.Services;
using RequestsManagement.DAL.Repositories;
using RequestsManagement.DTO.Enums;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.Tests;

/// <summary>
/// בדיקות שליפה, סינון ודפדוף - דרך השכבות DAL + BL מול InMemory DB.
/// </summary>
public class RequestQueryTests
{
    // יוצר BL מחובר ל-DAL אמיתי מעל InMemory DB
    private static RequestBL CreateBL(DAL.Context.AppDbContext context)
    {
        var dal = new RequestDAL(context);
        return new RequestBL(dal, new FakeStatsCache(), NullLogger<RequestBL>.Instance);
    }

    [Fact]
    public async Task GetRequests_NoFilter_ReturnsAllWithPaging()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateWithSeed(); // 5 פניות
        var bl = CreateBL(context);
        var filter = new RequestFilterDTO { Page = 1, PageSize = 20 };

        // Act
        var result = await bl.GetRequestsAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRequests_FilterByStatus_ReturnsOnlyMatching()
    {
        // Arrange - 2 פניות בסטטוס New
        using var context = TestDbContextFactory.CreateWithSeed();
        var bl = CreateBL(context);
        var filter = new RequestFilterDTO { Status = RequestStatus.New, Page = 1, PageSize = 20 };

        // Act
        var result = await bl.GetRequestsAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(r => r.Status == RequestStatus.New);
    }

    [Fact]
    public async Task GetRequests_SearchByOrganization_ReturnsMatching()
    {
        // Arrange - 2 פניות של "מקורות"
        using var context = TestDbContextFactory.CreateWithSeed();
        var bl = CreateBL(context);
        var filter = new RequestFilterDTO { Search = "מקורות", Page = 1, PageSize = 20 };

        // Act
        var result = await bl.GetRequestsAsync(filter, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(r => r.OrganizationName == "מקורות");
    }

    [Fact]
    public async Task GetRequests_PageSizeExceedsMax_IsClampedTo100()
    {
        // Arrange - PageSize חורג מהמותר
        using var context = TestDbContextFactory.CreateWithSeed();
        var bl = CreateBL(context);
        var filter = new RequestFilterDTO { Page = 1, PageSize = 5000 };

        // Act
        var result = await bl.GetRequestsAsync(filter, CancellationToken.None);

        // Assert - ה-PageSize הוגבל ל-100 (MaxPageSize)
        result.PageSize.Should().Be(RequestBL.MaxPageSize);
    }
}
