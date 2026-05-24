# 🏥 منصة اطمئن (Etmen) — DEPI Graduation Project

> **منصة صحية متكاملة** تربط المريض بالطبيب وتتيح المتابعة الصحية الشاملة مع نظام إدارة الأزمات الصحية.

---

## 📋 نظرة عامة

**اطمئن** هي منصة رعاية صحية مبنية بـ ASP.NET Core MVC تستهدف:

- **المريض** — متابعة حالته الصحية، حجز المواعيد، رفع نتائج التحاليل، تقييم المخاطر.
- **الطبيب** — إدارة المواعيد، الاطلاع على الملفات الطبية، إدارة أوقات الفراغ.
- **الإداري / Crisis Admin** — إدارة المستخدمين، مراقبة منصات الرعاية، إدارة الأزمات الصحية.
- **Hospital Staff** — استقبال طلبات الطوارئ ومتابعة قوائم الانتظار.

---

## 🗂️ هيكل المشروع

```
Etmen_DEPI_Project/
│
├── Etmen_Domain/          ← طبقة الـ Entities والـ Enums
│   └── Entities/          ← كل موديلات قاعدة البيانات
│
├── Etmen_DAL/             ← طبقة الوصول للبيانات (Data Access Layer)
│   ├── Configurations/    ← إعدادات EF Core لكل Entity (Fluent API)
│   ├── DbContext/         ← EtmenDbContext
│   ├── Migrations/        ← ملفات الـ Migrations
│   ├── Repositories/
│   │   ├── Interfaces/    ← عقود الـ Repositories (IGenericRepository, IUnitOfWork, ...)
│   │   └── Implementations/ ← تنفيذ الـ Repositories + UnitOfWork
│   └── Seeder/            ← DataSeeder (seed البيانات الأولية)
│
├── Etmen_BLL/             ← طبقة الـ Business Logic
│   ├── DTOs/              ← Data Transfer Objects مقسمة حسب الـ Feature
│   ├── Helpers/           ← ServiceResult, PaginatedResult, BmiHelper, RiskCalculatorHelper
│   ├── Mapping/           ← BLLMappingProfile (Mapster)
│   └── Repositories/
│       ├── IServices/     ← عقود الـ Services (interfaces)
│       └── Services/      ← تنفيذ الـ Services ← 🔴 المطلوب التنفيذ
│
└── Etmen_PL/              ← طبقة العرض (Presentation Layer)
    ├── Controllers/       ← AccountController, HomeController, PatientController
    ├── Models/            ← ViewModels
    └── Views/             ← Razor Views
```

---

## 🔧 التقنيات المستخدمة

| التقنية | الاستخدام |
|---|---|
| **ASP.NET Core MVC (.NET 10)** | الـ Framework الأساسي |
| **Entity Framework Core 10** | ORM للتواصل مع قاعدة البيانات |
| **SQL Server (LocalDB)** | قاعدة البيانات |
| **ASP.NET Core Identity** | إدارة المستخدمين والأدوار |
| **Mapster 10** | Mapping بين Entities وDTOs |
| **Cookie Authentication** | نظام الجلسات |

---

## 🗃️ الـ Entities الموجودة في `Etmen_Domain`

| Entity | الوصف |
|---|---|
| `ApplicationUser` | المستخدم الأساسي (extends IdentityUser) — يحتوي على FirstName, LastName, IsActive, VerificationToken |
| `PatientProfile` | بيانات المريض التفصيلية — الطول، الوزن، فصيلة الدم، الأمراض المزمنة، BMI (محسوب) |
| `DoctorProfile` | بيانات الطبيب — التخصص، رقم الترخيص، رسوم الاستشارة |
| `Appointment` | الموعد بين مريض وطبيب |
| `AvailableSlot` | أوقات فراغ الطبيب المتاحة للحجز |
| `MedicalRecord` | السجل الطبي للمريض |
| `LabResult` | نتيجة تحليل مخبري مرفوعة |
| `RiskAssessment` | نتيجة تقييم المخاطر الصحية للمريض |
| `Alert` | تنبيه للمستخدم |
| `Notification` | إشعار للمستخدم |
| `ChatMessage` | رسالة في محادثة بين مستخدمين |
| `FamilyLink` | ربط أفراد الأسرة بملف المريض |
| `EmergencyRequest` | طلب طوارئ |
| `HealthcareProvider` | مستشفى أو مركز رعاية صحي (للبحث الجغرافي) |
| `CrisisConfiguration` | إعداد الأزمة الصحية (مع الأعراض والحدود) |
| `OutbreakZone` | منطقة تفشي وباء داخل أزمة |
| `SymptomWeight` | وزن العرض في نظام تقييم المخاطر |

---

## 👥 الأدوار في النظام (Roles)

| Role | الصلاحيات |
|---|---|
| `Patient` | Dashboard، المواعيد، السجل الطبي، التحاليل، تقييم المخاطر، الطوارئ |
| `Doctor` | Dashboard، إدارة المواعيد، إضافة سجلات طبية، إدارة الأوقات |
| `Admin` | إدارة المستخدمين والمزودين، الإحصائيات، إعدادات النظام |
| `CrisisAdmin` | إدارة الأزمات الصحية والإقرار بها |
| `HospitalStaff` | استقبال طلبات الطوارئ وقوائم الانتظار |

---

## 🔑 الـ Services المطلوب تنفيذها

> **⚠️ ملاحظة مهمة:** كل الـ Services التالية موجودة كـ Stubs تـ throw `NotImplementedException` — المطلوب تنفيذ كل method فيها.
>
> **✅ استثناء:** `AuthService` و `AccountController` و `HomeController` منفذين بالكامل — **لا تعدل عليهم**.

---

### 1. `PatientService` ← `IPatientService`

**المسؤولية:** كل العمليات المتعلقة بملف المريض.

```csharp
// Profile
Task<ServiceResult<ProfileDto>> GetProfileAsync(string userId);
Task<ServiceResult<ProfileDto>> UpdateProfileAsync(string userId, ProfileDto dto);

// Dashboard
Task<ServiceResult<DashboardDto>> GetDashboardAsync(string userId);

// Medical Records
Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetMedicalRecordsAsync(string userId);
Task<ServiceResult<MedicalRecordDto>> GetLatestMedicalRecordAsync(string userId);
Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordAsync(string userId, MedicalRecordCreateDto dto);

// Risk Assessment
Task<ServiceResult<RiskResultDto>> AssessRiskAsync(string userId, RiskInputDto input);
Task<ServiceResult<RiskResultDto>> GetLatestRiskAssessmentAsync(string userId);
Task<ServiceResult<IEnumerable<RiskResultDto>>> GetRiskHistoryAsync(string userId);
```

**ملاحظات التنفيذ:**
- استخدم `_uow.PatientProfiles.GetByUserIdAsync(userId)` للوصول للبروفايل.
- دالة `AssessRiskAsync` تستدعي `RiskCalculatorHelper` الموجود في `Helpers/`.
- BMI محسوب تلقائياً في الـ Entity لا تحتاج لحسابه.

---

### 2. `DoctorService` ← `IDoctorService`

**المسؤولية:** إدارة ملف الطبيب، المواعيد، وأوقات الفراغ.

```csharp
// Profile
Task<ServiceResult<DoctorProfileDto>> GetProfileAsync(string userId);
Task<ServiceResult<DoctorProfileDto>> UpdateProfileAsync(string userId, DoctorProfileDto dto);

// Dashboard & Stats
Task<ServiceResult<DoctorDashboardDto>> GetDashboardAsync(string userId);
Task<ServiceResult<DoctorStatisticsDto>> GetStatisticsAsync(string userId);

// Slots
Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId);
Task<ServiceResult<AvailableSlotDto>> AddSlotAsync(string userId, CreateAvailableSlotDto dto);
Task<ServiceResult> BulkAddSlotsAsync(string userId, BulkCreateSlotsDto dto);
Task<ServiceResult> DeleteSlotAsync(string userId, int slotId);

// Appointments (Doctor View)
Task<ServiceResult<IEnumerable<DoctorAppointmentDto>>> GetAppointmentsAsync(string userId);
Task<ServiceResult<DoctorAppointmentDto>> GetAppointmentAsync(string userId, int appointmentId);
Task<ServiceResult> UpdateAppointmentStatusAsync(string userId, int appointmentId, UpdateAppointmentStatusDto dto);

// Patient Records
Task<ServiceResult<IEnumerable<PatientSearchDto>>> SearchPatientsAsync(string searchTerm);
Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordForPatientAsync(string doctorUserId, MedicalRecordCreateDto dto);
```

---

### 3. `AppointmentService` ← `IAppointmentService`

**المسؤولية:** حجز وإدارة المواعيد من جانب المريض.

```csharp
Task<ServiceResult<AppointmentDto>> BookAppointmentAsync(string userId, BookingRequestDto dto);
Task<ServiceResult<IEnumerable<AppointmentDto>>> GetPatientAppointmentsAsync(string userId);
Task<ServiceResult<AppointmentDto>> GetAppointmentByIdAsync(string userId, int appointmentId);
Task<ServiceResult> CancelAppointmentAsync(string userId, int appointmentId);
Task<ServiceResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlotsAsync(int doctorId, DateTime date);
Task<ServiceResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointmentsAsync(string userId);
```

**ملاحظات التنفيذ:**
- عند الحجز تأكد إن الـ Slot متاح (غير محجوز) قبل إنشاء الـ Appointment.
- عند الإلغاء أرجع الـ Slot لحالة متاح.

---

### 4. `MedicalRecordService` ← `IMedicalRecordService`

**المسؤولية:** CRUD الكامل للسجلات الطبية.

```csharp
Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByPatientAsync(string userId);
Task<ServiceResult<MedicalRecordDto>> GetByIdAsync(string userId, int recordId);
Task<ServiceResult<MedicalRecordDto>> GetLatestAsync(string userId);
Task<ServiceResult<MedicalRecordDto>> CreateAsync(string userId, MedicalRecordCreateDto dto);
Task<ServiceResult> DeleteAsync(string userId, int recordId);
Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetWithAbnormalValuesAsync(string userId);
```

---

### 5. `LabService` ← `ILabService`

**المسؤولية:** رفع وإدارة نتائج التحاليل المخبرية.

```csharp
Task<ServiceResult<LabResultDto>> GetLabResultByIdAsync(int labResultId);
Task<ServiceResult<List<LabResultDto>>> GetPatientLabResultsAsync(int patientId);
Task<ServiceResult<List<LabResultDto>>> GetLabResultsByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate);
Task<ServiceResult<LabResultDto>> UploadLabResultAsync(LabUploadDto dto);
Task<ServiceResult> UpdateLabResultAsync(int labResultId, LabUploadDto dto);
Task<ServiceResult> DeleteLabResultAsync(int labResultId);
Task<ServiceResult<Dictionary<string, object>>> AnalyzeLabResultsAsync(int patientId);
Task<ServiceResult<List<LabResultDto>>> GetAbnormalResultsAsync(int patientId);
Task<ServiceResult<List<LabResultDto>>> SearchLabResultsAsync(string testName, int pageNumber = 1, int pageSize = 10);
Task<ServiceResult<Dictionary<string, object>>> GetLabStatisticsAsync();
Task<ServiceResult> VerifyLabResultAsync(int labResultId);
Task<ServiceResult> RejectLabResultAsync(int labResultId, string reason);
```

**ملاحظات التنفيذ:**
- File upload يستخدم Azure OCR (إعداداتها في `appsettings.json` تحت `Azure`).
- `AnalyzeLabResultsAsync` تقارن القيم بالنطاقات الطبيعية.

---

### 6. `RiskService` ← `IRiskService`

**المسؤولية:** حساب وحفظ تقييم المخاطر الصحية.

```csharp
Task<ServiceResult<RiskResultDto>> CalculateRiskAsync(RiskInputDto dto);
Task<ServiceResult<List<RiskResultDto>>> GetPatientRiskHistoryAsync(int patientProfileId);
Task<ServiceResult> SaveRiskAssessmentAsync(int patientProfileId, RiskResultDto riskResult);
```

**ملاحظات التنفيذ:**
- `CalculateRiskAsync` يستخدم `RiskCalculatorHelper` في `Etmen_BLL/Helpers/`.
- احفظ كل تقييم في `RiskAssessment` entity.

---

### 7. `AlertService` ← `IAlertService`

**المسؤولية:** إنشاء وإدارة التنبيهات للمستخدم.

```csharp
Task<ServiceResult<List<AlertDto>>> GetUserAlertsAsync(string userId);
Task<ServiceResult<List<AlertDto>>> GetUnreadAlertsAsync(string userId);
Task<ServiceResult<AlertDto>> GetAlertByIdAsync(int alertId);
Task<ServiceResult<AlertDto>> CreateAlertAsync(int userId, string title, string message, string alertType);
Task<ServiceResult> MarkAsReadAsync(int alertId);
Task<ServiceResult> MarkAllAsReadAsync(string userId);
Task<ServiceResult> DeleteAlertAsync(int alertId);
Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
```

---

### 8. `NotificationService` ← `INotificationService`

**المسؤولية:** إرسال وإدارة الإشعارات (مواعيد، طوارئ، أزمات، ...).

```csharp
// CRUD
Task<ServiceResult<NotificationDto>> GetNotificationByIdAsync(int notificationId);
Task<ServiceResult<List<NotificationDto>>> GetUserNotificationsAsync(int userId, int pageNumber, int pageSize);
Task<ServiceResult<NotificationDto>> CreateNotificationAsync(int userId, string title, string message, string type);
Task<ServiceResult> MarkAsReadAsync(int notificationId);
Task<ServiceResult> MarkAllAsReadAsync(int userId);
Task<ServiceResult> DeleteNotificationAsync(int notificationId);

// Sending
Task<ServiceResult> SendAppointmentReminderAsync(int appointmentId);
Task<ServiceResult> SendAlertNotificationAsync(int alertId);
Task<ServiceResult> SendEmergencyNotificationAsync(int emergencyRequestId);
Task<ServiceResult> SendCrisisAlertAsync(int crisisId, List<int> userIds);
Task<ServiceResult> SendFamilyInvitationAsync(int familyLinkId);

// Bulk
Task<ServiceResult> SendBulkNotificationAsync(List<int> userIds, string title, string message);
Task<ServiceResult> ClearExpiredNotificationsAsync();

// Stats
Task<ServiceResult<int>> GetUnreadCountAsync(int userId);
Task<ServiceResult<Dictionary<string, int>>> GetNotificationStatisticsAsync();
```

---

### 9. `CrisisService` ← `ICrisisService`

**المسؤولية:** إدارة الأزمات الصحية (للأدمن) وعرضها (للكل).

```csharp
// Read (كل المستخدمين)
Task<ServiceResult<CrisisConfigurationDto>> GetActiveCrisisAsync();
Task<ServiceResult<List<CrisisConfigurationDto>>> GetAllCrisesAsync();
Task<ServiceResult<CrisisConfigurationDto>> GetCrisisByIdAsync(int crisisId);
Task<ServiceResult<CrisisStatsDto>> GetCrisisStatsAsync(int crisisId);

// Admin فقط
Task<ServiceResult<CrisisConfigurationDto>> CreateCrisisAsync(CreateCrisisDto dto);
Task<ServiceResult<CrisisConfigurationDto>> UpdateCrisisAsync(int crisisId, EditCrisisDto dto);
Task<ServiceResult> ActivateCrisisAsync(int crisisId);
Task<ServiceResult> DeactivateCrisisAsync(int crisisId);
Task<ServiceResult> DeleteCrisisAsync(int crisisId);

// Symptoms
Task<ServiceResult> AddSymptomAsync(int crisisId, SymptomWeightDto symptomDto);
Task<ServiceResult> AddMultipleSymptomsAsync(int crisisId, List<SymptomWeightDto> symptomsDto);
Task<ServiceResult> UpdateSymptomAsync(int crisisId, string symptomName, SymptomWeightDto updatedSymptomDto);
Task<ServiceResult> RemoveSymptomAsync(int crisisId, string symptomName);
Task<ServiceResult<List<SymptomWeightDto>>> GetSymptomsByCrisisAsync(int crisisId);

// Risk Thresholds
Task<ServiceResult> UpdateRiskThresholdsAsync(int crisisId, decimal? emergencyThreshold, decimal? highRiskThreshold, decimal? mediumRiskThreshold);
```

**ملاحظات التنفيذ:**
- لا يجوز أن تكون أكثر من أزمة واحدة نشطة في نفس الوقت.
- عند `ActivateCrisisAsync` اعمل deactivate للأزمات الأخرى النشطة أولاً.

---

### 10. `CrisisRiskEngineService` ← `ICrisisRiskEngineService`

**المسؤولية:** حساب مخاطر الأزمة على المريض وحساب احتمالية التفشي.

```csharp
Task<ServiceResult<CrisisRiskResultDto>> CalculateCrisisRiskAsync(int patientProfileId, int crisisConfigurationId);
Task<ServiceResult<decimal>> CalculateOutbreakProbabilityAsync(decimal latitude, decimal longitude, int crisisConfigurationId);
Task<ServiceResult<List<OutbreakZoneDto>>> GetPatientsInZoneAsync(int crisisConfigurationId);
```

**ملاحظات التنفيذ:**
- يستخدم أوزان الأعراض (`SymptomWeight`) من الـ `CrisisConfiguration`.
- الحدود المرجعية في `appsettings.json` تحت `CrisisConfig`.

---

### 11. `EmergencyService` ← `IEmergencyService`

**المسؤولية:** إنشاء ومتابعة طلبات الطوارئ.

```csharp
Task<ServiceResult<EmergencyRequestDto>> CreateEmergencyRequestAsync(EmergencyRequestDto dto);
Task<ServiceResult<EmergencyRequestDto>> GetEmergencyRequestAsync(int requestId);
Task<ServiceResult<List<EmergencyTrackingDto>>> GetPendingEmergenciesAsync();
Task<ServiceResult> UpdateEmergencyStatusAsync(int requestId, EmergencyUpdateDto dto);
Task<ServiceResult<HospitalQueueDto>> GetHospitalQueueAsync();
```

---

### 12. `FamilyService` ← `IFamilyService`

**المسؤولية:** ربط أفراد الأسرة بملف المريض عبر دعوات.

```csharp
Task<ServiceResult<FamilyDto>> InviteFamilyMemberAsync(FamilyInviteDto dto);
Task<ServiceResult> AcceptFamilyInviteAsync(string inviteToken);
Task<ServiceResult<List<FamilyDto>>> GetFamilyMembersAsync(int patientProfileId);
Task<ServiceResult> RemoveFamilyMemberAsync(int familyLinkId);
Task<ServiceResult> UpdateFamilyPermissionsAsync(int familyLinkId, FamilyDto dto);
```

**ملاحظات التنفيذ:**
- `InviteFamilyMemberAsync` يُنشئ `FamilyLink` بحالة Pending وتوكن دعوة.
- `AcceptFamilyInviteAsync` يُفعّل الـ FamilyLink باستخدام التوكن.

---

### 13. `NearbyService` ← `INearbyService`

**المسؤولية:** البحث عن مقدمي الرعاية الصحية القريبين جغرافياً.

```csharp
Task<ServiceResult<List<ProviderDto>>> SearchNearbyProvidersAsync(NearbySearchDto dto);
Task<ServiceResult<List<AvailableSlotDto>>> GetAvailableSlotsByProviderAsync(int providerId);
Task<ServiceResult> BookAppointmentAsync(BookingRequestDto dto);
```

**ملاحظات التنفيذ:**
- استخدم الـ `Latitude` و`Longitude` من `NearbySearchDto` مع نصف القطر من `appsettings.json` (`Geo:DefaultSearchRadiusInKm`).
- حساب المسافة: Haversine Formula أو استخدام EF Core Geography.

---

### 14. `AIChatService` ← `IAIChatService`

**المسؤولية:** الشات الصحي المبني على AI.

```csharp
Task<ServiceResult<ChatMessageDto>> SendMessageAsync(int userId, string message);
Task<ServiceResult<ChatThreadDto>> GetChatThreadAsync(int userId);
Task<ServiceResult<List<ChatMessageDto>>> GetChatHistoryAsync(int userId, int pageNumber, int pageSize);
Task<ServiceResult> ClearChatHistoryAsync(int userId);
```

**ملاحظات التنفيذ:**
- استخدم إعدادات الـ AI من `appsettings.json` تحت `AI` (endpoint, API key, model).
- خزّن المحادثات في `ChatMessage` entity.

---

### 15. `AdminService` ← `IAdminService`

**المسؤولية:** لوحة تحكم الأدمن الكاملة.

```csharp
// User Management
GetAllUsersAsync / GetUserByIdAsync / UpdateUserStatusAsync / BulkUserActionAsync / DeleteUserAsync

// Provider Management
GetAllProvidersAsync / GetProviderByIdAsync / CreateProviderAsync / UpdateProviderAsync / DeleteProviderAsync

// Dashboard & Reports
GetDashboardAsync / GetReportsAsync / GetCrisisManagementAsync / GetActivityLogsAsync

// System Config
GetSystemConfigAsync / UpdateSystemConfigAsync

// Crisis Actions
ApproveCrisisAsync / RejectCrisisAsync / UpdateCrisisStatusAsync
```

---

## 🗄️ الـ Configurations المطلوب تنفيذها (DAL)

كل ملفات `Etmen_DAL/Configurations/` تحتوي على stub — المطلوب تعبئة `Configure(EntityTypeBuilder<T> builder)` بالـ Fluent API الصحيح لكل Entity.

| ملف | Entity |
|---|---|
| `ApplicationUserConfig.cs` | ApplicationUser |
| `PatientProfileConfig.cs` | PatientProfile — لاحظ: BMI محسوب Computed Column |
| `DoctorProfileConfig.cs` | DoctorProfile |
| `AppointmentConfig.cs` | Appointment — علاقة Patient → Doctor |
| `AvailableSlotConfig.cs` | AvailableSlot — علاقة Doctor |
| `MedicalRecordConfig.cs` | MedicalRecord |
| `LabResultConfig.cs` | LabResult |
| `RiskAssessmentConfig.cs` | RiskAssessment |
| `AlertConfig.cs` | Alert |
| `NotificationConfig.cs` | Notification |
| `ChatMessageConfig.cs` | ChatMessage — Sender/Receiver |
| `FamilyLinkConfig.cs` | FamilyLink — Self-referencing |
| `EmergencyRequestConfig.cs` | EmergencyRequest |
| `HealthcareProviderConfig.cs` | HealthcareProvider |
| `CrisisConfigurationConfig.cs` | CrisisConfiguration — مع Symptoms JSON |
| `OutbreakZoneConfig.cs` | OutbreakZone |

---

## 🎮 الـ Controllers المطلوب استكمالها

### `PatientController.cs`
الـ actions موجودة كـ stubs. كل action تستدعي الـ service المناسب وتمرر النتيجة للـ View.

| Action | Service |
|---|---|
| `Dashboard` | `IPatientService.GetDashboardAsync` |
| `Profile` (GET/POST) | `IPatientService.GetProfileAsync / UpdateProfileAsync` |
| `Appointments` | `IAppointmentService.GetPatientAppointmentsAsync` |
| `Book` (GET/POST) | `INearbyService` + `IAppointmentService` |
| `MedicalRecords` (GET/POST) | `IMedicalRecordService` |
| `LabResults` (GET/POST) | `ILabService` |
| `RiskAssessment` (GET/POST) | `IPatientService.AssessRiskAsync` |
| `Alerts` | `IAlertService` |
| `Nearby` | `INearbyService` |

---

## 🛠️ تشغيل المشروع

### المتطلبات
- .NET 10 SDK
- SQL Server / LocalDB
- Visual Studio 2022+ أو VS Code

### خطوات التشغيل

```bash
# 1. Clone أو extract المشروع
# 2. عدّل Connection String في appsettings.json
# 3. نفّذ المشروع — الـ Migrations بتتطبق تلقائياً عند أول تشغيل

dotnet run --project Etmen_PL
```

### Connection String الافتراضية
```json
"DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=EtmenDB;Integrated Security=True"
```

---

## 🏗️ نمط `ServiceResult<T>`

كل الـ Services بترجع `ServiceResult<T>` — اتعرف على الـ patterns المتاحة في `Etmen_BLL/Helpers/ServiceResult.cs`:

```csharp
// Success
ServiceResult<T>.Success(data)
ServiceResult.Success()

// Errors
ServiceResult<T>.NotFound("message")
ServiceResult<T>.Conflict("message")
ServiceResult<T>.Failure("message")
ServiceResult<T>.Unauthorized("message")
```

---

## 🔒 الأمان

- الـ Cookies: HttpOnly + SecurePolicy = Always
- Anti-Forgery Token على كل POST
- Rate Limiting على Login
- Account Lockout بعد 5 محاولات خاطئة
- Email Verification مطلوبة قبل الدخول

---

## 🗺️ إعدادات `appsettings.json`

| Section | الاستخدام |
|---|---|
| `ConnectionStrings` | SQL Server |
| `Email` | SMTP لإرسال الإيميلات |
| `SendGrid` | بديل SMTP |
| `Azure.OcrApiKey` | Azure Cognitive Services للـ OCR |
| `AI` | Azure OpenAI للشات الصحي |
| `CrisisConfig` | حدود مستويات الخطر الافتراضية |
| `FileUpload` | الحد الأقصى لحجم الملفات والامتدادات المسموحة |
| `Geo` | نصف القطر الافتراضي للبحث الجغرافي |
| `Features` | تفعيل/تعطيل features معينة |
| `Security` | مدة الـ Cookie ومحاولات الـ Login |

---

## ✅ ما تم تنفيذه مسبقاً (لا تعدل عليه)

| الملف | الوصف |
|---|---|
| `AuthService.cs` | Register, Login, Verify Email, Forgot/Reset Password — مكتمل |
| `AccountController.cs` | كل flows الـ Authentication — مكتمل |
| `HomeController.cs` | Landing Page + Error Pages — مكتمل |
| `DTOs/` كاملاً | كل الـ Data Transfer Objects — لا تعدل |
| `Helpers/` كاملاً | ServiceResult, PaginatedResult, BmiHelper, RiskCalculatorHelper |
| `Mapping/BLLMappingProfile.cs` | كل الـ Mapster mappings |
| `Domain/Entities/` كاملاً | كل الـ Entities |
| `DAL/Repositories/` | Interfaces وImplementations للـ DAL Repositories |
| `Program.cs` | DI Registration وMiddleware Pipeline |

---

*مشروع تخرج DEPI — منصة اطمئن الصحية*
