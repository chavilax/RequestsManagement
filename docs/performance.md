# ביצועים ואינדקסים

מסמך זה מתעד כיצד נבחנה יעילות שתי פעולות ה-API המרכזיות על בסיס **100,000 רשומות**, אילו אינדקסים נוספו ומדוע, וכיצד נמנעת טעינת כלל הנתונים לזיכרון.

> המדידות בוצעו מול SQL Server LocalDB עם 100,000 רשומות באמצעות `SET STATISTICS TIME` ו-`SET SHOWPLAN_TEXT`.

---

## עקרון מנחה: עיבוד בצד השרת

כל פעולות הסינון, החיפוש, המיון, הדפדוף וה-Aggregations מתבצעות **בבסיס הנתונים** דרך `IQueryable` של EF Core, ומתורגמות ל-SQL יחיד. אין טעינה של כלל הרשומות לזיכרון:

- `AsNoTracking()` — אין change tracking בשליפות קריאה בלבד
- `CountAsync()` — הספירה מתבצעת ב-DB על ה-query המסונן, לפני הדפדוף
- `Skip().Take()` — רק העמוד המבוקש נמשך (לכל היותר 100 רשומות), לא 100,000
- `GroupBy()` — ה-Aggregations מחושבים ב-DB עם `GROUP BY`, לא בזיכרון

---

## פעולה 1: שליפה מסוננת + ממוינת + מדופדפת

**Endpoint:** `GET /api/requests?status=1&priority=2&sortBy=createdat&sortDescending=true`

**ה-SQL שנוצר (מהותית):**
```sql
SELECT TOP 20 Id, Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, RowVersion
FROM Requests
WHERE Status = 1 AND Priority = 2
ORDER BY CreatedAt DESC;
```

**Execution Plan (SHOWPLAN):**
```
|--Index Seek(OBJECT:([Requests].[IX_Requests_Status_CreatedAt]),
   SEEK:([Status]=(1)) ORDERED BACKWARD)
```

**ניתוח:**
- ה-DB מבצע **Index Seek** (ולא Table Scan) על `IX_Requests_Status_CreatedAt`
- `ORDERED BACKWARD` — האינדקס משרת גם את המיון (`CreatedAt DESC`) ללא פעולת Sort נפרדת
- **זמן מדוד:** ~0–1ms לשליפת עמוד מתוך 100K רשומות

**הסיבה שזה יעיל:** האינדקס המורכב `(Status, CreatedAt)` תומך בו-זמנית בסינון לפי Status ובמיון לפי CreatedAt. ה-DB "קופץ" ישירות לרשומות הרלוונטיות ומחזיר אותן כבר ממוינות.

---

## פעולה 2: Aggregations (נתונים מסכמים)

**Endpoint:** `GET /api/requests/stats`

**ה-SQL שנוצר:**
```sql
SELECT Status, COUNT(*) FROM Requests GROUP BY Status;
SELECT Priority, COUNT(*) FROM Requests GROUP BY Priority;
```

**ניתוח:**
- **זמן מדוד (ללא cache):** ~15–25ms עבור GROUP BY על 100K רשומות
- **זמן מדוד (עם cache):** ~5ms — שיפור של פי ~60

**אופטימיזציה — Cache:** מכיוון ש-GROUP BY על כל הטבלה יקר יחסית, והנתון נקרא בתדירות גבוהה (רענון דשבורד), הוא נשמר ב-`IMemoryCache` עם תפוגה של 2 דקות ו-Invalidation בכל עדכון סטטוס. ראה `docs/caching.md`.

---

## האינדקסים שנוספו ומדוע

| אינדקס | עמודות | משרת |
|---|---|---|
| `IX_Requests_Status_CreatedAt` | (Status, CreatedAt) | סינון לפי סטטוס + מיון לפי תאריך — התרחיש הנפוץ ביותר |
| `IX_Requests_Priority` | Priority | סינון לפי עדיפות |
| `IX_Requests_AssignedTo` | AssignedTo | סינון לפי מטפל |
| `IX_Requests_OrganizationName` | OrganizationName | סינון/חיפוש לפי שם ארגון |
| `IX_Requests_CreatedAt` | CreatedAt | מיון לפי תאריך ללא סינון סטטוס |
| `IX_RequestStatusHistory_RequestId_ChangedAt` | (RequestId, ChangedAt) | שליפת היסטוריה של פנייה, ממוינת |

---

## Bottleneck אפשרי והצעת שיפור

**ה-Bottleneck:** החיפוש הטקסטואלי (`WHERE Title LIKE '%term%' OR OrganizationName LIKE '%term%'`).

**זמן מדוד:** ~20ms — איטי יחסית לשליפה המסוננת (~1ms).

**הסיבה:** דפוס `LIKE '%term%'` (wildcard בהתחלה) **אינו יכול להשתמש באינדקס רגיל** (B-Tree), ולכן ה-DB מבצע scan. על 100K זה עדיין סביר, אך לא ישתרג טוב ליותר רשומות.

**הצעת שיפור:**
- מעבר ל-**Full-Text Search** של SQL Server (`CONTAINS`) שמשתמש באינדקס טקסט ייעודי
- לחלופין, אינדקס מחושב / עמודת חיפוש מנורמלת

לא יושם כעת כי על 100K הביצועים מספקים, והוספת Full-Text Search הייתה Over-Engineering ביחס לדרישה.
