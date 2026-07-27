# مخطط الكيانات والعلاقات للمشروع (Entity-Relationship Diagram - ERD)

يحتوي هذا الملف على توثيق شامل لبنية قاعدة البيانات لمشروع **إطمن (Etmen)**. تم بناء قاعدة البيانات باستخدام تقنية **Entity Framework Core** و **SQL Server**، وتعتمد على نظام **ASP.NET Core Identity** لإدارة المستخدمين والصلاحيات.

---

## المخطط البصري (Visual ERD Diagram)

> [!TIP]
> يمكنك عرض هذا المخطط مباشرة في بيئات دعم Markdown التي تدعم رسم **Mermaid** مثل GitHub و VS Code.

```mermaid
erDiagram
    %% --- Identity / User Management ---
    ApplicationUser ||--o| PatientProfile : "has 1:1"
    ApplicationUser ||--o| DoctorProfile : "has 1:1"
    ApplicationUser ||--o| StaffProfile : "has 1:1"
    ApplicationUser ||--o{ Alert : "receives 1:N"
    ApplicationUser ||--o{ Notification : "receives 1:N"
    ApplicationUser ||--o{ ChatMessage : "sends 1:N"
    ApplicationUser ||--o{ ChatMessage : "receives 1:N"

    %% --- Patient Relationships ---
    PatientProfile ||--o{ MedicalRecord : "has 1:N"
    PatientProfile ||--o{ RiskAssessment : "undergoes 1:N"
    PatientProfile ||--o{ Appointment : "books 1:N"
    PatientProfile ||--o{ LabResult : "has 1:N"
    PatientProfile ||--o{ FamilyLink : "creates as primary 1:N"
    PatientProfile ||--o{ FamilyLink : "linked as member 1:N"
    PatientProfile ||--o{ EmergencyRequest : "initiates 1:N"
    PatientProfile ||--o{ Review : "writes 1:N"

    %% --- Doctor Relationships ---
    DoctorProfile ||--o{ Appointment : "conducts 1:N"
    DoctorProfile ||--o{ AvailableSlot : "manages 1:N"
    DoctorProfile ||--o{ DoctorProvider : "affiliates N:M"
    DoctorProfile ||--o{ Review : "receives 1:N"

    %% --- Healthcare Provider Relationships ---
    HealthcareProvider ||--o{ StaffProfile : "employs 1:N"
    HealthcareProvider ||--o{ DoctorProvider : "affiliates N:M"
    HealthcareProvider ||--o{ EmergencyRequest : "handles 1:N"
    HealthcareProvider ||--o{ Review : "receives 1:N"

    %% --- Staff & Logging ---
    StaffProfile ||--o{ StaffActivityLog : "logs actions 1:N"

    %% --- Crisis Configurations ---
    CrisisConfiguration ||--o{ OutbreakZone : "defines zones 1:N"
    CrisisConfiguration ||--o{ CrisisSymptomWeights : "configures weights 1:N"

    %% --- Emergency & Risk ---
    RiskAssessment ||--o| EmergencyRequest : "triggers 1:1"
    EmergencyRequest }o--o| ApplicationUser : "assigned to Doctor 1:N"
    EmergencyRequest }o--o| ApplicationUser : "responded by User 1:N"

    ApplicationUser {
        string Id PK
        string FirstName
        string LastName
        string ProfilePicture
        bool IsEmailVerified
        datetime EmailVerificationSentAt
        string VerificationToken
        datetime VerificationTokenExpiry
        string ResetPasswordToken
        datetime ResetPasswordTokenExpiry
        bool IsActive
        datetime CreatedAt
        datetime LastLoginAt
        bool MustChangePassword
    }

    PatientProfile {
        int Id PK
        string ApplicationUserId FK
        string FullName
        datetime DateOfBirth
        string Gender
        decimal Height
        decimal Weight
        int ActivityLevel
        string BloodType
        bool HasChronicDiseases
        string ChronicDiseasesNotes
        string Allergies
        string CurrentMedications
        decimal Latitude
        decimal Longitude
        datetime CreatedAt
        datetime UpdatedAt
    }

    DoctorProfile {
        int Id PK
        string ApplicationUserId FK
        string FullName
        string Specialization
        string LicenseNumber
        int YearsOfExperience
        string Bio
        decimal ConsultationFee
        bool IsAvailable
        datetime CreatedAt
        datetime UpdatedAt
        bool IsOnboarded
        string OnboardingDataJson
    }

    StaffProfile {
        int Id PK
        string ApplicationUserId FK
        int HealthcareProviderId FK
        int RoleType
        int ActiveShift
        bool IsInvitationAccepted
        string InvitationSenderUserId
        string InvitationToken
        datetime InvitationTokenExpiry
        datetime JoinedAt
    }

    MedicalRecord {
        int Id PK
        int PatientProfileId FK
        datetime RecordDate
        decimal SystolicBP
        decimal DiastolicBP
        decimal BloodSugar
        decimal HeartRate
        decimal Temperature
        decimal OxygenSaturation
        string Symptoms
        string Notes
        datetime CreatedAt
    }

    RiskAssessment {
        int Id PK
        int PatientProfileId FK
        datetime AssessmentDate
        decimal RiskScore
        int RiskLevel
        string Symptoms
        string RecommendationsJson
        bool IsEmergency
        datetime CreatedAt
    }

    HealthcareProvider {
        int Id PK
        string Name
        string Type
        decimal Latitude
        decimal Longitude
        string Address
        string Phone
        int AvailableBeds
        int BedCapacity
        int AmbulanceCapacity
        int AvailableAmbulances
        bool IsEmergencyCenter
        bool IsActive
        datetime CreatedAt
    }

    Appointment {
        int Id PK
        int PatientProfileId FK
        int DoctorProfileId FK
        datetime AppointmentDate
        timespan StartTime
        timespan EndTime
        int Status
        string Notes
        datetime CreatedAt
        datetime UpdatedAt
        bool ReminderSentOneDayBefore
        bool ReminderSentTwoHoursBefore
    }

    AvailableSlot {
        int Id PK
        int DoctorProfileId FK
        datetime SlotDate
        timespan SlotStart
        timespan SlotEnd
        bool IsBooked
        datetime CreatedAt
    }

    LabResult {
        int Id PK
        int PatientProfileId FK
        string TestName
        datetime TestDate
        string FilePath
        string FileUrl
        string OcrExtractedData
        string Results
        datetime CreatedAt
    }

    FamilyLink {
        int Id PK
        int PrimaryPatientId FK
        int LinkedPatientId FK
        string Relationship
        string InviteToken
        bool IsAccepted
        bool CanViewRecords
        bool CanViewRisk
        bool CanBookAppointments
        datetime CreatedAt
        datetime AcceptedAt
    }

    ChatMessage {
        int Id PK
        string SenderId FK
        string ReceiverId FK
        string Message
        bool IsRead
        datetime SentAt
    }

    Alert {
        int Id PK
        string UserId FK
        string Title
        string Message
        int Status
        string AlertType
        datetime CreatedAt
        datetime ReadAt
    }

    Notification {
        int Id PK
        string UserId FK
        string Title
        string Message
        bool IsRead
        string Link
        datetime CreatedAt
    }

    CrisisConfiguration {
        int Id PK
        string CrisisName
        int CrisisType
        int SystemMode
        bool IsActive
        string Description
        datetime StartDate
        datetime EndDate
        decimal EmergencyThreshold
        decimal HighRiskThreshold
        decimal MediumRiskThreshold
        datetime CreatedAt
        datetime UpdatedAt
    }

    OutbreakZone {
        int Id PK
        int CrisisConfigurationId FK
        string ZoneName
        decimal CenterLatitude
        decimal CenterLongitude
        decimal RadiusInKm
        string PolygonCoordinatesJson
        int RiskLevel
        datetime CreatedAt
    }

    EmergencyRequest {
        int Id PK
        int PatientProfileId FK
        int RiskAssessmentId FK
        int HealthcareProviderId FK
        int Status
        string EmergencyType
        string Description
        decimal Latitude
        decimal Longitude
        datetime RequestedAt
        datetime AcceptedAt
        datetime CompletedAt
        string ResponseNotes
        bool IsAutoGenerated
        int PriorityScore
        datetime AutoEscalatedAt
        string EscalationReason
        bool DoctorsNotified
        datetime DoctorsNotifiedAt
        bool AdminNotified
        datetime AdminNotifiedAt
        string AssignedDoctorUserId FK
        datetime DoctorAssignedAt
        string RespondedByUserId FK
        string PatientRecommendations
        string FamilyRecommendations
        string PrescribedMedications
    }

    StaffActivityLog {
        int Id PK
        int StaffProfileId FK
        string Action
        string Details
        datetime CreatedAt
    }

    DoctorProvider {
        int DoctorProfileId PK_FK
        int HealthcareProviderId PK_FK
        bool IsEmergencyDoctor
        bool IsOwner
        string AffiliationRole
    }

    Review {
        int Id PK
        int PatientProfileId FK
        int DoctorProfileId FK
        int HealthcareProviderId FK
        int Rating
        string Comment
        datetime CreatedAt
    }

    CrisisSymptomWeights {
        int CrisisConfigurationId PK_FK
        string SymptomName PK
        decimal Weight
        bool IsEmergencySymptom
    }
```

---

## تفاصيل الجداول والكيانات (Database Tables Directory)

### 1. إدارة المستخدمين (User Management)

#### جدول `ApplicationUser` (AspNetUsers)
يمثل المستخدم الرئيسي في النظام ويرتبط بجداول الهوية الافتراضية لـ ASP.NET Identity.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `string` | Primary Key | المعرف الفريد للمستخدم (GUID) |
| `FirstName` | `string` | Nullable | الاسم الأول |
| `LastName` | `string` | Nullable | الاسم الأخير |
| `ProfilePicture` | `string` | Nullable | مسار أو رابط الصورة الشخصية |
| `IsEmailVerified` | `bool` | Default: `false` | مؤشر يؤكد تفعيل البريد الإلكتروني |
| `EmailVerificationSentAt`| `datetime` | Nullable | وقت إرسال رابط التفعيل |
| `VerificationToken` | `string` | Nullable | رمز التحقق من البريد |
| `VerificationTokenExpiry`| `datetime` | Nullable | تاريخ انتهاء رمز التحقق |
| `ResetPasswordToken` | `string` | Nullable | رمز استعادة كلمة المرور |
| `ResetPasswordTokenExpiry`| `datetime`| Nullable | تاريخ انتهاء رمز الاستعادة |
| `IsActive` | `bool` | Default: `true` | حالة الحساب (نشط/موقف) |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ إنشاء الحساب |
| `LastLoginAt` | `datetime` | Nullable | تاريخ آخر تسجيل دخول |
| `MustChangePassword` | `bool` | Default: `false` | فرض تغيير كلمة المرور عند الدخول القادم |

---

### 2. الكيانات الشخصية (Profiles)

#### جدول `PatientProfile`
يحتوي على البيانات الطبية والشخصية للمرضى.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف ملف المريض |
| `ApplicationUserId` | `string` | Foreign Key, Unique, Required | يربط الحساب بجدول المستخدمين |
| `FullName` | `string` | Length: 100, Nullable | الاسم الكامل للمريض |
| `DateOfBirth` | `datetime` | Nullable | تاريخ الميلاد |
| `Gender` | `string` | Length: 10, Nullable | الجنس |
| `Height` | `decimal(5,2)`| Nullable | الطول (سم) |
| `Weight` | `decimal(5,2)`| Nullable | الوزن (كجم) |
| `ActivityLevel` | `int` (Enum) | Default: `Sedentary` | مستوى النشاط البدني للمريض |
| `BloodType` | `string` | Length: 20, Nullable | فصيلة الدم |
| `HasChronicDiseases` | `bool` | Required | هل يعاني من أمراض مزمنة؟ |
| `ChronicDiseasesNotes` | `string` | Length: 500, Nullable | تفاصيل الأمراض المزمنة |
| `Allergies` | `string` | Length: 500, Nullable | الحساسية التي يعاني منها المريض |
| `CurrentMedications` | `string` | Length: 500, Nullable | الأدوية الحالية |
| `Latitude` | `decimal(9,6)`| Nullable | إحداثيات خط العرض للمريض |
| `Longitude` | `decimal(9,6)`| Nullable | إحداثيات خط الطول للمريض |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ التسجيل |
| `UpdatedAt` | `datetime` | Nullable | تاريخ تحديث الملف |

#### جدول `DoctorProfile`
يحتوي على البيانات المهنية والتخصص للأطباء.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف ملف الطبيب |
| `ApplicationUserId` | `string` | Foreign Key, Unique, Required | يربط الحساب بجدول المستخدمين |
| `FullName` | `string` | Length: 200, Nullable | الاسم الكامل للطبيب |
| `Specialization` | `string` | Length: 300, Nullable | التخصص الطبي |
| `LicenseNumber` | `string` | Length: 100, Nullable | رقم ترخيص مزاولة المهنة |
| `YearsOfExperience` | `int` | Nullable | سنوات الخبرة |
| `Bio` | `string` | Length: 500, Nullable | نبذة عن الطبيب |
| `ConsultationFee` | `decimal(10,2)`| Nullable | تكلفة الكشف الطبي |
| `IsAvailable` | `bool` | Default: `true` | هل الطبيب متاح لاستقبال الحجوزات؟ |
| `IsOnboarded` | `bool` | Default: `false` | هل أتم مرحلة التهيئة والتأكيد؟ |
| `OnboardingDataJson` | `string` | Nullable | بيانات إضافية للتهيئة بصيغة JSON |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ التسجيل |
| `UpdatedAt` | `datetime` | Nullable | تاريخ التحديث |

#### جدول `StaffProfile`
يمثل موظفي الاستقبال أو موظفي الإدارة التابعين للمستشفيات أو المراكز الطبية.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الموظف |
| `ApplicationUserId` | `string` | Foreign Key, Unique, Required | يربط الموظف بالمستخدم |
| `HealthcareProviderId` | `int` | Foreign Key, Required | يربط الموظف بالجهة الطبية التابع لها |
| `RoleType` | `int` (Enum) | Default: `Receptionist` | دور الموظف (موظف استقبال، مسؤول نظام، الخ) |
| `ActiveShift` | `int` (Enum) | Default: `None` | النوبتجية/الوردية النشطة |
| `IsInvitationAccepted` | `bool` | Default: `false` | هل تم قبول دعوة الانضمام للمؤسسة؟ |
| `InvitationSenderUserId`| `string` | Length: 450, Nullable | معرف المستخدم الذي أرسل الدعوة |
| `InvitationToken` | `string` | Length: 200, Nullable | كود/توكن الدعوة |
| `InvitationTokenExpiry`| `datetime` | Nullable | تاريخ انتهاء صلاحية الدعوة |
| `JoinedAt` | `datetime` | Nullable | تاريخ الانضمام للمؤسسة |

---

### 3. السجلات الطبية والتحاليل (Clinical Records)

#### جدول `MedicalRecord`
لتسجيل القراءات الحيوية للمريض بشكل دوري لمتابعة حالته الصحية.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف السجل الطبي |
| `PatientProfileId` | `int` | Foreign Key, Required | يربط السجل بملف المريض |
| `RecordDate` | `datetime` | Default: `UtcNow` | تاريخ تسجيل القراءات |
| `SystolicBP` | `decimal(5,2)`| Nullable | ضغط الدم الانقباضي |
| `DiastolicBP` | `decimal(5,2)`| Nullable | ضغط الدم الانبساطي |
| `BloodSugar` | `decimal(5,2)`| Nullable | نسبة السكر في الدم |
| `HeartRate` | `decimal(5,2)`| Nullable | نبضات القلب بالدقيقة |
| `Temperature` | `decimal(5,2)`| Nullable | درجة حرارة الجسم |
| `OxygenSaturation` | `decimal(5,2)`| Nullable | نسبة الأكسجين بالدم |
| `Symptoms` | `string` | Length: 1000, Nullable | الأعراض المصاحبة |
| `Notes` | `string` | Length: 1000, Nullable | ملاحظات الطبيب أو المريض |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ الإدخال للنظام |

#### جدول `RiskAssessment`
تقييمات المخاطر الصحية للمرضى والتي يتم حسابها بناءً على محرك الأزمات والأعراض المكتشفة.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف التقييم |
| `PatientProfileId` | `int` | Foreign Key, Required | يربط التقييم بملف المريض |
| `AssessmentDate` | `datetime` | Default: `UtcNow` | تاريخ التقييم |
| `RiskScore` | `decimal(3,2)`| Range: 0.00 to 1.00 | درجة الخطورة المحسوبة |
| `RiskLevel` | `int` (Enum) | Required | مستوى الخطورة (Normal, Low, Medium, High) |
| `Symptoms` | `string` | Length: 2000, Nullable | الأعراض التي تم تسجيلها في هذا التقييم |
| `RecommendationsJson` | `string` | Nullable | التوصيات الموجهة للمريض بصيغة JSON |
| `IsEmergency` | `bool` | Required | هل الحالة تعد طارئة وتستدعي تدخل فوري؟ |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ الحفظ |

#### جدول `LabResult`
يحتوي على التحاليل الطبية والتقارير التي يرفعها المرضى، ويدعم حفظ النصوص المستخرجة عبر الـ OCR.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف نتيجة التحليل |
| `PatientProfileId` | `int` | Foreign Key, Required | يربط التحليل بملف المريض |
| `TestName` | `string` | Length: 300, Required | اسم التحليل/الفحص الطبي |
| `TestDate` | `datetime` | Required | تاريخ الفحص |
| `FilePath` | `string` | Length: 500, Nullable | مسار الملف المخزن محلياً |
| `FileUrl` | `string` | Length: 500, Nullable | رابط الملف على سيرفر التخزين |
| `OcrExtractedData` | `string` | Nullable | النصوص الطبية المستخرجة من الفحص بالـ OCR |
| `Results` | `string` | Length: 1000, Nullable | ملخص النتائج أو التقييم النهائي |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ الرفع |

---

### 4. المواعيد والتقييمات (Scheduling & Reviews)

#### جدول `Appointment`
الحجوزات الطبية بين المرضى والأطباء.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الحجز |
| `PatientProfileId` | `int` | Foreign Key, Required | المريض صاحب الحجز |
| `DoctorProfileId` | `int` | Foreign Key, Nullable | الطبيب المعالج (يمكن تركه فارغاً مؤقتاً في بعض الحالات) |
| `AppointmentDate` | `datetime` | Required | تاريخ الحجز |
| `StartTime` | `timespan` | Required | وقت بدء الحجز |
| `EndTime` | `timespan` | Required | وقت انتهاء الحجز |
| `Status` | `int` (Enum) | Default: `Scheduled` | حالة الحجز (Scheduled, Cancelled, Completed, etc.) |
| `Notes` | `string` | Length: 500, Nullable | ملاحظات المريض أو شكواه |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ إنشاء الحجز |
| `UpdatedAt` | `datetime` | Nullable | تاريخ التحديث |
| `ReminderSentOneDayBefore` | `bool` | Default: `false` | هل تم إرسال تذكير بالبريد الإلكتروني قبل الحجز بـ 24 ساعة؟ |
| `ReminderSentTwoHoursBefore`| `bool` | Default: `false` | هل تم إرسال تذكير بالبريد الإلكتروني قبل الحجز بـ ساعتين؟ |

#### جدول `AvailableSlot`
الفترات الزمنية المتاحة للأطباء والتي يمكن للمرضى الحجز فيها.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الفترة الزمنية |
| `DoctorProfileId` | `int` | Foreign Key, Required | الطبيب الذي يملك الفترة |
| `SlotDate` | `datetime` | Required | التاريخ |
| `SlotStart` | `timespan` | Required | وقت البدء |
| `SlotEnd` | `timespan` | Required | وقت الانتهاء |
| `IsBooked` | `bool` | Default: `false` | هل تم حجز هذه الفترة؟ |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ الإنشاء |

#### جدول `Review`
التقييمات والآراء التي يكتبها المرضى للأطباء أو للمستشفيات والجهات الطبية.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف التقييم |
| `PatientProfileId` | `int` | Foreign Key, Required | المريض الذي أضاف التقييم |
| `DoctorProfileId` | `int` | Foreign Key, Nullable | الطبيب الذي يتم تقييمه |
| `HealthcareProviderId` | `int` | Foreign Key, Nullable | المستشفى/الجهة الطبية التي يتم تقييمها |
| `Rating` | `int` | Range: 1 to 5 | عدد النجوم (1-5) |
| `Comment` | `string` | Length: 1000, Required | التعليق والوصف |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ كتابة التقييم |

---

### 5. إدارة الأزمات والطوارئ (Crisis & Emergency Management)

#### جدول `CrisisConfiguration`
إعدادات إدارة أزمة صحية أو تفشي وبائي في النظام وتحديد المعايير المتبعة للفرز التلقائي.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف إعداد الأزمة |
| `CrisisName` | `string` | Length: 200, Required | اسم الأزمة/المرض الوبائي (مثل كوفيد-19) |
| `CrisisType` | `int` (Enum) | Required | نوع الأزمة (وباء، كارثة طبيعية، الخ) |
| `SystemMode` | `int` (Enum) | Default: `Normal` | وضع النظام العام (Normal, Pandemic, Crisis) |
| `IsActive` | `bool` | Default: `false` | هل هذه التهيئة نشطة حالياً في محرك الأزمات؟ |
| `Description` | `string` | Length: 1000, Nullable | تفاصيل وشرح الأزمة |
| `StartDate` | `datetime` | Required | تاريخ البدء الفعلي |
| `EndDate` | `datetime` | Nullable | تاريخ الانتهاء (عند احتواء الأزمة) |
| `EmergencyThreshold` | `decimal` | Default: `0.70` | الحد الأدنى لدرجة الخطورة لاعتبار الحالة طارئة |
| `HighRiskThreshold` | `decimal` | Default: `0.50` | الحد الأدنى لدرجة الخطورة لاعتبار الحالة عالية الخطورة |
| `MediumRiskThreshold` | `decimal` | Default: `0.30` | الحد الأدنى لدرجة الخطورة لاعتبار الحالة متوسطة الخطورة |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ إنشاء السجل |
| `UpdatedAt` | `datetime` | Nullable | تاريخ التحديث |

#### جدول `OutbreakZone`
المناطق الجغرافية الموبوءة أو التي يتفشى بها الفيروس لتنبيه المرضى وتعديل حسابات خطورة الفرز.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف المنطقة الموبوءة |
| `CrisisConfigurationId`| `int` | Foreign Key, Required | يربط المنطقة بتهيئة أزمة محددة |
| `ZoneName` | `string` | Length: 200, Required | اسم المنطقة (مثال: حي المعادي) |
| `CenterLatitude` | `decimal(9,6)`| Required | إحداثيات مركز المنطقة (خط عرض) |
| `CenterLongitude` | `decimal(9,6)`| Required | إحداثيات مركز المنطقة (خط طول) |
| `RadiusInKm` | `decimal` | Required | نصف قطر المنطقة المحددة بالكيلومترات |
| `PolygonCoordinatesJson`| `string` | Nullable | إحداثيات مضلع جغرافي مفصل لحواف المنطقة بصيغة JSON |
| `RiskLevel` | `int` | Required | مستوى الخطر بالمنطقة (مثال: 1-5 أو كود لون) |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ تحديد المنطقة |

#### جدول `CrisisSymptomWeights`
جدول تابع (Owned Table) لجدول `CrisisConfiguration` يربط وزن كل عرض طبي بالأزمة لفرز حالة المرضى بدقة.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `CrisisConfigurationId`| `int` | Primary Key, Foreign Key | معرف الأزمة التابع لها |
| `SymptomName` | `string` | Primary Key, Length: 200 | اسم العرض الطبي (مثال: الحمى، السعال) |
| `Weight` | `decimal(3,2)`| Range: 0.00 to 1.00 | مدى تأثير هذا العرض على تقييم خطورة الحالة |
| `IsEmergencySymptom` | `bool` | Default: `false` | هل العرض يعبر عن خطر فوري بمفرده؟ |

#### جدول `EmergencyRequest`
طلبات الاستغاثة والإسعاف وحالات الطوارئ التي تطلبها الحالات أو يقوم النظام بتوليدها تلقائياً.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف طلب الاستغاثة |
| `PatientProfileId` | `int` | Foreign Key, Required | المريض صاحب الاستغاثة |
| `RiskAssessmentId` | `int` | Foreign Key, Nullable | الفرز الصحي المرتبط بالطلب |
| `HealthcareProviderId`| `int` | Foreign Key, Nullable | المستشفى/الجهة الطبية التي تتعامل مع الطلب |
| `Status` | `int` (Enum) | Default: `Pending` | حالة الطلب (Pending, Dispatched, Arrived, Resolved, Escalated, etc.) |
| `EmergencyType` | `string` | Length: 500, Nullable | تصنيف الطوارئ (تنفس، نوبة قلبية، حادث، تفشي) |
| `Description` | `string` | Length: 1000, Nullable | تفاصيل المشكلة الصحية الحالية |
| `Latitude` | `decimal(9,6)`| Nullable | إحداثيات موقع الاستغاثة (عرض) |
| `Longitude` | `decimal(9,6)`| Nullable | إحداثيات موقع الاستغاثة (طول) |
| `RequestedAt` | `datetime` | Default: `UtcNow` | وقت إرسال الطلب |
| `AcceptedAt` | `datetime` | Nullable | وقت قبول المستشفى أو المسعف للطلب |
| `CompletedAt` | `datetime` | Nullable | وقت حل الطلب وإنهائه |
| `ResponseNotes` | `string` | Length: 500, Nullable | ملاحظات المسعفين أو الطبيب المستجيب |
| `IsAutoGenerated` | `bool` | Default: `false` | هل تم توليد الطلب تلقائياً بواسطة خوارزمية الفرز؟ |
| `PriorityScore` | `int` | Default: `0` | درجة الأولوية في طابور الطوارئ |
| `AutoEscalatedAt` | `datetime` | Nullable | وقت تصعيد الطلب تلقائياً لعدم الاستجابة |
| `EscalationReason` | `string` | Length: 1000, Nullable | سبب تصعيد الاستغاثة |
| `DoctorsNotified` | `bool` | Required | هل تم إخطار الأطباء المناوبين بالطلب؟ |
| `DoctorsNotifiedAt` | `datetime` | Nullable | وقت إخطار الأطباء |
| `AdminNotified` | `bool` | Required | هل تم إخطار المسؤول الإداري بالطلب؟ |
| `AdminNotifiedAt` | `datetime` | Nullable | وقت إخطار المسؤول |
| `AssignedDoctorUserId` | `string` | Foreign Key, Nullable | الطبيب المعين لمتابعة حالة الطوارئ (من جدول المستخدمين) |
| `DoctorAssignedAt` | `datetime` | Nullable | وقت تعيين الطبيب |
| `RespondedByUserId` | `string` | Foreign Key, Nullable | معرف المستخدم (الموظف/المسؤول) الذي استجاب للطلب |
| `PatientRecommendations`| `string` | Length: 2000, Nullable | التوصيات الطبية المقدمة للمريض فوراً |
| `FamilyRecommendations` | `string` | Length: 2000, Nullable | التوصيات الطبية لأفراد أسرة المريض لمساعدته وتجنب العدوى |
| `PrescribedMedications`| `string` | Length: 2000, Nullable | الأدوية الطارئة الموصوفة للمريض في الميدان |

---

### 6. مقدمو الخدمات الطبية (Healthcare Providers)

#### جدول `HealthcareProvider`
يمثل المستشفيات، العيادات والمراكز الصحية الشريكة في النظام.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الجهة الطبية |
| `Name` | `string` | Length: 200, Required | اسم المستشفى أو المركز |
| `Type` | `string` | Required | النوع (مستشفى عام، عيادة تخصصية، مركز طوارئ) |
| `Latitude` | `decimal(9,6)`| Required | إحداثيات الموقع (عرض) |
| `Longitude` | `decimal(9,6)`| Required | إحداثيات الموقع (طول) |
| `Address` | `string` | Length: 300, Nullable | العنوان النصي |
| `Phone` | `string` | Length: 20, Nullable | رقم الهاتف للتواصل |
| `AvailableBeds` | `int` | Concurrency Check, Nullable | عدد الأسرة الشاغرة حالياً (يخضع للفحص التزامني) |
| `BedCapacity` | `int` | Concurrency Check, Default: 150 | السعة الكلية للأسرة بالمستشفى |
| `AmbulanceCapacity` | `int` | Concurrency Check, Default: 4 | السعة الكلية لسيارات الإسعاف |
| `AvailableAmbulances` | `int` | Concurrency Check, Default: 4 | سيارات الإسعاف الشاغرة الجاهزة للانطلاق |
| `IsEmergencyCenter` | `bool` | Required | هل المركز يقبل حالات طوارئ وإسعاف؟ |
| `IsActive` | `bool` | Default: `true` | حالة تشغيل المؤسسة في النظام |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ التسجيل بالنظام |

#### جدول `DoctorProvider`
جدول وسيط يمثل العلاقة (أطراف بأطراف N:M) بين الأطباء والجهات الطبية التي يعملون بها.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `DoctorProfileId` | `int` | Composite Primary Key, Foreign Key | معرف ملف الطبيب |
| `HealthcareProviderId`| `int` | Composite Primary Key, Foreign Key | معرف الجهة الطبية |
| `IsEmergencyDoctor` | `bool` | Default: `false` | هل يعمل كطبيب طوارئ في هذه المؤسسة؟ |
| `IsOwner` | `bool` | Default: `false` | هل العيادة مملوكة بالكامل لهذا الطبيب؟ |
| `AffiliationRole` | `nvarchar(100)`| Nullable | المسمى الوظيفي للطبيب في المؤسسة (استشاري، طبيب مقيم) |

---

### 7. وسائل التواصل والتنبيهات (Communication & Logging)

#### جدول `ChatMessage`
لتسجيل المحادثات الفورية والرسائل الطبية بين المرضى والأطباء.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الرسالة |
| `SenderId` | `string` | Foreign Key, Required | المستخدم المرسل للرسالة |
| `ReceiverId` | `string` | Foreign Key, Required | المستخدم المستلم للرسالة |
| `Message` | `string` | Length: 2000, Required | محتوى الرسالة النصي |
| `IsRead` | `bool` | Default: `false` | مؤشر قراءة الرسالة |
| `SentAt` | `datetime` | Default: `UtcNow` | وقت إرسال الرسالة |

#### جدول `Alert`
لتنبيهات الطوارئ والتنبيهات التلقائية للنظام في حالة اكتشاف المخاطر.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف التنبيه |
| `UserId` | `string` | Foreign Key, Required | المستخدم الموجه له التنبيه |
| `Title` | `string` | Length: 300, Required | عنوان التنبيه |
| `Message` | `string` | Length: 1000, Required | محتوى التنبيه وتفاصيله |
| `Status` | `int` (Enum) | Default: `Unread` | حالة التنبيه (Unread, Read, Dismissed) |
| `AlertType` | `string` | Required | نوع التنبيه (طوارئ، وقاية، وباء، حجز) |
| `CreatedAt` | `datetime` | Default: `UtcNow` | تاريخ توليد التنبيه |
| `ReadAt` | `datetime` | Nullable | تاريخ قراءة التنبيه |

#### جدول `Notification`
الإشعارات العامة للمستخدمين داخل التطبيق.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الإشعار |
| `UserId` | `string` | Foreign Key, Required | المستخدم المستهدف بالإشعار |
| `Title` | `string` | Length: 300, Required | عنوان الإشعار |
| `Message` | `string` | Length: 1000, Required | تفاصيل الإشعار |
| `IsRead` | `bool` | Default: `false` | هل تمت قراءة الإشعار؟ |
| `Link` | `string` | Length: 500, Nullable | رابط داخل التطبيق يتم توجيه المستخدم إليه عند الضغط |
| `CreatedAt` | `datetime` | Default: `UtcNow` | وقت توليد الإشعار |

#### جدول `StaffActivityLog`
لمراقبة أفعال الموظفين لأغراض الحماية والأمن ومراجعة التغييرات الإدارية.

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف السجل |
| `StaffProfileId` | `int` | Foreign Key, Required | معرف الموظف الذي قام بالفعل |
| `Action` | `string` | Length: 100, Required | الإجراء المتبع (مثل: تعديل عدد الأسرة، إرسال دعوة) |
| `Details` | `string` | Length: 1000, Nullable | تفاصيل التغييرات الحادثة |
| `CreatedAt` | `datetime` | Default: `UtcNow` | وقت وتاريخ القيام بالفعل |

---

### 8. ربط العائلات (Family Relationships)

#### جدول `FamilyLink`
لربط ملفات المرضى وتوفير إمكانية الرعاية العائلية (مثل حجز موعد للتابع أو مراجعة تقاريره في الطوارئ).

| الحقل (Field) | النوع (Type) | القيود (Constraints) | الوصف (Description) |
| :--- | :--- | :--- | :--- |
| `Id` | `int` | Primary Key, Identity | معرف الرابط العائلي |
| `PrimaryPatientId` | `int` | Foreign Key, Required | المريض الأساسي (الراعي العائلي) |
| `LinkedPatientId` | `int` | Foreign Key, Required | المريض التابع أو المرتبط بالملف |
| `Relationship` | `string` | Length: 100, Required | طبيعة العلاقة (أب، أم، ابن، زوج، الخ) |
| `InviteToken` | `string` | Required, Unique Index | رمز الدعوة لربط الحسابات وتأكيد العلاقة |
| `IsAccepted` | `bool` | Default: `false` | هل وافق الحساب التابع على الربط؟ |
| `CanViewRecords` | `bool` | Default: `false` | صلاحية الاطلاع على السجلات الحيوية والطبية للتابع |
| `CanViewRisk` | `bool` | Default: `false` | صلاحية الاطلاع على نتائج تقييمات الخطورة للتابع |
| `CanBookAppointments` | `bool` | Default: `false` | صلاحية حجز المواعيد نيابة عن الحساب التابع |
| `CreatedAt` | `datetime` | Default: `UtcNow` | وقت إرسال طلب الربط |
| `AcceptedAt` | `datetime` | Nullable | وقت وتاريخ قبول الطلب وتفعيل الصلاحيات |
