using FluentAssertions;
using RequestsManagement.BL.Services;
using RequestsManagement.DTO.Enums;

namespace RequestsManagement.Tests;

/// <summary>
/// Unit Tests ללוגיקת מעברי הסטטוס. בודק לוגיקה טהורה - ללא DB.
/// </summary>
public class StatusTransitionRulesTests
{
    // [Theory] + [InlineData] = מריץ את אותה בדיקה עם ערכים שונים.
    // כל שורה היא תרחיש: (סטטוס נוכחי, סטטוס יעד, האם מותר)

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.New, RequestStatus.Waiting, true)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed, true)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed, true)]
    public void IsAllowed_ValidTransitions_ReturnsTrue(
        RequestStatus from, RequestStatus to, bool expected)
    {
        // Act
        var result = StatusTransitionRules.IsAllowed(from, to);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress)] // Completed סופי
    [InlineData(RequestStatus.Completed, RequestStatus.New)]
    [InlineData(RequestStatus.New, RequestStatus.Completed)]        // חייב לעבור דרך InProgress/Waiting
    public void IsAllowed_InvalidTransitions_ReturnsFalse(
        RequestStatus from, RequestStatus to)
    {
        // Act
        var result = StatusTransitionRules.IsAllowed(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_CompletedIsFinal_NoTransitionsAllowed()
    {
        // Completed הוא סטטוס סופי - אף מעבר ממנו אינו חוקי
        foreach (RequestStatus target in Enum.GetValues<RequestStatus>())
        {
            StatusTransitionRules.IsAllowed(RequestStatus.Completed, target)
                .Should().BeFalse($"מעבר מ-Completed ל-{target} צריך להיות אסור");
        }
    }
}
