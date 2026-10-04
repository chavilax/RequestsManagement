# Concurrency ו-Bulk Update

## Optimistic Concurrency

### הבעיה: Lost Update
כאשר שני משתמשים קוראים את אותה פנייה ושניהם מעדכנים, העדכון השני "דורס" את הראשון ללא ידיעה. במערכת רב-משתמשים זו בעיה אמיתית.

### הפתרון
כל פנייה נושאת עמודת `RowVersion` מסוג `rowversion` של SQL Server — ערך שה-DB מעדכן **אוטומטית** בכל `UPDATE`.

**הזרימה:**
1. הלקוח שולף פנייה ומקבל את ה-`RowVersion` (מקודד Base64).
2. בעדכון, הלקוח מחזיר את ה-`RowVersion` שקיבל.
3. EF Core מגדיר את הערך כ-`OriginalValue` ומוסיף אותו לתנאי ה-`WHERE` של ה-UPDATE:
   ```sql
   UPDATE Requests SET Status=@s, UpdatedAt=@u
   WHERE Id=@id AND RowVersion=@originalRowVersion
   ```
4. אם מישהו עדכן בינתיים, ה-`RowVersion` ב-DB השתנה → **0 שורות מושפעות** → EF זורק `DbUpdateConcurrencyException` → מתורגם ל-**409 Conflict**.

### נקודה מרכזית
ה-Concurrency **נאכף במסד הנתונים**, לא ב-UI. אפילו אם קוראים ישירות ל-API, המנגנון עובד. העדכון של `Status`, `UpdatedAt` ורשומת ה-Audit מתבצעים באותה טרנזקציה — עקביות מלאה.

### קודי תגובה
| מצב | קוד |
|---|---|
| עדכון תקין | 200 OK |
| פנייה לא קיימת | 404 Not Found |
| מעבר סטטוס אסור | 400 Bad Request |
| עדכון מתחרה | 409 Conflict |

### מעברי סטטוס מותרים
```
New        → InProgress, Waiting
InProgress → Waiting, Completed
Waiting    → InProgress, Completed
Completed  → (סופי, אין מעברים)
```

---

## Bulk Update

### הדרישה
עדכון סטטוס של עד 100 פניות בבקשה אחת.

### הבחירה: Partial Success (ולא All-or-Nothing)

**נבחר Partial Success.** נימוק: בסביבה ארגונית, כישלון של פנייה אחת (למשל שעודכנה בינתיים או שמעבר הסטטוס אליה אינו חוקי) **לא צריך לבטל** את עדכון 99 הפניות התקינות. חשוב יותר שהלקוח יקבל תמונה מלאה.

**All-or-Nothing** היה מתאים לפעולה פיננסית טרנזקציונית, אך כאן הוא פחות שימושי ועלול לתסכל את המשתמש.

### ההתנהגות
כל פנייה מטופלת ונשמרת **בנפרד**. התשובה מפרטת מה הצליח ומה נכשל:

```json
{
  "totalRequested": 5,
  "succeededCount": 3,
  "failedCount": 2,
  "succeeded": [20054, 13041, 64812],
  "failed": [
    { "requestId": 47051, "reason": "InvalidTransition" },
    { "requestId": 99999, "reason": "NotFound" }
  ]
}
```

**סיבות כישלון אפשריות:** `NotFound`, `InvalidTransition`, `ConcurrencyConflict`.

### אכיפת הגבול
מגבלת 100 הפניות נאכפת בשכבת ה-BL (`MaxBulkSize`), כך שגם קריאה ישירה ל-API (עוקפת UI) נדחית עם 400 אם חורגים.
