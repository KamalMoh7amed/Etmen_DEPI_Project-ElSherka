# Business Logic Layer (BLL) Architectural Review & Presentation Layer Plan (Feature-Based)

This document presents a detailed review of the Business Logic Layer (BLL) of the **Etmen** platform, along with a design plan for the Presentation Layer (PL) structured using **Feature-Based (Granular) Controllers** for separation of concerns and single responsibility.

---

## Table of Contents
1. [Controllers Plan](#1-controllers-plan)
2. [ViewModels Plan](#2-viewmodels-plan)
3. [Views Plan](#3-views-plan)
4. [User Stories](#4-user-stories)
5. [BLL Observations & Recommendations](#5-bll-observations--recommendations)

---

## 1. Controllers Plan

The controllers are structured into small, single-responsibility classes based on functional features rather than grouped role-based classes.

### 1.1 Authentication & Main Features

#### 1. AccountController
* **Who uses it**: Guest / Authenticated Users (All roles)
* **Responsibility**: User signup, sign-in, email validation, and password recovery. Exposes [IAuthService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAuthService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Register` | GET | Renders the registration form. | None | None | `Register` View + new `RegisterDto` |
| `Register` | POST | Submits registration details and creates profile. | `IAuthService.RegisterAsync` | `RegisterDto` (Form) | Redirect to `VerifyEmailNotice` or return `Register` View |
| `VerifyEmailNotice` | GET | Displays check-email instructions. | None | None | `VerifyEmailNotice` View |
| `VerifyEmail` | GET | Confirms user email verification token. | `IAuthService.VerifyEmailAsync` | `string userId`, `string token` | `VerifyEmail` View |
| `Login` | GET | Renders login page. | None | `string? returnUrl` | `Login` View + new `LoginDto` |
| `Login` | POST | Authenticates and signs in user. | `IAuthService.LoginAsync` | `LoginDto` (Form), `string? returnUrl` | Redirect to role dashboard or return `Login` View |
| `Logout` | POST | Signs out the current user session. | `SignInManager.SignOutAsync` | None | Redirect to `Login` |
| `ForgotPassword` | GET | Renders password recovery form. | None | None | `ForgotPassword` View + new `ForgotPasswordDto` |
| `ForgotPassword` | POST | Generates recovery email. | `IAuthService.ForgotPasswordAsync` | `ForgotPasswordDto` (Form) | Redirect to `ForgotPasswordConfirmation` |
| `ForgotPasswordConfirmation` | GET | Recovery link email notice. | None | None | `ForgotPasswordConfirmation` View |
| `ResetPassword` | GET | Renders password reset form. | None | `string token`, `string email` | `ResetPassword` View + `ResetPasswordDto` |
| `ResetPassword` | POST | Resets password. | `IAuthService.ResetPasswordAsync` | `ResetPasswordDto` (Form) | Redirect to `ResetPasswordConfirmation` |
| `ResetPasswordConfirmation` | GET | Confirms password reset. | None | None | `ResetPasswordConfirmation` View |

#### 2. HomeController
* **Who uses it**: Guest / Authenticated Users
* **Responsibility**: Serves general landing page, privacy policy, and handles role redirection. Exposes [ICrisisService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ICrisisService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders public landing page (or redirects if logged in). | `ICrisisService.GetActiveCrisisAsync` | None | `Index` View + `LandingPageViewModel` |
| `Privacy` | GET | Displays privacy policy. | None | None | `Privacy` View |
| `Error` | GET | Renders standard error pages. | None | `int? statusCode` | `Error` View + `ErrorViewModel` |

---

### 1.2 Patient Features

#### 3. PatientDashboardController
* **Who uses it**: Patient
* **Responsibility**: Displays patient landing dashboard metrics. Exposes [IPatientService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IPatientService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders patient home dashboard panels. | `IPatientService.GetDashboardAsync` | None | `Index` View + `DashboardDto` |

#### 4. PatientProfileController
* **Who uses it**: Patient
* **Responsibility**: Renders and saves patient baseline medical metrics. Exposes [IPatientService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IPatientService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders patient profile editing page. | `IPatientService.GetProfileAsync` | None | `Index` View + `ProfileDto` |
| `Index` | POST | Updates patient metrics (weight, allergies, etc.). | `IPatientService.UpdateProfileAsync` | `ProfileDto` (Form) | `Index` View with updated `ProfileDto` |

#### 5. MedicalRecordsController
* **Who uses it**: Patient
* **Responsibility**: Renders vitals logs and logs manual entries. Exposes [IPatientService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IPatientService.cs) and [IMedicalRecordService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IMedicalRecordService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists previous medical logs. | `IPatientService.GetMedicalRecordsAsync` | None | `Index` View + `IEnumerable<MedicalRecordDto>` |
| `Create` | POST | Logs a manual patient vitals entry. | `IPatientService.AddMedicalRecordAsync` | `MedicalRecordCreateDto` (Form) | Redirect to `Index` |

#### 6. LabResultsController
* **Who uses it**: Patient
* **Responsibility**: Renders lab result reports and handles OCR document uploads. Exposes [ILabService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ILabService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Displays timeline of lab uploads. | `ILabService.GetPatientLabResultsAsync` | None | `Index` View + `IEnumerable<LabResultDto>` |
| `Upload` | POST | Submits a PDF/image lab report with OCR processing flag. | `ILabService.UploadLabResultAsync` | `LabUploadDto` (Form) + file | Redirect to `Index` |

#### 7. RiskAssessmentController
* **Who uses it**: Patient
* **Responsibility**: Processes self-assessments and displays recommendations. Exposes [IPatientService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IPatientService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders assessment inputs questionnaire. | None | None | `Index` View + new `RiskInputDto` |
| `Index` | POST | Computes risk and schedules triage. | `IPatientService.AssessRiskAsync` | `RiskInputDto` (Form) | Redirect to `Result` (storing JSON in `TempData`) |
| `Result` | GET | Renders calculated risk category and recommendations. | None | None (reads from `TempData`) | `Result` View + `RiskResultDto` |
| `History` | GET | Lists previous risk scores. | `IPatientService.GetRiskHistoryAsync` | None | `History` View + `IEnumerable<RiskResultDto>` |

#### 8. NearbyProvidersController
* **Who uses it**: Patient
* **Responsibility**: Queries and maps nearest emergency clinics. Exposes [INearbyService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/INearbyService.cs) and [IAppointmentService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAppointmentService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders GPS location map finder. | None | None | `Index` View + new `NearbySearchDto` |
| `Index` | POST | Lists facilities near coordinate radius. | `INearbyService.SearchNearbyProvidersAsync` | `NearbySearchDto` (Form) | `Index` View + `List<ProviderDto>` |
| `Book` | POST | Books a slot with a doctor. | `IAppointmentService.BookAppointmentAsync` | `BookingRequestDto` (Form) | Redirect to Patient Appointments list |

#### 9. FamilyLinkingController
* **Who uses it**: Patient
* **Responsibility**: Invitation flows and viewer permission adjustments. Exposes [IFamilyService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IFamilyService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists family links and status. | `IFamilyService.GetFamilyMembersAsync` | None | `Index` View + `List<FamilyDto>` |
| `Invite` | POST | Sends link invite. | `IFamilyService.InviteFamilyMemberAsync` | `FamilyInviteDto` (Form) | Redirect to `Index` |
| `Accept` | GET | Completes link from token parameter. | `IFamilyService.AcceptFamilyInviteAsync` | `string token` | Redirect to `Index` |
| `Remove` | POST | Deletes family link. | `IFamilyService.RemoveFamilyMemberAsync` | `int id` | Redirect to `Index` |
| `UpdatePermissions` | POST | Adjusts record view settings. | `IFamilyService.UpdateFamilyPermissionsAsync` | `int id`, `FamilyDto` (Form) | Redirect to `Index` |

---

### 1.3 Doctor Features

#### 10. DoctorDashboardController
* **Who uses it**: Doctor
* **Responsibility**: Doctor clinics calendar schedule and clinic metrics. Exposes [IDoctorService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IDoctorService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Displays schedule summary and statistics charts. | `IDoctorService.GetDashboardAsync` | None | `Index` View + `DoctorDashboardDto` |

#### 11. DoctorProfileController
* **Who uses it**: Doctor
* **Responsibility**: Renders and saves doctor clinical settings. Exposes [IDoctorService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IDoctorService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders profile editor form. | `IDoctorService.GetProfileAsync` | None | `Index` View + `DoctorProfileDto` |
| `Index` | POST | Updates availability, fees, specialization. | `IDoctorService.UpdateProfileAsync` | `DoctorProfileDto` (Form) | `Index` View with updated `DoctorProfileDto` |

#### 12. DoctorSlotsController
* **Who uses it**: Doctor
* **Responsibility**: Configures doctor availability slots. Exposes [IDoctorService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IDoctorService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders calendar grid of slots. | `IDoctorService.GetAvailableSlotsAsync` | None | `Index` View + `IEnumerable<AvailableSlotDto>` |
| `Create` | POST | Adds a single availability slot. | `IDoctorService.AddSlotAsync` | `CreateAvailableSlotDto` (Form) | Redirect to `Index` |
| `CreateBulk` | POST | Auto-generates series of slots in range. | `IDoctorService.BulkAddSlotsAsync` | `BulkCreateSlotsDto` (Form) | Redirect to `Index` |
| `Delete` | POST | Deletes an unbooked availability slot. | `IDoctorService.DeleteSlotAsync` | `int id` | Redirect to `Index` |

#### 13. DoctorAppointmentsController
* **Who uses it**: Doctor
* **Responsibility**: Manages scheduled consultations. Exposes [IDoctorService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IDoctorService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists scheduled appointments. | `IDoctorService.GetAppointmentsAsync` | None | `Index` View + `IEnumerable<DoctorAppointmentDto>` |
| `Details` | GET | Shows specific appointment details. | `IDoctorService.GetAppointmentAsync` | `int id` | `Details` View + `DoctorAppointmentDto` |
| `UpdateStatus` | POST | Updates appointment status. | `IDoctorService.UpdateAppointmentStatusAsync` | `UpdateAppointmentStatusDto` (Form) | Redirect to `Index` |

#### 14. DoctorPatientsController
* **Who uses it**: Doctor
* **Responsibility**: Search patient registry, review histories, record diagnostic logs, and show AI summaries. Exposes:
  * [IDoctorService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IDoctorService.cs)
  * [IPatientService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IPatientService.cs)
  * [ICriticalIntelligenceService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ICriticalIntelligenceService.cs)

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Search` | GET | Displays patient search lookup page. | None | None | `Search` View |
| `Search` | POST | Returns patients matching keyword (AJAX). | `IDoctorService.SearchPatientsAsync` | `string searchTerm` | Partial View `_PatientList` + `IEnumerable<PatientSearchDto>` |
| `Details` | GET | Renders patient clinical records, AI summary, and deterioration warnings. | `IPatientService.GetProfileAsync`, `ICriticalIntelligenceService.GenerateMedicalSummaryAsync`, `ICriticalIntelligenceService.PredictDeteriorationAsync` | `int patientProfileId` | `Details` View + Custom ViewModel |
| `AddMedicalRecord` | POST | Documents diagnosis and treatment notes for a patient. | `IDoctorService.AddMedicalRecordForPatientAsync` | `MedicalRecordCreateDto` (Form) | Redirect to `Details` |

---

### 1.4 Emergency & Triage Features

#### 15. EmergencyController
* **Who uses it**: Patient
* **Responsibility**: Triggers ambulance requests and tracks dispatch. Exposes [IEmergencyService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IEmergencyService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `RequestAmbulance` | POST | Requests an emergency ambulance at coordinates. | `IEmergencyService.CreateEmergencyRequestAsync` | `EmergencyRequestDto` (Form) | Redirect to `Track` |
| `Track` | GET | Tracks ambulance status and distance. | `IEmergencyService.GetEmergencyRequestAsync` | `int id` | `Track` View + `EmergencyRequestDto` |

#### 16. DoctorPanicInboxController
* **Who uses it**: Doctor
* **Responsibility**: Handles urgent patient alerts and case assignment. Exposes [ICriticalIntelligenceService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ICriticalIntelligenceService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists assigned and unassigned critical cases. | `ICriticalIntelligenceService.GetDoctorPanicInboxAsync` | None | `Index` View + `DoctorPanicInboxDto` |
| `Claim` | POST | Assigns a critical care request to the doctor. | `ICriticalIntelligenceService.AssignBestDoctorAsync` | `int id` | Redirect to `Index` |

#### 17. HospitalQueueController
* **Who uses it**: Hospital Staff
* **Responsibility**: Monitors incoming ambulances and manages bed availability. Exposes [IHospitalStaffService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IHospitalStaffService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists active ambulance triages. | `IHospitalStaffService.GetQueueAsync` | None | `Index` View + `HospitalStaffQueueDto` |
| `Details` | GET | Displays detailed medical context of the emergency patient. | `IHospitalStaffService.GetRequestDetailAsync` | `int id` | `Details` View + `HospitalStaffEmergencyDetailDto` |
| `Respond` | POST | Hospital staff accepts or rejects the request. | `IHospitalStaffService.RespondToRequestAsync` | `HospitalStaffEmergencyRespondDto` (Form) | Redirect to `Index` |
| `UpdateBeds` | POST | Modifies the hospital's available emergency beds configuration. | `IHospitalStaffService.UpdateBedsAsync` | `HospitalStaffBedsUpdateDto` (Form) | Redirect to `Index` |

---

### 1.5 Admin Features

#### 18. AdminDashboardController
* **Who uses it**: General Admin
* **Responsibility**: System overview telemetry dashboard. Exposes [IAdminService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Shows active users, appointments, and crisis status. | `IAdminService.GetDashboardAsync` | None | `Index` View + `AdminDashboardDto` |

#### 19. AdminUsersController
* **Who uses it**: General Admin
* **Responsibility**: Manages user profiles. Exposes [IAdminService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists system users with status toggles. | `IAdminService.GetAllUsersAsync` | `int pageNumber = 1` | `Index` View + `PaginatedResult<UserListItemDto>` |
| `UpdateStatus` | POST | Activates or deactivates a user account. | `IAdminService.UpdateUserStatusAsync` | `int userId`, `UpdateUserStatusDto` (Form) | Redirect to `Index` |
| `BulkAction` | POST | Applies actions on multiple users. | `IAdminService.BulkUserActionAsync` | `BulkUserActionDto` (Form) | Redirect to `Index` |
| `Delete` | POST | Permanently deletes a user from the system. | `IAdminService.DeleteUserAsync` | `int userId` | Redirect to `Index` |

#### 20. AdminProvidersController
* **Who uses it**: General Admin
* **Responsibility**: Registers healthcare centers. Exposes [IAdminService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists provider centers with locations. | `IAdminService.GetAllProvidersAsync` | `int pageNumber = 1` | `Index` View + `PaginatedResult<ProviderListItemDto>` |
| `Create` | GET | Form to register a hospital/clinic profile. | None | None | `Create` View + new `CreateProviderDto` |
| `Create` | POST | Submits details to register a new provider. | `IAdminService.CreateProviderAsync` | `CreateProviderDto` (Form) | Redirect to `Index` |
| `Edit` | GET | Renders edit provider interface. | `IAdminService.GetProviderByIdAsync` | `int id` | `Edit` View + `ProviderListItemDto` |
| `Edit` | POST | Saves updates to provider coordinates, beds, and status. | `IAdminService.UpdateProviderAsync` | `int id`, `UpdateProviderDto` (Form) | Redirect to `Index` |
| `Delete` | POST | Deletes a registered provider center. | `IAdminService.DeleteProviderAsync` | `int id` | Redirect to `Index` |

#### 21. AdminCrisisController
* **Who uses it**: General Admin / Crisis Admin
* **Responsibility**: Configures epidemics, symptom weights, outbreak maps, and approves escalations. Exposes:
  * [ICrisisService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ICrisisService.cs)
  * [ICriticalIntelligenceService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/ICriticalIntelligenceService.cs)
  * [IAdminService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs)

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists all configured crises (active/inactive). | `ICrisisService.GetAllCrisesAsync` | None | `Index` View + `List<CrisisConfigurationDto>` |
| `Create` | GET | Form to configure a new epidemic. | None | None | `Create` View + new `CreateCrisisDto` |
| `Create` | POST | Saves new crisis template. | `ICrisisService.CreateCrisisAsync` | `CreateCrisisDto` (Form) | Redirect to `Index` |
| `Edit` | GET | Form to edit crisis fields. | `ICrisisService.GetCrisisByIdAsync` | `int id` | `Edit` View + `CrisisConfigurationDto` |
| `Edit` | POST | Submits edits to crisis thresholds. | `ICrisisService.UpdateCrisisAsync` | `int id`, `EditCrisisDto` (Form) | Redirect to `Index` |
| `Details` | GET | Shows crisis configuration, weights, and stats. | `ICrisisService.GetCrisisByIdAsync`, `ICrisisService.GetCrisisStatsAsync` | `int id` | `Details` View + Custom ViewModel |
| `Activate` | POST | Activates crisis mode for a crisis configuration. | `ICrisisService.ActivateCrisisAsync` | `int id` | Redirect to `Details` |
| `Deactivate` | POST | Deactivates a crisis configuration, returning system to Normal. | `ICrisisService.DeactivateCrisisAsync` | `int id` | Redirect to `Details` |
| `AddSymptom` | POST | Associates a symptom weight with a crisis template. | `ICrisisService.AddSymptomAsync` | `int crisisId`, `SymptomWeightDto` (Form) | Redirect to `Details` |
| `UpdateSymptom` | POST | Updates a symptom's weight multiplier. | `ICrisisService.UpdateSymptomAsync` | `int crisisId`, `string symptomName`, `SymptomWeightDto` (Form) | Redirect to `Details` |
| `RemoveSymptom` | POST | Deletes a symptom association. | `ICrisisService.RemoveSymptomAsync` | `int crisisId`, `string symptomName` | Redirect to `Details` |
| `CommandCenter` | GET | Dashboard showing real-time dispatch wait times. | `ICriticalIntelligenceService.GetCommandCenterAsync` | None | `CommandCenter` View + `CriticalCommandCenterDto` |
| `Heatmap` | GET | Shows map of critical clusters and outbreak zones. | `ICriticalIntelligenceService.GetCrisisHeatmapAsync` | `int? crisisId` | `Heatmap` View + `CrisisHeatmapDto` |
| `Approve` | POST | Admin approval for newly escalated outbreak zones. | `IAdminService.ApproveCrisisAsync` | `int crisisId` | Redirect to `Index` |
| `Reject` | POST | Rejects an escalated zone request with a reason. | `IAdminService.RejectCrisisAsync` | `int crisisId`, `string reason` | Redirect to `Index` |

#### 22. AdminReportsController
* **Who uses it**: General Admin
* **Responsibility**: Renders configuration settings and audit logs. Exposes [IAdminService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Settings` | GET | Renders timeouts, lockout, OCR/AI toggles. | `IAdminService.GetSystemConfigAsync` | None | `Settings` View + `SystemConfigDto` |
| `Settings` | POST | Updates system configurations. | `IAdminService.UpdateSystemConfigAsync` | `SystemConfigDto` (Form) | Redirect to `Settings` |
| `Logs` | GET | Displays admin and doctor audit trails. | `IAdminService.GetActivityLogsAsync` | `int page = 1` | `Logs` View + `List<ActivityLogDto>` |
| `Exports` | GET | Lists generated report file logs. | `IAdminService.GetReportsAsync` | `int page = 1` | `Exports` View + `PaginatedResult<AdminReportDto>` |

---

### 1.6 Chat Features

#### 23. ChatController
* **Who uses it**: Patients / Doctors
* **Responsibility**: Handles secure messaging channels. Exposes [IChatService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IChatService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Lists user's active conversations. | `IChatService.GetThreadsAsync` | None | `Index` View + `IEnumerable<ChatThreadDto>` |
| `Thread` | GET | Renders chat box with messaging history. | `IChatService.GetMessagesAsync` | `string otherUserId` | `Thread` View + `IEnumerable<ChatMessageDto>` |
| `SendMessage` | POST | Sends message to user. | `IChatService.SendMessageAsync` | `string receiverId`, `string content` (Form) | JSON Status + `ChatMessageDto` |
| `MarkRead` | POST | Marks thread messages as read. | `IChatService.MarkThreadReadAsync` | `string otherUserId` | JSON Success status |
| `GetUnreadCount` | GET | Fetches unread messages for notification badge. | `IChatService.GetUnreadCountAsync` | None | JSON Count |

#### 24. ChatbotController
* **Who uses it**: Patient / Doctor
* **Responsibility**: AI Chatbot wellness advice. Exposes [IChatbotService](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAIChatService.cs).

| Action Name | HTTP Verb | What it does | BLL Method called | Input | Returns |
|---|---|---|---|---|---|
| `Index` | GET | Renders AI Health chatbot chat page. | None | None | `Index` View |
| `Ask` | POST | Queries the Gemini API for wellness advice (AJAX). | `IChatbotService.AskAsync` | `ChatbotRequest` (Body) | JSON Response |

---

## 2. ViewModels Plan

The presentation layer uses ViewModels corresponding to the BLL's DTO contracts. The following tables detail every model, its direction, and properties.

### 2.1 Authentication & Profile ViewModels

| ViewModel Name | Used in View / Action | Data Direction | Properties List (Type, Name, Purpose) |
|---|---|---|---|
| **RegisterDto** | Account / `Register` | FROM User (Form) | <ul><li>`string FirstName`: User's first name</li><li>`string LastName`: User's last name</li><li>`string Email`: Unique email registration address</li><li>`string Password`: Account security password</li><li>`string ConfirmPassword`: Password validation match</li><li>`string? PhoneNumber`: Optional contact number</li></ul> |
| **LoginDto** | Account / `Login` | FROM User (Form) | <ul><li>`string Email`: Login email credentials</li><li>`string Password`: Login password credentials</li><li>`bool RememberMe`: Toggle for persistent session cookie</li></ul> |
| **ForgotPasswordDto** | Account / `ForgotPassword` | FROM User (Form) | <ul><li>`string Email`: The address to send recovery links to</li></ul> |
| **ResetPasswordDto** | Account / `ResetPassword` | FROM User (Form) | <ul><li>`string Email`: The target account email</li><li>`string Password`: New account password</li><li>`string ConfirmPassword`: New password match check</li><li>`string? Token`: Security token passed from route link</li></ul> |
| **ProfileDto** | PatientProfile / `Index` | BOTH (Populates & updates) | <ul><li>`string FullName`: Patient's display name</li><li>`DateTime? DateOfBirth`: Age calculations context</li><li>`string? Gender`: Gender indicator</li><li>`decimal? Height`: Height in centimeters</li><li>`decimal? Weight`: Weight in kilograms</li><li>`PhysicalActivityLevel ActivityLevel`: Enum for energy consumption</li><li>`string? BloodType`: Critical blood group value</li><li>`bool HasChronicDiseases`: Indicator for high risk</li><li>`string? ChronicDiseasesNotes`: Detailed chronic logs</li><li>`string? Allergies`: Critical drug/food allergies</li><li>`string? CurrentMedications`: Active medical treatments</li></ul> |

---

### 2.2 Patient Operations ViewModels

| ViewModel Name | Used in View / Action | Data Direction | Properties List (Type, Name, Purpose) |
|---|---|---|---|
| **DashboardDto** | PatientDashboard / `Index` | TO View | <ul><li>`string PatientName`: Display greeting</li><li>`RiskResultDto? LatestRiskAssessment`: Summary of latest risk status</li><li>`int UnreadAlertsCount`: Notifications indicator</li><li>`int UpcomingAppointmentsCount`: Calendar indicator</li><li>`decimal? LatestBmi`: Body Mass Index computed</li><li>`string? LatestBmiCategory`: BMI classification string</li><li>`List<RecentAppointmentDto> UpcomingAppointments`: Next appointments (limit 5)</li><li>`List<RecentAlertDto> RecentAlerts`: Alert logs feed</li></ul> |
| **MedicalRecordCreateDto** | MedicalRecords / `Create`, DoctorPatients / `AddMedicalRecord` | FROM User (Form) | <ul><li>`int PatientId`: Target patient profile identifier</li><li>`DateTime RecordDate`: Timestamp for entry</li><li>`decimal? SystolicBP`: Systolic blood pressure (mmHg)</li><li>`decimal? DiastolicBP`: Diastolic blood pressure (mmHg)</li><li>`decimal? BloodSugar`: Blood sugar level (mg/dL)</li><li>`decimal? HeartRate`: Pulse rate (bpm)</li><li>`decimal? Temperature`: Temp in Celsius</li><li>`decimal? OxygenSaturation`: Pulse oximetry value (%)</li><li>`string? Symptoms`: Patient reported symptom tags</li><li>`string? Diagnosis`: Doctor diagnosis text</li><li>`string? Treatment`: Doctor treatment directives</li><li>`string? Notes`: Clinical record notes</li><li>`List<string>? PrescribedMedications`: Prescribed drugs list</li></ul> |
| **MedicalRecordDto** | MedicalRecords / `Index`, DoctorPatients / `Details` | TO View | <ul><li>`int Id`: Primary key</li><li>`DateTime RecordDate`: Date vitals were logged</li><li>`decimal? SystolicBP`: Blood pressure systolic</li><li>`decimal? DiastolicBP`: Blood pressure diastolic</li><li>`decimal? BloodSugar`: Blood sugar levels</li><li>`decimal? HeartRate`: Cardiac rate</li><li>`decimal? Temperature`: Temperature</li><li>`decimal? OxygenSaturation`: Oxygen levels</li><li>`string? Symptoms`: Symptoms logged</li><li>`string? Notes`: Diagnosis/medication logs details</li></ul> |
| **LabUploadDto** | LabResults / `Upload` | FROM User (Form) | <ul><li>`int PatientId`: Target patient profile ID</li><li>`string TestName`: Name of medical test (e.g. CBC, Lipid Profile)</li><li>`DateTime TestDate`: Date the sample was analyzed</li><li>`string? FilePath`: File system path of the uploaded document</li><li>`bool UseOcr`: Flags file for background OCR extraction</li></ul> |
| **LabResultDto** | LabResults / `Index` | TO View | <ul><li>`int Id`: Primary key</li><li>`string TestName`: Clinical name of test</li><li>`DateTime TestDate`: Test execution date</li><li>`string? FilePath`: Document path</li><li>`string? FileUrl`: Clickable web access link to report</li><li>`string? OcrExtractedData`: Extracted text from PDF/image</li><li>`string? Results`: Audit logs and verified findings</li><li>`DateTime CreatedAt`: Audit timestamp</li></ul> |
| **RiskInputDto** | RiskAssessment / `Index` | FROM User (Form) | <ul><li>`string? Symptoms`: Comma-separated list of selected symptoms</li><li>`decimal? HeartRate`: Measured pulse</li><li>`decimal? SystolicBP`: Measured systolic blood pressure</li><li>`decimal? DiastolicBP`: Measured diastolic blood pressure</li><li>`decimal? Temperature`: Measured body temperature</li><li>`decimal? OxygenSaturation`: Pulse oximeter reading</li><li>`decimal? BloodSugar`: Measured glucose</li><li>`decimal? Latitude`: GPS location latitude</li><li>`decimal? Longitude`: GPS location longitude</li></ul> |
| **RiskResultDto** | RiskAssessment / `Result` | TO View | <ul><li>`decimal RiskScore`: Calculated numeric score (0-1)</li><li>`RiskLevel RiskLevel`: Severity classification (Emergency, High, Medium, Low)</li><li>`string RiskColor`: UI display color indicator (danger, warning, success)</li><li>`string RiskLabel`: Arabic display label (طارئ, مرتفع, الخ)</li><li>`bool IsEmergency`: True if critical vital limit breached</li><li>`List<string> Recommendations`: List of tailored actions</li><li>`List<string> TriggeredSymptoms`: List of critical symptoms detected</li><li>`string? NearestEmergencyCenter`: Name of nearest hospital provider</li><li>`bool WasAutoEscalated`: True if escalated to dispatch queue</li><li>`int? EmergencyRequestId`: Emergency request identifier</li><li>`string? EscalationMessage`: Explanation of triage actions</li></ul> |
| **NearbySearchDto** | NearbyProviders / `Index` | BOTH | <ul><li>`decimal Latitude`: Center coordinates latitude</li><li>`decimal Longitude`: Center coordinates longitude</li><li>`string Type`: Provider filter type (Hospital, Clinic)</li><li>`int RadiusInKm`: Search query perimeter</li></ul> |
| **EmergencyRequestDto** | Emergency / `RequestAmbulance` | FROM User (Form) | <ul><li>`int PatientProfileId`: Caller profile identifier</li><li>`decimal Latitude`: Precise GPS latitude</li><li>`decimal Longitude`: Precise GPS longitude</li><li>`string? EmergencyType`: Primary distress category</li><li>`string? Description`: Optional detail notes</li></ul> |
| **FamilyInviteDto** | FamilyLinking / `Invite` | FROM User (Form) | <ul><li>`int LinkedPatientId`: Relative's profile identifier</li><li>`string Relationship`: Relationship type label</li><li>`bool CanViewRecords`: Grant view access to vitals/records</li><li>`bool CanViewRisk`: Grant access to risk assessments</li><li>`bool CanBookAppointments`: Grant permission to schedule visits</li></ul> |
| **FamilyDto** | FamilyLinking / `Index` | TO View | <ul><li>`int Id`: Primary key link</li><li>`string Relationship`: Relationship label</li><li>`bool IsAccepted`: Invite confirmation status</li><li>`bool CanViewRecords`: Records permission setting</li><li>`bool CanViewRisk`: Risk permission setting</li><li>`bool CanBookAppointments`: Appointment booking permission setting</li><li>`string LinkedPatientName`: Linked relation's name</li><li>`DateTime CreatedAt`: Link creation timestamp</li></ul> |

---

### 2.3 Doctor ViewModels

| ViewModel Name | Used in View / Action | Data Direction | Properties List (Type, Name, Purpose) |
|---|---|---|---|
| **DoctorProfileDto** | DoctorProfile / `Index` | BOTH | <ul><li>`int Id`: Primary profile key</li><li>`string FullName`: Doctor's display name</li><li>`string? Specialization`: Clinical specialization</li><li>`string? LicenseNumber`: State medical license number</li><li>`int? YearsOfExperience`: Experience level</li><li>`string? Bio`: Professional bio statement</li><li>`decimal? ConsultationFee`: Clinic pricing</li><li>`bool IsAvailable`: Toggle to accept appointments</li><li>`string? Email`: Login email</li><li>`string? PhoneNumber`: Contact phone</li></ul> |
| **DoctorDashboardDto** | DoctorDashboard / `Index` | TO View | <ul><li>`string DoctorName`: Display greeting name</li><li>`string? Specialization`: Specialization label</li><li>`int TodayAppointmentsCount`: Scheduled patients count for today</li><li>`int PendingAppointmentsCount`: Awaiting confirmation</li><li>`int TotalPatientsCount`: Distinct patient count</li><li>`decimal? AverageRating`: Rating metrics</li><li>`List<UpcomingAppointmentDto> UpcomingAppointments`: Daily list view</li><li>`List<RecentPatientDto> RecentPatients`: Patient history feed</li></ul> |
| **DoctorStatisticsDto**| DoctorDashboard / `Index` (Stats) | TO View | <ul><li>`int TotalAppointments`: All logged appointments</li><li>`int CompletedAppointments`: Success sessions count</li><li>`int CancelledAppointments`: Cancelled sessions count</li><li>`int NoShowAppointments`: Missed sessions count</li><li>`decimal CompletionRate`: Percentage completion metrics</li><li>`int TotalPatients`: Patients served</li><li>`int NewPatientsThisMonth`: Volume metrics</li><li>`decimal? AverageConsultationFee`: Fee metrics</li></ul> |
| **DoctorAppointmentDto**| DoctorAppointments / `Index` | TO View | <ul><li>`int Id`: Primary key</li><li>`int PatientId`: Patient ID</li><li>`string PatientName`: Patient display name</li><li>`string? PatientPhone`: Patient phone number</li><li>`string? PatientEmail`: Patient email address</li><li>`DateTime AppointmentDate`: Scheduled date</li><li>`TimeSpan StartTime`: Start slot</li><li>`TimeSpan EndTime`: End slot</li><li>`string Status`: Appointment status string</li><li>`string? Notes`: Diagnosis/consult notes</li></ul> |
| **UpdateAppointmentStatusDto**| DoctorAppointments / `UpdateStatus` | FROM User (Form) | <ul><li>`int AppointmentId`: Target booking ID</li><li>`string Status`: Target status string (Scheduled, Confirmed, Completed)</li><li>`string? Notes`: Doctor consult summary</li></ul> |
| **DoctorPanicInboxDto**| DoctorPanicInbox / `Index` | TO View | <ul><li>`string DoctorUserId`: Active doctor account ID</li><li>`string DoctorName`: Doctor name</li><li>`int TotalCriticalCases`: Active critical caseload</li><li>`int AssignedToDoctor`: Claims count</li><li>`int UnassignedCriticalCases`: Unclaimed caseload pool</li><li>`List<DoctorPanicInboxItemDto> Items`: Critical cases detailed list</li></ul> |

---

### 2.4 Hospital Staff & Triage ViewModels

| ViewModel Name | Used in View / Action | Data Direction | Properties List (Type, Name, Purpose) |
|---|---|---|---|
| **HospitalStaffQueueDto**| HospitalQueue / `Index` | TO View | <ul><li>`int? ProviderId`: Center profile identifier</li><li>`string? ProviderName`: Center name</li><li>`int PendingCount`: Queue count</li><li>`int AcceptedCount`: Claims count</li><li>`int EscalatedCount`: Red-flagged count</li><li>`int? AvailableBeds`: Bed capacity</li><li>`List<HospitalStaffQueueItemDto> Items`: Active queue list</li></ul> |
| **HospitalStaffQueueItemDto**| HospitalQueue / `Index` (List) | TO View | <ul><li>`int RequestId`: Emergency record ID</li><li>`int PatientProfileId`: Patient profile ID</li><li>`string PatientName`: Patient name</li><li>`string? PatientPhone`: Patient contact</li><li>`string EmergencyType`: Category label</li><li>`EmergencyRequestStatus Status`: Triage status</li><li>`DateTime RequestedAt`: Request timestamp</li><li>`int WaitingMinutes`: Elapsed waiting minutes</li><li>`bool IsAutoGenerated`: Heuristics source</li><li>`int PriorityScore`: Calculated triage priority (700-1100)</li></ul> |
| **HospitalStaffEmergencyDetailDto**| HospitalQueue / `Details` | TO View | <ul><li>`int RequestId`: Emergency request identifier</li><li>`EmergencyRequestStatus Status`: Current triage state</li><li>`string EmergencyType`: Categorization type</li><li>`string? Description`: Incident description</li><li>`DateTime RequestedAt`: Initial call time</li><li>`DateTime? AcceptedAt`: Acceptance time</li><li>`string? ResponseNotes`: Hospital response notes</li><li>`string PatientName`: Patient name</li><li>`string? PatientPhone`: Patient phone number</li><li>`string? BloodType`: Blood group context</li><li>`bool HasChronicDiseases`: Chronic status indicator</li><li>`string? ChronicDiseasesNotes`: Chronic logs</li><li>`string? Allergies`: Allergies</li><li>`string? CurrentMedications`: Medications</li><li>`decimal? Latitude`: Lat coordinate</li><li>`decimal? Longitude`: Long coordinate</li><li>`int? AssignedProviderAvailableBeds`: Current hospital capacity</li></ul> |
| **HospitalStaffEmergencyRespondDto**| HospitalQueue / `Respond` | FROM User (Form) | <ul><li>`int RequestId`: Triage case ID</li><li>`int ProviderId`: Responding hospital ID</li><li>`string Status`: Target state (Accepted, Rejected, Completed)</li><li>`string? ResponseNotes`: Audited rejection/action reasons</li></ul> |
| **HospitalStaffBedsUpdateDto**| HospitalQueue / `UpdateBeds` | FROM User (Form) | <ul><li>`int ProviderId`: Target hospital identifier</li><li>`int AvailableBeds`: New bed count</li></ul> |

---

### 2.5 Administrator & Crisis ViewModels

| ViewModel Name | Used in View / Action | Data Direction | Properties List (Type, Name, Purpose) |
|---|---|---|---|
| **AdminDashboardDto** | AdminDashboard / `Index` | TO View | <ul><li>`int TotalUsers`: Global user count</li><li>`int ActiveDoctors`: Verified active doctors</li><li>`int ActivePatients`: Active patient profiles</li><li>`int TotalAppointments`: System bookings total</li><li>`int PendingEmergencyRequests`: Escalations waiting in queue</li><li>`bool IsCrisisModeActive`: System mode status</li><li>`string? ActiveCrisisName`: Name of active epidemic</li></ul> |
| **SystemConfigDto** | AdminReports / `Settings` | BOTH | <ul><li>`bool EnableCrisisMode`: Global toggle for epidemic settings</li><li>`bool EnableAIChat`: Gemini API access toggle</li><li>`bool EnableOCR`: OCR scanning toggle</li><li>`bool EnableFamilyLinking`: Consent sharing toggle</li><li>`bool EnableEmergencyRequests`: Ambulance dispatch toggle</li><li>`int MaxLoginAttempts`: Security lockout threshold</li><li>`int LockoutDurationMinutes`: Penalty lockout period</li></ul> |
| **CreateCrisisDto** | AdminCrisis / `Create` | FROM User (Form) | <ul><li>`string CrisisName`: Name of outbreak</li><li>`CrisisType CrisisType`: Outbreak category enum</li><li>`SystemMode SystemMode`: Target system mode (Normal, Crisis)</li><li>`string? Description`: Crisis notes</li><li>`DateTime StartDate`: Start date</li><li>`DateTime? EndDate`: End date</li><li>`decimal EmergencyThreshold`: Emergency score target</li><li>`decimal HighRiskThreshold`: High risk score target</li><li>`decimal MediumRiskThreshold`: Medium risk score target</li></ul> |
| **CrisisConfigurationDto**| AdminCrisis / `Details` | TO View | <ul><li>`int Id`: Primary key</li><li>`string CrisisName`: Epidemic name</li><li>`CrisisType CrisisType`: Epidemic classification</li><li>`SystemMode SystemMode`: System mode</li><li>`bool IsActive`: Active configuration indicator</li><li>`DateTime StartDate`: Start date</li><li>`List<SymptomWeightDto> SymptomWeights`: Outbreak weights configuration</li><li>`int ZonesCount`: Active buffer zones</li></ul> |
| **SymptomWeightDto** | AdminCrisis / `AddSymptom` | BOTH | <ul><li>`string SymptomName`: Symptom name</li><li>`decimal Weight`: Weight multiplier (0-1)</li><li>`bool IsEmergencySymptom`: Emergency status flag</li></ul> |
| **CriticalCommandCenterDto**| AdminCrisis / `CommandCenter` | TO View | <ul><li>`int ActiveCriticalCases`: Total active cases</li><li>`int WaitingForHospital`: Pending bed reservation cases</li><li>`int HospitalAccepted`: Hospital-admitted cases</li><li>`int WaitingForDoctor`: Unassigned critical cases</li><li>`int DoctorAssigned`: Doctor-assigned cases</li><li>`decimal AverageWaitingMinutes`: System response time metric</li><li>`List<CriticalCommandCenterItemDto> Cases`: Grid cases</li></ul> |
| **CrisisHeatmapDto** | AdminCrisis / `Heatmap` | TO View | <ul><li>`int? CrisisId`: Target crisis ID</li><li>`int TotalGeoTaggedCriticalCases`: Heat points count</li><li>`List<CrisisHeatmapPointDto> Points`: Coordinates list</li><li>`List<CrisisHeatmapZoneDto> Zones`: Radius buffers list</li></ul> |

---

## 3. Views Plan

The views plan maps out Razor files. Designs emphasize dynamic elements (glassmorphism layouts, dynamic chart canvas components, alert notification feeds, and maps).

### 3.1 Account Views
* **`Register.cshtml`**: Renders registration form. Input validation highlights fields on blur. Links to Login view.
* **`Login.cshtml`**: Login form with remember me, forgot password links, and social logins.
* **`ForgotPassword.cshtml`**: Accepts email address to request a reset link.
* **`ResetPassword.cshtml`**: Form to enter new password.

### 3.2 Patient Area Views
* **PatientDashboard / `Index.cshtml`**: Displays dynamic risk score meter (colored dynamically using `RiskColor`), upcoming appointments, unread alert bell, and BMI category indicators. Displays alert toasts.
* **PatientProfile / `Index.cshtml`**: Multi-tab profile card to edit clinical context metrics.
* **MedicalRecords / `Index.cshtml`**: Renders history list of previous logs. Includes slide-out panel containing form with `MedicalRecordCreateDto` fields for manual vitals logging.
* **LabResults / `Index.cshtml`**: List of uploads with verified statuses. Form to submit lab reports with OCR toggle.
* **RiskAssessment / `Index.cshtml`**: Renders interactive clinical self-assessment checklist. Renders custom checkboxes for common symptoms. Shows caution warnings when critical levels are entered.
* **RiskAssessment / `Result.cshtml`**: Displays calculated risk score. Renders immediate action recommendations (red alert boxes for critical risk, green for low risk) and lists nearby hospital emergency centers.
* **NearbyProviders / `Index.cshtml`**: Renders GPS search panel and map displaying nearby hospitals. Shows list of `ProviderDto` cards with distances and booking buttons.
* **Emergency / `Track.cshtml`**: Real-time dispatch tracking. Renders progress bar (Pending -> Accepted/Dispatched -> Arrived) and details for the assigned provider.
* **FamilyLinking / `Index.cshtml`**: Renders linked relatives list. Form modal for `FamilyInviteDto` to send invitations.

### 3.3 Doctor Area Views
* **DoctorDashboard / `Index.cshtml`**: Telemetry panels showing appointment counts, recent patient profiles, and statistics charts.
* **DoctorAppointments / `Index.cshtml`**: Calendar and list layouts filterable by status (Scheduled, Confirmed, Completed).
* **DoctorAppointments / `Details.cshtml`**: View details of scheduling. Contains form status updater using `UpdateAppointmentStatusDto`.
* **DoctorSlots / `Index.cshtml`**: Availability calendar. Contains modal forms for single and bulk slots creation.
* **DoctorPatients / `Search.cshtml`**: Patient search bar. Displays results list in partial views with details links.
* **DoctorPatients / `Details.cshtml`**: Comprehensive patient chart. Renders AI summary findings and deterioration risk warnings. Contains diagnostic input form using `MedicalRecordCreateDto` to save consultation records.
* **DoctorPanicInbox / `Index.cshtml`**: Grid of high-risk cases. Displays patient name, score, and suggested first message. Offers option to self-claim unassigned cases.

### 3.4 Hospital Staff Views
* **HospitalQueue / `Index.cshtml`**: Live ambulance dispatch console. Shows patient counts in different triage stages. Contains form to adjust available beds count.
* **HospitalQueue / `Details.cshtml`**: Comprehensive profile details for triage (blood group, allergies). Renders accept/reject/escalate action buttons.

### 3.5 Admin Area Views
* **AdminDashboard / `Index.cshtml`**: System stats dashboard (active users, appointments, crisis banners).
* **AdminUsers / `Index.cshtml`**: Users table. User status toggle (active/inactive) and role adjustments form.
* **AdminCrisis / `Index.cshtml`**: Crisis templates list. Contains activate/deactivate toggles.
* **AdminCrisis / `Details.cshtml`**: Displays crisis details. Symptom weights config panel with CRUD tools. Lists active zones.
* **AdminCrisis / `CommandCenter.cshtml`**: Live dispatch status grid showing case stages and provider details.
* **AdminCrisis / `Heatmap.cshtml`**: Interactive geographical map displaying case coordinates (colored by severity) and outbreak buffer circles.
* **AdminReports / `Settings.cshtml`**: Toggles for OCR, AI, crisis settings, session times.
* **AdminReports / `Logs.cshtml`**: List of admin audit logs.
* **AdminReports / `Exports.cshtml`**: Lists generated report file logs.

### 3.6 Chat Views
* **Chat / `Index.cshtml`**: Active chat threads list. Displays unread count indicators.
* **Chat / `Thread.cshtml`**: Scrollable message window. Message inputs with dynamic send button.
* **Chatbot / `Index.cshtml`**: AI Health chatbot page. Text area to ask health questions. Displays answers with formatted bullet lists.

---

## 4. User Stories

The BLL supports clinical features, emergency actions, and crisis mode settings. Below are user stories organized by role.

### 4.1 Guests
* *As a Guest, I can register a new account, so that I can access personalized health tracking features.*
* *As a Guest, I can verify my email using a link, so that my account becomes active.*
* *As a Guest, I can request password recovery, so that I can regain access if I forget my password.*
* *As a Guest, I can view public landing pages and see if there is an active health crisis mode in my region.*

### 4.2 Patients
* *As a Patient, I can view my dashboard, so that I can see my latest health status, upcoming appointments, and notifications.*
* *As a Patient, I can update my health profile (height, weight, chronic conditions, allergies, medications), so that the system and doctors have accurate clinical contexts.*
* *As a Patient, I can manually log my vitals (blood pressure, blood sugar, oxygen, temperature), so that I can track my metrics over time.*
* *As a Patient, I can upload my lab results with options for OCR analysis, so that my lab values are recorded and parsed in my medical file.*
* *As a Patient, I can perform a risk self-assessment by entering my vitals and selecting current symptoms, so that I can determine if I need urgent medical attention.*
* *As a Patient, I can view detailed results of my risk assessment along with clinical recommendations and dynamic color indicators, so that I understand my risk level.*
* *As a Patient, I can search for nearby healthcare providers and view their available slots, so that I can find a nearby clinic or hospital.*
* *As a Patient, I can book an appointment slot with a doctor, so that I can secure a consultation time.*
* *As a Patient, I can view my upcoming and past appointments and cancel them if necessary, so that I can manage my medical schedule.*
* *As a Patient, I can trigger an emergency request with my current location coordinates, so that an ambulance and nearby emergency center can assist me.*
* *As a Patient, I can track my active emergency request distance, provider name, and arrival status, so that I know when help will arrive.*
* *As a Patient, I can invite my family members to link accounts and manage their permissions to view my medical records or risk results, so that they are aware of my health status.*
* *As a Patient, I can accept a family link invitation, so that I can link accounts with my family.*
* *As a Patient, I can view my notifications and alerts, and mark them as read, so that my dashboard stays clean.*
* *As a Patient, I can chat with my assigned doctor, so that I can receive online consultation and follow-up.*
* *As a Patient, I can chat with the Gemini AI chatbot, so that I can ask wellness and lifestyle questions and get immediate motivational health tips.*

### 4.3 Doctors
* *As a Doctor, I can view my dashboard and scheduling statistics, so that I can see today's agenda and patient completion rates.*
* *As a Doctor, I can update my professional profile, specialization, experience, bio, fee, and availability status, so that patients see accurate booking details.*
* *As a Doctor, I can list, add, delete, and bulk-create availability slots, so that I can configure my clinic hours.*
* *As a Doctor, I can view my scheduled appointments and update their status (Scheduled, Confirmed, Completed, Cancelled, No-Show) with clinical notes, so that I keep track of each consult.*
* *As a Doctor, I can search for patient profiles by name, phone, or email, so that I can access their medical details.*
* *As a Doctor, I can view a patient's full medical history, past risk scores, and previous lab uploads, so that I can make informed diagnoses.*
* *As a Doctor, I can view an AI-generated medical summary for a patient highlighting critical findings and missing data, so that I can quickly review complex records.*
* *As a Doctor, I can view an AI deterioration probability trend for a patient, so that I can identify high-risk patients who might worsen.*
* *As a Doctor, I can add a medical record entry (diagnosis, treatment, vitals, prescribed medications) for a patient, so that their file is updated with our consultation details.*
* *As a Doctor, I can view the Panic Inbox listing urgent escalated critical cases, so that I can see who needs emergency clinical attention.*
* *As a Doctor, I can self-assign an unassigned critical case from the Panic Inbox, so that I can initiate contact with the patient.*
* *As a Doctor, I can chat with patients who are assigned to me or have an active emergency escalation, so that I can guide them through critical situations.*

### 4.4 Hospital Staff
* *As a Hospital Staff, I can view the emergency triage queue assigned to my hospital, so that I see pending, accepted, and escalated cases.*
* *As a Hospital Staff, I can view the detailed medical information (blood type, allergies, chronic illnesses) and location coordinates of an emergency patient, so that my team is prepared.*
* *As a Hospital Staff, I can accept an emergency request (reserving an emergency bed) or reject it (triggering automatic escalation to another center), so that the patient is triaged appropriately.*
* *As a Hospital Staff, I can update my facility's available emergency beds, so that the system has real-time capacity coordinates.*

### 4.5 System Administrators
* *As an Admin, I can view system dashboard KPIs and statistics, so that I can monitor platform usage and health metrics.*
* *As an Admin, I can manage user accounts (list, view details, activate, deactivate, change roles) individually or in bulk, so that I can enforce platform policies.*
* *As an Admin, I can manage healthcare providers (CRUD provider profiles, coordinates, bed configurations), so that they are visible to patients and emergency systems.*
* *As an Admin, I can view and audit activity logs, so that I can trace all administrative actions on the platform.*
* *As an Admin, I can view and export system reports, so that I can analyze health metrics and emergency request outcomes.*
* *As an Admin, I can adjust system configurations (session timeout, lockout limit, feature toggles), so that the platform meets security and operational requirements.*
* *As an Admin, I can view the Live Command Center showing operational status updates, active emergency request counts, and hospital/doctor engagements, so that I can supervise triage operations.*
* *As an Admin, I can view the crisis heatmap indicating geographical clusters of critical cases and active outbreak zones, so that I can identify epidemiological hot spots.*
* *As an Admin, I can CRUD crisis configurations (thresholds, start/end dates, modes), so that the system is configured to handle specific epidemics.*
* *As an Admin, I can CRUD symptom weights for a crisis, so that the risk calculator weights specific symptoms differently during an outbreak.*
* *As an Admin, I can activate or deactivate a crisis configuration, so that the system switches to crisis mode and alters risk algorithms.*
* *As an Admin, I can approve or reject crisis-related escalations or outbreak zones, so that I validate critical actions manually.*

---

## 5. BLL Observations & Recommendations

A deep structural review of the BLL codebase reveals significant accomplishments, along with several gaps, bugs, and inconsistencies that should be addressed before deploying to production.

### 5.1 BLL Findings by Feature Area

#### 1. System Authentication ([AuthService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/AuthService.cs))
* **BLL Confirmation**: Handles user registration via Identity's `UserManager`, validates duplicate emails, generates verification tokens, and hashes passwords. It links the registration step with the DAL by initializing a `PatientProfile` and calling `_uow.PatientProfiles.AddAsync(profile)`.
* **Gaps/Bugs**: Account lockout is initialized (`lockoutOnFailure: true`) during password verification, but the lockout timespan configuration settings are not processed in the BLL. They are defined in the presentation layer (`Program.cs`) instead of the BLL.
* **Recommendations**: Relocate session lockout check validations to a centralized BLL policy helper to keep business rules independent of the presentation layer.

#### 2. Clinical Risk Engine ([RiskCalculatorHelper.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Helpers/RiskCalculatorHelper.cs), [PatientService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/PatientService.cs))
* **BLL Confirmation**: Performs vitals evaluation using clinical baselines. Features evaluation of systolic/diastolic blood pressure, pulse, temperature, blood sugar, oxygen levels, and match checking for both Arabic and English symptoms.
* **Gaps/Bugs**: The oxygen scoring logic sets risk to `1.0m` (highest) if o2 is below 85%, and o2 below 90% triggers `isEmergency = true`. However, the calculation formula uses a division by the count of active vital factors: `Math.Round(totalScore / factors, 2)`. If a patient reports low o2 but normal blood pressure and temperature, the critical o2 risk gets averaged down by the normal vitals, yielding a low composite score. This is a potential clinical risk.
* **Recommendations**: Change the risk aggregator to use a non-linear scaling formula or a weighted average where oxygen saturation below 90% forces the composite score to a minimum value of `0.80` regardless of other normal vitals.

#### 3. Automatic Escalation Triaging ([CriticalCareEscalationService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/CriticalCareEscalationService.cs))
* **BLL Confirmation**: Monitors patient risk assessments. When a high-risk score is generated, it checks if an active auto-generated escalation already exists, and if not, creates a new `EmergencyRequest` with status `Escalated` and a priority score.
* **Gaps/Bugs**: Sends alerts and notifications to all available doctors using a database loop: `_uow.Alerts.AddAsync(...)`. If there are many active doctors on the platform, executing this inline during a patient's risk submission will slow down the HTTP response.
* **Recommendations**: Delegate notification sending to an out-of-process background worker using Hangfire or a hosted background service to avoid blocking the user request.

#### 4. Clinical Intelligence Dashboard ([CriticalIntelligenceService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/CriticalIntelligenceService.cs))
* **BLL Confirmation**: Computes waiting time averages for patients in the emergency queue, manages the doctor's Panic Inbox, determines doctor assignments by matching specializations to patient symptoms, and calculates deterioration probabilities.
* **Gaps/Bugs**: The deterioration logic uses a simple rule-based approach (e.g., if O2 is below 94%, add 15% to risk). This is a simple heuristic rather than an AI-based prediction model.
* **Recommendations**: Clearly document that this is a rule-based algorithm rather than a machine learning model, or integrate a simple linear regression predictor to project vitals trends based on historical data.

#### 5. Hospital Triaging Queue ([HospitalStaffService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/HospitalStaffService.cs))
* **BLL Confirmation**: Handles emergency requests assigned to a specific hospital. When staff accept a case, it reserves an emergency bed: `provider.AvailableBeds = Math.Max(0, (provider.AvailableBeds ?? 0) - 1)`.
* **Gaps/Bugs**: State transitions are validated by `ValidateTransition`. If a case is rejected, the status changes to `Rejected`, but the assigned hospital ID remains unchanged. The code says: `request.HealthcareProviderId ??= provider.Id`. If a hospital rejects a request, it should clear the provider assignment or escalate it to another provider, rather than locking it to the rejecting hospital.
* **Recommendations**: Modify `ApplyResponse` during rejection to clear `HealthcareProviderId` and increase the triage priority score so the case is highlighted for assignment to other facilities.

#### 6. Lab Analysis & File Processing ([LabService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/LabService.cs))
* **BLL Confirmation**: Manages lab records, detects abnormal findings by searching OCR text and results for keywords, and tracks verification audit logs.
* **Gaps/Bugs**: If the user checks the OCR toggle, it sets `OcrExtractedData` to `"OCR processing pending."`. However, there is no actual OCR processor or background worker implemented in the BLL.
* **Recommendations**: Define an `IOcrService` interface and implement it using a library like Tesseract OCR or Azure Form Recognizer, processing the uploads asynchronously.

#### 7. AI Health Chatbot ([AIChatService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/AIChatService.cs))
* **BLL Confirmation**: Integrates with the Gemini API using an HTTP client. Configures system instructions that restrict responses to wellness topics and prevent the AI from diagnosing conditions or prescribing medications.
* **Gaps/Bugs**: The HttpClient timeout is hardcoded to 30 seconds. If the API is slow, the connection will drop.
* **Recommendations**: Move the timeout configuration and connection keys to `appsettings.json` to configure the service settings dynamically.

---

### 5.2 Critical BLL Gaps and Mismatches

#### 1. Mismatch on User ID Types (Identity vs. Profiles)
* **Finding**: The ASP.NET Core Identity table (`AspNetUsers`) uses a `string` GUID for `ApplicationUser.Id`. This is implemented correctly in `ApplicationUser.cs` and `AuthService.RegisterAsync` (where `ApplicationUserId = user.Id` is mapped).
* **Mismatch in AdminService**: Interface declarations in [IAdminService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/IServices/IAdminService.cs) define methods with `int userId`:
  * `Task<ServiceResult<UserListItemDto>> GetUserByIdAsync(int userId);`
  * `Task<ServiceResult> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);`
  * `Task<ServiceResult> DeleteUserAsync(int userId);`
* **Mismatch in Alert/Notification Services**:
  * `IAlertService.CreateAlertAsync` takes `int userId`, but the `Alert` database entity maps `UserId` to a `string` foreign key.
  * `INotificationService.MarkAllAsReadAsync` and `GetUnreadCountAsync` take `int userId`, but the notification table uses a `string` foreign key for user association.
* **Impact**: These type mismatches will prevent the code from compiling or cause runtime queries to fail.
* **Resolution**: Standardize all user identifiers in the BLL. Use `string applicationUserId` when referencing the core security account, and `int patientProfileId` or `int doctorProfileId` when referencing specific profile entities.

#### 2. Empty AdminService Implementation ([AdminService.cs](file:///c:/Users/Kozmo0_2/source/repos/Etmen_DEPI_Project-ElSherka/Etmen_BLL/Repositories/Services/AdminService.cs))
* **Finding**: `AdminService.cs` is completely stubbed out with `NotImplementedException` for all 19 methods.
* **Impact**: Key administrative functions, such as system configurations, provider registration, reports, and user activation, are not functional.
* **Resolution**: Implement the methods in `AdminService.cs` using the DAL unit of work, querying the `HealthcareProviders`, `ActivityLogs`, and Identity services.

#### 3. Duplicate Booking Business Logic
* **Finding**: Both `IAppointmentService.BookAppointmentAsync` and `INearbyService.BookAppointmentAsync` handle slot booking.
* **Impact**: Maintaining booking logic in two separate service files increases maintenance complexity and can lead to inconsistent state checks.
* **Resolution**: Consolidate all booking actions inside `AppointmentService` and have `NearbyService` reference it for scheduling operations.

#### 4. Hardcoded Status Strings in DTOs
* **Finding**: State changes (such as appointment or emergency updates) are passed as raw strings in DTOs (e.g., `UpdateAppointmentStatusDto.Status`, `EmergencyUpdateDto.Status`) and parsed at runtime using `Enum.TryParse`.
* **Impact**: Increases the risk of validation errors.
* **Resolution**: Refactor the DTO definitions to use strongly-typed enums (`AppointmentStatus`, `EmergencyRequestStatus`) to catch validation errors early during model binding.

---

### 5.3 Presentation Layer Architecture Suggestions

* **Use ASP.NET Core Areas**: Use Areas to separate the namespaces and layouts for different roles (`Admin`, `Doctor`, `Hospital`, `Patient`), corresponding to the separate workspaces in the design plan.
* **Real-time Triage Integration**: Use SignalR in `HospitalQueueController` and `EmergencyController` to stream ambulance dispatch queues and update triage statuses without requiring manual page refreshes.
* **Dynamic Alert System**: Add a layout component that checks `/Chat/GetUnreadCount` and `/api/notifications/unread` to display real-time notifications.
* **Consistent Error Handling**: Ensure the controllers catch BLL error results and map them to `ModelState` using the `ServiceResult` wrapper.
