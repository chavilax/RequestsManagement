using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RequestsManagement.BL.Exceptions;
using RequestsManagement.BL.Services;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DAL.Repositories;
using RequestsManagement.DTO.Enums;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.Tests;

/// <summary>
/// בדיקות עדכון סטטוס - Validation, NotFound ומעברים אסורים.
/// </summary>
public class UpdateStatusTests
{
    private static RequestBL CreateBL(DAL.Context.AppDbContext context)
    {
        var dal = new RequestDAL(context);
        return new RequestBL(dal, new FakeStatsCache(), NullLogger<RequestBL>.Instance);
    }

    [Fact]
    public async Task UpdateStatus_RequestNotFound_ThrowsNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateWithSeed();
        var bl = CreateBL(context);
        var dto = new UpdateStatusDTO { NewStatus = RequestStatus.InProgress };

        // Act
        var act = () => bl.UpdateStatusAsync(99999, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ThrowsValidation()
    {
        // Arrange - ניצור פנייה שהושלמה, ננסה להחזיר אותה ל-InProgress (אסור)
        using var context = TestDbContextFactory.Create();
        var request = new Request
        {
            Title = "פנייה שהושלמה",
            OrganizationName = "בדיקה",
            Status = RequestStatus.Completed,
            Priority = RequestPriority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var bl = CreateBL(context);
        var dto = new UpdateStatusDTO { NewStatus = RequestStatus.InProgress };

        // Act
        var act = () => bl.UpdateStatusAsync(request.Id, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_UpdatesAndCreatesHistory()
    {
        // Arrange - פנייה New, נעדכן ל-InProgress (מותר)
        using var context = TestDbContextFactory.Create();
        var request = new Request
        {
            Title = "פנייה חדשה",
            OrganizationName = "בדיקה",
            Status = RequestStatus.New,
            Priority = RequestPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var bl = CreateBL(context);
        var dto = new UpdateStatusDTO { NewStatus = RequestStatus.InProgress, ChangedBy = "דנה" };

        // Act
        var result = await bl.UpdateStatusAsync(request.Id, dto, CancellationToken.None);

        // Assert - הסטטוס עודכן ונרשמה היסטוריה
        result.Status.Should().Be(RequestStatus.InProgress);

        var history = await bl.GetHistoryAsync(request.Id, CancellationToken.None);
        history.Should().Contain(h =>
            h.PreviousStatus == RequestStatus.New &&
            h.NewStatus == RequestStatus.InProgress &&
            h.ChangedBy == "דנה");
    }

    [Fact]
    public async Task BulkUpdate_ExceedsMaxSize_ThrowsValidation()
    {
        // Arrange - יותר מ-100 פניות
        using var context = TestDbContextFactory.Create();
        var bl = CreateBL(context);
        var dto = new BulkUpdateStatusDTO
        {
            RequestIds = Enumerable.Range(1, 101).Select(i => (long)i).ToList(),
            NewStatus = RequestStatus.InProgress,
        };

        // Act
        var act = () => bl.BulkUpdateStatusAsync(dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task BulkUpdate_MixedValidAndInvalid_ReturnsPartialSuccess()
    {
        // Arrange - פנייה תקינה (New) + מזהה לא קיים
        using var context = TestDbContextFactory.Create();
        var valid = new Request
        {
            Title = "תקינה",
            OrganizationName = "בדיקה",
            Status = RequestStatus.New,
            Priority = RequestPriority.Low,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Requests.Add(valid);
        await context.SaveChangesAsync();

        var bl = CreateBL(context);
        var dto = new BulkUpdateStatusDTO
        {
            RequestIds = new List<long> { valid.Id, 99999 }, // אחת תקינה, אחת לא קיימת
            NewStatus = RequestStatus.InProgress,
        };

        // Act
        var result = await bl.BulkUpdateStatusAsync(dto, CancellationToken.None);

        // Assert - Partial Success: אחת הצליחה, אחת נכשלה
        result.SucceededCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Failed.Should().Contain(f => f.RequestId == 99999 && f.Reason == "NotFound");
    }
}
