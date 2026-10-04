# מערכת ניהול פניות (Requests Management)

מערכת Full Stack לניהול פניות/בקשות המתקבלות מארגונים ומעסיקים. מיועדת לעבודה של מספר משתמשים במקביל ולכמות גדולה של פניות (נבדקה על 100,000 רשומות).

---

## טכנולוגיות וגרסאות

| שכבה | טכנולוגיה | גרסה |
|---|---|---|
| Backend | .NET / ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core | 9.0 |
| Database | SQL Server (LocalDB) | — |
| Frontend | Angular | 21 |
| UI | Angular Material | 21 |
| בדיקות | xUnit + FluentAssertions + SQLite (Integration) | — |

---

## מבנה הפתרון

```
TestProject/
├── RequestsManagementBE/              # Backend (.NET)
│   ├── RequestsManagement.WebApi/     # שכבת ה-API (Controllers, Middleware, Program)
│   ├── RequestsManagement.BL/         # לוגיקה עסקית (Services, Mapping, Rules, Exceptions)
│   ├── RequestsManagement.DAL/        # גישה לנתונים (DbContext, Entities, Repositories, Seed)
│   ├── RequestsManagement.DTO/        # אובייקטי העברת נתונים + Enums
│   └── RequestsManagement.Tests/      # בדיקות אוטומטיות (Unit + Integration)
├── RequestsManagementFE/              # Frontend (Angular)
│   └── src/app/
│       ├── components/                # רכיבים (רשימה, dialogs, פאנל stats)
│       ├── services/                  # RequestsService (קריאות API)
│       ├── models/                    # TypeScript interfaces
│       └── interceptors/              # error interceptor
└── docs/                              # תיעוד (תכנון, ביצועים, החלטות)
```

**ארכיטקטורת שכבות:** `DTO ← DAL ← BL ← WebApi`. ה-WebApi מכיר רק את ה-BL (הזרימה: Controller → BL → DAL → DB). הפרדת אחריות מלאה, Dependency Injection בכל השכבות.

---

## דרישות מוקדמות

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ / 22.12+ / 24+
- SQL Server LocalDB (מגיע עם Visual Studio) או SQL Server
- כלי EF: `dotnet tool install --global dotnet-ef`

---

## הרצה

### 1. Backend

```bash
cd RequestsManagementBE

# יצירת בסיס הנתונים והטבלאות
dotnet ef database update --project RequestsManagement.DAL --startup-project RequestsManagement.WebApi

# יצירת 100,000 רשומות בדיקה (ניתן להרצה חוזרת - מנקה ומייצר מחדש)
dotnet run --project RequestsManagement.WebApi -- --seed

# הרצת השרת
dotnet run --project RequestsManagement.WebApi
```
ה-API יעלה על `http://localhost:5080` (Swagger: `http://localhost:5080/swagger`).

### 2. Frontend

```bash
cd RequestsManagementFE
npm install
npm start
```
האפליקציה תעלה על `http://localhost:4200`.

---

## נקודות קצה (API Endpoints)

| Method | Endpoint | תיאור |
|---|---|---|
| GET | `/api/requests` | שליפה עם סינון, חיפוש, מיון ודפדוף |
| GET | `/api/requests/stats` | נתונים מסכמים (Aggregations) |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס |
| PATCH | `/api/requests/{id}/status` | עדכון סטטוס יחיד (עם Concurrency) |
| POST | `/api/requests/bulk-status` | עדכון סטטוס מרובה (עד 100) |

**פרמטרי שליפה:** `page`, `pageSize` (עד 100), `status`, `priority`, `organizationName`, `assignedTo`, `createdFrom`, `createdTo`, `search`, `sortBy`, `sortDescending`.

---

## בדיקות

```bash
cd RequestsManagementBE
dotnet test
```
22 בדיקות: Unit (לוגיקת מעברים, שליפה, סינון, validation) + Integration (API + DB כולל Concurrency 409).

---

## תיעוד נוסף

| מסמך | תוכן |
|---|---|
| [docs/work-plan.md](docs/work-plan.md) | תכנון העבודה, פירוק למשימות, תלויות, הערכות מאמץ |
| [docs/performance.md](docs/performance.md) | מדידות ביצועים, Query Plan, אינדקסים |
| [docs/concurrency.md](docs/concurrency.md) | Optimistic Concurrency + Bulk Update |
| [docs/caching.md](docs/caching.md) | אסטרטגיית Cache + Invalidation |
| [docs/decisions.md](docs/decisions.md) | החלטות טכנולוגיות, מגבלות, שימוש ב-AI |

---

## תמצית הדרישות שמומשו

- ✅ שליפה עם Pagination, Filtering (6 שדות + טווח תאריכים), חיפוש טקסטואלי, מיון (6 שדות), 2 Aggregations — הכל בצד השרת
- ✅ עדכון סטטוס עם Optimistic Concurrency (RowVersion → 409)
- ✅ היסטוריית שינויים (Audit)
- ✅ Bulk Update (עד 100, Partial Success)
- ✅ Angular: Debounce, ביטול בקשות (switchMap), מצבי Loading/Empty/Error, טיפול ב-409
- ✅ Cache עם Expiration + Invalidation
- ✅ 22 בדיקות אוטומטיות כולל Integration
- ✅ Global Error Handling + Logging
