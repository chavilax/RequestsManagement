using RequestsManagement.BL.Interfaces;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.Tests;

/// <summary>
/// מימוש Cache מזויף לבדיקות - לא שומר כלום (תמיד miss).
/// מאפשר לבדוק את לוגיקת ה-BL בלי תלות ב-cache אמיתי.
/// </summary>
public class FakeStatsCache : IStatsCache
{
    public RequestStatsDTO? Get() => null;
    public void Set(RequestStatsDTO stats) { }
    public void Invalidate() { }
}
