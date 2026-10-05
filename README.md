<div dir="rtl" align="right">

# מערכת חיפוש וסינון בקשות

מערכת Web API ב-.NET 8 המאפשרת חיפוש וסינון בקשות עם תמיכה בפילטרים מרובים, דפדוף, מיון והרשאות ברמת השאילתה.

---

## הרצת המערכת

### דרישות מקדימות

| כלי | גרסה | קישור |
|-----|------|-------|
| .NET SDK | 8.0 | [הורדה](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Node.js | 18+ | [הורדה](https://nodejs.org/) |
| npm | 9+ | מגיע עם Node.js |

### הרצת ה-Backend

```bash
# מעבר לתיקיית ה-API
cd src/Requests.Api

# הרצה
dotnet run
```

**כתובות זמינות:**
- HTTP: `http://localhost:5050`
- Swagger UI: `http://localhost:5050/swagger`

### הרצת ה-Frontend

```bash
# מעבר לתיקיית Angular
cd src/requests-web

# התקנת תלויות (פעם ראשונה בלבד)
npm install

# הרצת שרת פיתוח
npm start
```

**כתובת:** `http://localhost:4200`

### הרצה במקביל

יש להריץ את ה-Backend וה-Frontend בשני טרמינלים נפרדים:

```bash
# טרמינל 1 - Backend
cd src/Requests.Api && dotnet run

# טרמינל 2 - Frontend
cd src/requests-web && npm start
```

### בדיקת הרשאות שונות

אפשר לשנות את ה-User Context דרך URL parameters:

| כתובת | משתמש | תפקיד |
|-------|-------|-------|
| `http://localhost:4200` | User 1 | רגיל |
| `http://localhost:4200?userId=5` | User 5 | רגיל |
| `http://localhost:4200?isAdmin=true` | User 1 | מנהל |
| `http://localhost:4200?userId=3&isAdmin=true` | User 3 | מנהל |

---

## הרצת בדיקות

```bash
# הרצת כל הבדיקות מתיקיית src
cd src
dotnet test

# הרצה עם פירוט מלא
dotnet test --logger "console;verbosity=detailed"

# הרצת בדיקות יחידה בלבד
dotnet test Requests.Application.Tests

# הרצת בדיקות אינטגרציה בלבד
dotnet test Requests.Api.Tests
```

### סיכום הבדיקות

| סוג | פרויקט | כמות | תיאור |
|-----|--------|------|-------|
| Unit Tests | `Requests.Application.Tests` | 4 | בדיקת לוגיקה עסקית (Authorization, Filtering, Pagination) |
| Integration Tests | `Requests.Api.Tests` | 7 | בדיקת API מקצה לקצה (Validation, Authorization, Sorting) |

**סה"כ: 11 מתודות בדיקה** (חלקן `[Theory]` עם מספר מקרי בדיקה)

---

## טכנולוגיות שנבחרו

### Backend

| טכנולוגיה | גרסה | מדוע נבחרה |
|-----------|------|------------|
| **.NET 8** | 8.0 | הגרסה החדשה ביותר עם ביצועים משופרים ו-LTS |
| **ASP.NET Core** | 8.0 | Framework סטנדרטי ל-Web API עם תמיכה מובנית ב-DI |
| **Entity Framework Core** | 8.0.17 | ORM עם תמיכה ב-IQueryable לתרגום שאילתות ל-SQL |
| **EF Core InMemory** | 8.0.17 | מאפשר בדיקות ופיתוח ללא התקנת DB |
| **FluentValidation** | 11.11.0 | ולידציה גמישה עם תחביר קריא והפרדה מ-DTOs |
| **Swashbuckle** | 6.8.1 | תיעוד API אוטומטי עם Swagger UI |

### Frontend

| טכנולוגיה | גרסה | מדוע נבחרה |
|-----------|------|------------|
| **Angular** | 18+ | Framework מודרני עם Signals, Standalone Components |
| **TypeScript** | 5.x | Type Safety ותחזוקתיות |
| **RxJS** | 7.x | ניהול Async עם Observables |

### בדיקות

| טכנולוגיה | גרסה | מדוע נבחרה |
|-----------|------|------------|
| **xUnit** | 2.9.2 | Framework בדיקות סטנדרטי ל-.NET |
| **FluentAssertions** | 6.12.2 | Assertions קריאים יותר |
| **WebApplicationFactory** | 8.0.2 | בדיקות אינטגרציה ללא צורך בשרת חיצוני |

---

## הנחות שבוצעו

### 1. סימולציית אימות משתמש
אימות מבוצע דרך HTTP Headers במקום מערכת Authentication מלאה:
- `X-User-Id` - מזהה המשתמש (מספר)
- `X-Is-Admin` - האם מנהל ("true"/"false")

### 2. מסד נתונים In-Memory
המערכת משתמשת ב-EF Core InMemory לצורך הדגמה. בפרודקשן יוחלף ב-SQL Server.

### 3. נתוני דוגמה
המערכת מאותחלת עם 15 בקשות לדוגמה בעלייה.

### 4. תאריכים ב-UTC
כל התאריכים נשמרים ומעובדים ב-UTC לעקביות.

### 5. חיפוש Case-Insensitive
חיפוש לפי מספר בקשה אינו רגיש לאותיות גדולות/קטנות.

### 6. ברירות מחדל לדפדוף
- עמוד: 1
- גודל עמוד: 20
- מקסימום לעמוד: 100

### 7. מיון ברירת מחדל
מיון לפי `CreatedAt` בסדר יורד (החדשים קודם).

---

## החלטות טכניות

### החלטה 1: IQueryable לסינון ברמת ה-Database

**הבחירה:** שימוש ב-`IQueryable<T>` לבניית שאילתות דינמיות שמתורגמות ל-SQL.

**למה:** הדרישה לטפל במיליוני רשומות מחייבת שהסינון יתבצע ב-Database ולא בזיכרון.

**אלטרנטיבה שנשקלה:** טעינת כל הרשומות לזיכרון וסינון עם LINQ to Objects.

**למה נדחתה:** לא סקלבילית - מיליון רשומות × 1KB = 1GB בזיכרון לכל Request.

**הערה לפרודקשן:** כדי לתמוך במיליוני רשומות, יוגדרו אינדקסים (DB Indexes) על השדות השכיחים בחיפוש ובסינון: `Status`, `CreatedAt`, `OwnerId`, `RequestNumber`.

```csharp
// הבחירה - נוצר SQL עם WHERE
query.Where(r => r.Status == status).ToListAsync();

// האלטרנטיבה - טוען הכל לזיכרון
context.Requests.ToList().Where(r => r.Status == status);
```

---

### החלטה 2: Clean Architecture עם 4 שכבות

**הבחירה:** הפרדה ל-Domain, Application, Infrastructure, API.

**למה:** 
- בדיקתיות - אפשר לבדוק Business Logic בלי DB
- תחזוקתיות - שינוי ב-Infrastructure לא משפיע על Application
- גמישות - אפשר להחליף DB או Framework

**אלטרנטיבה שנשקלה:** ארכיטקטורה פשוטה של 2 שכבות (API + Data).

**למה נדחתה:** קוד מעורבב, קשה לבדוק, תלויות הדדיות.

```
Domain (0 תלויות) ← Application ← Infrastructure ← API
```

---

### החלטה 3: FluentValidation במקום Data Annotations

**הבחירה:** `FluentValidation` לולידציה.

**למה:**
- הפרדת Validation מ-DTOs - קוד נקי יותר
- כללים מורכבים - `DateFrom <= DateTo` קל לכתוב
- בדיקתיות - אפשר לבדוק Validator בנפרד
- הודעות שגיאה - גמישות מלאה בניסוח

**אלטרנטיבה שנשקלה:** `[Required]`, `[Range]` ושאר Data Annotations.

**למה נדחתה:** מוגבלת לכללים פשוטים, קשה לבדוק, הודעות נוקשות.

```csharp
// FluentValidation - גמיש וקריא
RuleFor(x => x.DateTo)
    .GreaterThanOrEqualTo(x => x.DateFrom)
    .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
    .WithMessage("תאריך סיום חייב להיות אחרי תאריך התחלה");
```

---

### החלטה 4: Whitelist לשדות מיון

**הבחירה:** רשימה סגורה של שדות מותרים למיון.

**למה:** מניעת SQL Injection דרך פרמטר `sortBy`.

**אלטרנטיבה שנשקלה:** לקבל כל שם שדה מהמשתמש.

**למה נדחתה:** פרצת אבטחה - משתמש יכול להזריק קוד זדוני.

```csharp
private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
{
    "Id", "RequestNumber", "Status", "RequestType", "CreatedAt", "CustomerId", "OwnerId"
};
```

---

## מה לא הושלם והמשך

### ✅ הושלם

- [x] Backend מלא עם חיפוש, סינון, דפדוף ומיון
- [x] הרשאות ברמת השאילתה (Admin רואה הכל, User רואה שלו)
- [x] ולידציה עם FluentValidation
- [x] בדיקות יחידה ואינטגרציה (9 בדיקות)
- [x] תיעוד ארכיטקטורה (Microservices + Resilience)
- [x] Frontend ב-Angular עם חיפוש ודפדוף (בונוס)

### 🔲 לא הושלם

| פריט | עדיפות | הערכת זמן |
|------|--------|-----------|
| אימות אמיתי (JWT/OAuth) | גבוהה | 4-6 שעות |
| מסד נתונים אמיתי (SQL Server) | גבוהה | 2-3 שעות |
| Caching עם Redis | בינונית | 3-4 שעות |
| Logging מפורט (Serilog) | נמוכה | 1-2 שעות |

### איך הייתי ממשיך

**שלב 1 (מיידי):** החלפת InMemory ב-SQL Server עם Migrations.

**שלב 2 (קרוב):** הוספת JWT Authentication עם Refresh Tokens.

**שלב 3 (בהמשך):** Redis Cache לשאילתות נפוצות + Rate Limiting.

---

## מבנה הפרויקט

```
src/
├── Requests.Api/              # שכבת API - Controllers, Validators
├── Requests.Application/      # שכבת לוגיקה - Services, DTOs, Interfaces
├── Requests.Domain/           # שכבת Domain - Entities, Enums
├── Requests.Infrastructure/   # שכבת Data - DbContext, Repositories
├── Requests.Api.Tests/        # בדיקות אינטגרציה
├── Requests.Application.Tests/ # בדיקות יחידה
├── requests-web/              # Angular Frontend
├── ARCHITECTURE.md            # מסמך ארכיטקטורה (Microservices)
└── README.md                  # קובץ זה
```

---

## קבצים מרכזיים לסקירה

| קובץ | תפקיד |
|------|-------|
| `Requests.Api/Controllers/RequestsController.cs` | נקודת כניסה ל-API |
| `Requests.Api/Validators/SearchRequestQueryValidator.cs` | כללי ולידציה |
| `Requests.Application/Requests/RequestService.cs` | לוגיקה עסקית ובניית פילטרים |
| `Requests.Infrastructure/Repositories/RequestRepository.cs` | ביצוע שאילתות עם IQueryable |
| `requests-web/src/app/services/request-search.service.ts` | שירות HTTP ב-Angular |

</div>
