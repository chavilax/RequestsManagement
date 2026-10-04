# תכנית עבודה – מערכת ניהול פניות
**מבדק Full Stack – מפתח/ת רמה ג׳**

---

## טכנולוגיות נבחרות

| שכבה | טכנולוגיה | נימוק |
|---|---|---|
| Backend | .NET 9 Web API (C#) | ביצועים, DI מובנה, async מלא |
| Database | SQL Server + EF Core 9 | Optimistic Concurrency מובנה ב-EF, RowVersion native, אינדקסים גמישים |
| Frontend | Angular 21+ | Standalone components, RxJS, HttpClient |
| Cache | IMemoryCache (ניתן להרחיב ל-Redis) | מספיק לדרישה, פשוט לתחזוקה |
| Tests | xUnit + WebApplicationFactory | Integration tests ללא mock מורכב |

---

## מבנה הפרויקט

מבנה שכבתי בסגנון ארגוני (BL / DAL / DTO / WebApi):

```
d:\TestProject\
├── docs\                              ← תיעוד ותכנון
├── RequestsManagementBE\              ← Backend
│   ├── RequestsManagement.sln
│   ├── RequestsManagement.DTO\        ← DTOs, Enums (שכבת בסיס)
│   ├── RequestsManagement.DAL\        ← EF Core, DbContext, Entities, Repositories, Seed
│   ├── RequestsManagement.BL\         ← Business Logic, Services, Mapping
│   ├── RequestsManagement.WebApi\     ← Controllers, Middleware, Validators
│   └── RequestsManagement.Tests\      ← xUnit + Integration
└── RequestsManagementFE\              ← Angular app (בהמשך)
```

**תלויות בין שכבות:** `DTO ← DAL ← BL ← WebApi` (כולן נגישות ל-Tests)

---

## משימות לפי סדר ביצוע

---

### 🔵 EPIC 1 – תכנון ותשתית (תלויות: אין)

#### TASK-01 – תכנון בסיס הנתונים ומודל הנתונים
**מה נדרש:** הגדרת טבלאות, שדות, סוגי נתונים, אינדקסים, מבנה Audit
**תוצר:** סכמה מתועדת, קובץ migrations ראשוני
**הערכת מאמץ:** 2 שעות
**קשר לדרישות:** §מבנה פנייה, §1 שליפה, §3 היסטוריה

**אינדקסים מתוכננים:**
- `IX_Requests_Status` על Status
- `IX_Requests_Priority` על Priority
- `IX_Requests_CreatedAt` על CreatedAt
- `IX_Requests_AssignedTo` על AssignedTo
- `IX_Requests_OrganizationName` לחיפוש
- אינדקס מורכב `(Status, Priority, CreatedAt)` לשאילתות מסוננות עם מיון

#### TASK-02 – הקמת Solution ופרויקטים ✅ הושלם
**מה נדרש:** יצירת structure שכבתי, הגדרת Dependencies, DI registration, appsettings
**תוצר:** Solution עם 5 פרויקטים (DTO/DAL/BL/WebApi/Tests), builds ללא שגיאות
**הערכת מאמץ:** 1 שעה
**תלויות:** TASK-01

#### TASK-03 – יצירת 100,000 רשומות (Seeder)
**מה נדרש:** DbSeeder שניתן להריץ מחדש (truncate + re-seed), נתונים מגוונים בכל השדות
**תוצר:** פקודת seed מייצרת 100K רשומות
**הערכת מאמץ:** 1.5 שעות
**תלויות:** TASK-02

---

### 🟢 EPIC 2 – Backend Core (תלויות: EPIC 1)

#### TASK-04 – DbContext, Entities, RowVersion
**מה נדרש:** Entity `Request` עם `[Timestamp]` RowVersion, `RequestStatusHistory` entity, EF configurations
**תוצר:** Migrations, DbContext מוכן, Concurrency token מוגדר
**הערכת מאמץ:** 1.5 שעות

#### TASK-05 – DTOs ו-Mapping
**מה נדרש:** `RequestDto`, `RequestListItemDto`, `RequestFilterParams`, `UpdateStatusDto`, `BulkUpdateDto`, `StatusHistoryDto`
**תוצר:** כל ה-DTOs, mapping profile
**הערכת מאמץ:** 1 שעה
**תלויות:** TASK-04

#### TASK-06 – Repository ו-Service: שליפה עם Pagination/Filtering/Sorting
**מה נדרש:** IQueryable pipeline בצד שרת, סינון מורכב, מיון דינמי, Aggregations, CancellationToken
**תוצר:** `GET /api/requests?page=1&pageSize=20&status=New&...` מחזיר תוצאות + metadata
**הערכת מאמץ:** 3 שעות
**קשר לדרישות:** §1 שליפת נתונים

**Aggregations מתוכננים:**
1. מספר פניות לפי Status
2. מספר פניות לפי Priority

#### TASK-07 – עדכון סטטוס יחיד + Optimistic Concurrency
**מה נדרש:** `PATCH /api/requests/{id}/status`, בדיקת RowVersion, טיפול ב-DbUpdateConcurrencyException, validation מעברי סטטוס
**תוצר:** 200 OK / 404 / 409 Conflict / 400 Validation
**הערכת מאמץ:** 2 שעות
**קשר לדרישות:** §2 עדכון סטטוס

**מעברי סטטוס מותרים:**
```
New        → InProgress, Waiting
InProgress → Waiting, Completed
Waiting    → InProgress, Completed
Completed  → (סופי, אסור לשנות)
```

#### TASK-08 – Audit: היסטוריית שינויי סטטוס
**מה נדרש:** שמירה אוטומטית ל-`RequestStatusHistory` בכל עדכון, `GET /api/requests/{id}/history`
**תוצר:** Endpoint מחזיר רשימת שינויים עם RequestId, PreviousStatus, NewStatus, ChangedAt, ChangedBy
**הערכת מאמץ:** 1.5 שעות
**קשר לדרישות:** §3 היסטוריית שינויים
**תלויות:** TASK-07

#### TASK-09 – Bulk Update Endpoint
**מה נדרש:** `POST /api/requests/bulk-status`, עד 100 פניות, Partial Success עם תשובה מפורטת
**תוצר:** JSON response עם `succeeded[]`, `failed[]` + סיבת כישלון לכל אחד
**הערכת מאמץ:** 2 שעות
**קשר לדרישות:** §4 פעולת Bulk

**בחירה: Partial Success (ולא All-or-Nothing)**
נימוק: בעדכון סטטוס ארגוני, כישלון פנייה אחת לא צריך לגרור ביטול של 99 אחרות. חשוב יותר לדעת בדיוק מה הצליח ומה נכשל.

#### TASK-10 – Global Error Handling + Validation + Logging
**מה נדרש:** Middleware לטיפול מרכזי בשגיאות, FluentValidation, built-in logging
**תוצר:** כל שגיאה מחזירה ProblemDetails מובנה, לוגים בנקודות מרכזיות
**הערכת מאמץ:** 1.5 שעות

#### TASK-11 – Cache עבור Aggregations
**מה נדרש:** IMemoryCache על endpoint הסטטיסטיקות, Expiration, Invalidation בעדכון סטטוס
**תוצר:** `GET /api/requests/stats` מוגש מ-cache, מתאפס בכל שינוי סטטוס
**הערכת מאמץ:** 1 שעה
**קשר לדרישות:** §7 Cache
**תלויות:** TASK-06, TASK-07

---

### 🟡 EPIC 3 – Angular Frontend (תלויות: EPIC 2)

#### TASK-12 – הקמת Angular Project + מבנה
**מה נדרש:** Angular 17+, standalone components, environments, HttpClient, routing
**תוצר:** פרויקט רץ, מחובר ל-API, מבנה תיקיות מסודר
**הערכת מאמץ:** 1 שעה

#### TASK-13 – RequestsService
**מה נדרש:** Service עם כל קריאות ה-API, טיפול ב-HttpParams, TypeScript interfaces
**תוצר:** Service מלא עם getRequests, updateStatus, bulkUpdate, getHistory
**הערכת מאמץ:** 1.5 שעות
**תלויות:** TASK-12

#### TASK-14 – רשימת פניות עם חיפוש/סינון/מיון/דפדוף
**מה נדרש:** Component עם טבלה, debounce 300ms על חיפוש, switchMap לביטול בקשות ישנות, pagination controls, Loading/Empty/Error states
**תוצר:** רכיב עובד עם כל הסינונים מחוברים לשרת
**הערכת מאמץ:** 3 שעות
**קשר לדרישות:** §5 Angular

#### TASK-15 – עדכון סטטוס + טיפול ב-409
**מה נדרש:** Dialog/inline update, טיפול ב-Conflict (הצגת הודעה ברורה + refresh), מניעת שליחה כפולה
**תוצר:** UX תקין לעדכון, הודעת שגיאה מובנת ב-409
**הערכת מאמץ:** 1.5 שעות
**תלויות:** TASK-14

#### TASK-16 – Aggregations Panel + היסטוריית שינויים
**מה נדרש:** הצגת סטטיסטיקות מה-API, panel/modal להיסטוריית שינויים לפנייה ספציפית
**תוצר:** שני רכיבים עובדים המוצגים בממשק
**הערכת מאמץ:** 1.5 שעות
**תלויות:** TASK-13

---

### 🔴 EPIC 4 – בדיקות (תלויות: EPIC 2)

#### TASK-17 – Unit Tests: Filtering/Pagination Logic
**מה נדרש:** בדיקה שה-query pipeline מחזיר תוצאות נכונות לפי פרמטרים
**תוצר:** לפחות 2 בדיקות unit משמעותיות
**הערכת מאמץ:** 1 שעה

#### TASK-18 – Unit Test: Validation + Invalid Input
**מה נדרש:** בדיקות לסטטוס לא חוקי, מעבר סטטוס אסור, pageSize מעל הגבול
**תוצר:** לפחות 2 בדיקות validation
**הערכת מאמץ:** 1 שעה

#### TASK-19 – Unit Test: Concurrency Conflict
**מה נדרש:** סימולציה של שני עדכונים מתחרים – אחד מצליח, השני מקבל 409
**תוצר:** בדיקה המוכיחה שה-Optimistic Concurrency עובד כראוי
**הערכת מאמץ:** 1.5 שעות

#### TASK-20 – Integration Test: API + Database
**מה נדרש:** WebApplicationFactory עם DB בדיקה, בדיקת flow מלא
**תוצר:** Integration test הכולל POST → GET → PATCH ובדיקת תוצאה בפועל
**הערכת מאמץ:** 2 שעות

---

### ⚪ EPIC 5 – ביצועים ותיעוד (תלויות: EPIC 1-3)

#### TASK-21 – מדידת ביצועים ואינדקסים
**מה נדרש:** הרצת Query Plan על 2 endpoints מרכזיים, תיעוד זמן לפני/אחרי אינדקסים
**תוצר:** קובץ performance.md עם ממצאים
**הערכת מאמץ:** 1.5 שעות
**קשר לדרישות:** §6 ביצועים

#### TASK-22 – תיעוד מלא (README + docs)
**מה נדרש:** הוראות הרצה, מבנה פתרון, החלטות טכנולוגיות, Cache, Concurrency, Bulk, מגבלות, שימוש ב-AI
**תוצר:** README.md מקיף + מסמכי docs
**הערכת מאמץ:** 1.5 שעות
**קשר לדרישות:** §11 תיעוד

---

## סדר ביצוע וגרף תלויות

```
TASK-01 → TASK-02 → TASK-03
                  ↓
               TASK-04 → TASK-05 → TASK-06 → TASK-11
                              ↓
                           TASK-07 → TASK-08
                              ↓
                           TASK-09
                              ↓
                           TASK-10
                              ↓
                    TASK-12 → TASK-13 → TASK-14 → TASK-15
                                              ↓
                                           TASK-16
                                              ↓
                                   TASK-17..TASK-20
                                              ↓
                                   TASK-21 → TASK-22
```

---

## סיכום הערכת מאמץ

| EPIC | משימות | זמן משוער |
|---|---|---|
| EPIC 1 – תשתית | TASK 01-03 | ~4.5 שעות |
| EPIC 2 – Backend | TASK 04-11 | ~14 שעות |
| EPIC 3 – Angular | TASK 12-16 | ~8.5 שעות |
| EPIC 4 – בדיקות | TASK 17-20 | ~5.5 שעות |
| EPIC 5 – ביצועים + תיעוד | TASK 21-22 | ~3 שעות |
| **סה"כ** | **22 משימות** | **~35 שעות** |

---

## החלטות טכנולוגיות מרכזיות

### 1. SQL Server + EF Core (לעומת MongoDB)
**בחירה:** SQL Server
**נימוק:** RowVersion/Timestamp native בעיצוב הטבלה, EF Core מטפל ב-DbUpdateConcurrencyException אוטומטית, שאילתות מורכבות עם LINQ ו-IQueryable יעילות, JOIN ל-Audit table פשוט
**חלופה שנשקלה:** MongoDB – מתאים לנתונים לא מובנים, אך Optimistic Concurrency דורש מימוש ידני מורכב יותר

### 2. Partial Success ב-Bulk Update (לעומת All-or-Nothing)
**בחירה:** Partial Success
**נימוק:** בסביבה ארגונית, כישלון אחד לא צריך לבטל 99 עדכונים תקינים. הלקוח מקבל תמונה מלאה: מה הצליח, מה נכשל ולמה
**חלופה שנשקלה:** All-or-Nothing – פשוט יותר לוגית, אך פחות שימושי בפרקטיקה

### 3. IMemoryCache (לעומת Redis)
**בחירה:** IMemoryCache
**נימוק:** הדרישה היא "ברמה מוגבלת". IMemoryCache פשוט, ללא תלות חיצונית, מספיק למה שנדרש. תיאור ה-scale-out (Redis) יופיע בתיעוד
**מגבלה ידועה:** לא עובד בסביבה עם מספר instances – פתרון: Redis Distributed Cache

---

## סטטוס התקדמות

| משימה | סטטוס |
|---|---|
| TASK-02 – הקמת Solution ופרויקטים | ✅ הושלם |
| שאר המשימות | ⬜ בתהליך |
