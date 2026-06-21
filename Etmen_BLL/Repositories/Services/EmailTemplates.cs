

namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Generates beautiful, responsive Arabic HTML email templates for all Etmen platform events.
    /// All templates use inline CSS and table-based layouts for maximum email-client compatibility.
    /// </summary>
    internal static class EmailTemplates
    {
        // ── Shared brand colors ──────────────────────────────────────────
        private const string ColorPrimary  = "#1a6b5a";   // Teal green
        private const string ColorAccent   = "#22c55e";   // Bright green
        private const string ColorWarning  = "#f59e0b";   // Amber
        private const string ColorDanger   = "#ef4444";   // Red
        private const string ColorBg       = "#f0fdf4";   // Light mint
        private const string ColorCard     = "#ffffff";
        private const string ColorText     = "#1f2937";
        private const string ColorMuted    = "#4b5563";   // Darker gray for better accessibility
        private const string ColorBorder   = "#e5e7eb";

        // ────────────────────────────────────────────────────────────────
        // 1. ACCOUNT ACTIVATION
        // ────────────────────────────────────────────────────────────────
        public static string AccountActivation(string name, string activationLink, string role)
        {
            var roleAr = role == "Doctor" ? "طبيب" : "مريض";
            return Wrap($@"
                <div style='text-align:center; margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>✉️</span>
                    </div>
                    <h1 style='color:{ColorPrimary};font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تفعيل حسابك</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>منصة اطمئن الطبية</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    شكراً لتسجيلك في <strong>منصة اطمئن</strong> كـ<strong>{roleAr}</strong>.
                    خطوة واحدة فقط تفصلك عن الوصول لحسابك — اضغط على الزر أدناه لتفعيل بريدك الإلكتروني.
                </p>

                <div style='text-align:center;margin:36px 0;'>
                    <a href='{activationLink}'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 40px;border-radius:12px;font-size:17px;font-weight:700;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 4px 15px rgba(26,107,90,0.35);'>
                        ✅ تفعيل الحساب الآن
                    </a>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f8fafc;border-right:4px solid {ColorWarning};border-radius:8px;margin-top:24px;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:{ColorMuted};font-size:13px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                ⏱️ هذا الرابط صالح لمدة <strong>24 ساعة</strong> فقط.<br>
                                إذا لم تقم بالتسجيل، يمكنك تجاهل هذا البريد بأمان.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تفعيل الحساب");
        }

        // ────────────────────────────────────────────────────────────────
        // 2. WELCOME EMAIL (after activation)
        // ────────────────────────────────────────────────────────────────
        public static string Welcome(string name, string role)
        {
            var isDoctor = role == "Doctor";
            var roleAr = isDoctor ? "الطبيب" : "المريض";
            var features = isDoctor
                ? new[]
                {
                    ("📅", "إدارة المواعيد",        "قبل وإلغاء مواعيد مرضاك بسهولة"),
                    ("👥", "متابعة المرضى",          "اطّلع على السجلات الطبية ونتائج التحاليل"),
                    ("⚠️", "نظام الإنذار المبكر",    "تلقَّ تنبيهات فورية عند ارتفاع مستوى الخطر"),
                    ("💬", "التواصل الآمن",          "تواصل مع مرضاك عبر الدردشة الآمنة"),
                }
                : new[]
                {
                    ("🏥", "حجز المواعيد",           "احجز موعداً مع أفضل الأطباء بنقرة واحدة"),
                    ("🔬", "نتائج التحاليل",          "تتبع نتائج تحاليلك وأشعتك في مكان واحد"),
                    ("📊", "تقييم مستوى الخطر",       "احصل على تقييم فوري لحالتك الصحية"),
                    ("👨‍👩‍👧", "ربط العائلة",             "اربط أفراد عائلتك لمتابعة صحتهم"),
                };

            // Use robust table layouts instead of display:flex for full Outlook compatibility
            var featureCards = string.Join("", features.Select(f => $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background:{ColorBg};border-radius:12px;margin-bottom:12px;border:1px solid rgba(26,107,90,0.1);'>
                    <tr>
                        <td style='padding:16px;width:32px;vertical-align:top;font-size:24px;'>{f.Item1}</td>
                        <td style='padding:16px 0 16px 16px;vertical-align:top;'>
                            <div style='color:{ColorPrimary};font-weight:700;font-size:15px;font-family:""Cairo"",sans-serif;'>{f.Item2}</div>
                            <div style='color:{ColorMuted};font-size:13px;margin-top:2px;font-family:""Cairo"",sans-serif;'>{f.Item3}</div>
                        </td>
                    </tr>
                </table>
            "));

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='font-size:56px;margin-bottom:12px;'>🎉</div>
                    <h1 style='color:{ColorPrimary};font-size:28px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>أهلاً بك يا {name}!</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>انضم الآن إلى مجتمع اطمئن الطبي كـ<strong>{roleAr}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    تم تفعيل حسابك بنجاح 🎊 — إليك ما يمكنك فعله الآن:
                </p>

                {featureCards}

                <div style='text-align:center;margin:32px 0;'>
                    <a href='/'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 44px;border-radius:12px;font-size:17px;font-weight:700;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 4px 15px rgba(26,107,90,0.35);'>
                        🚀 ابدأ الآن
                    </a>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;font-family:""Cairo"",sans-serif;'>
                    إذا واجهتك أي مشكلة، تواصل معنا على <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>support@etmen.com</a>
                </p>
            ", "مرحباً بك");
        }

        // ────────────────────────────────────────────────────────────────
        // 3. LAB RESULT NOTIFICATION
        // ────────────────────────────────────────────────────────────────
        public static string LabResult(string name, string testName, DateTime testDate)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,#0ea5e9,#38bdf8);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>🔬</span>
                    </div>
                    <h1 style='color:#0369a1;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>نتيجة تحليلك جاهزة</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    نتيجة تحليلك <strong>«{testName}»</strong> بتاريخ <strong>{testDate:dd/MM/yyyy}</strong>
                    أصبحت جاهزة. يمكنك الاطلاع على التقرير الكامل في المرفق.
                </p>

                <div style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:16px;padding:20px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>اسم التحليل</td>
                            <td style='color:{ColorText};font-weight:700;font-size:14px;text-align:left;padding:10px 0;font-family:""Cairo"",sans-serif;'>{testName}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;border-top:1px solid #dbeafe;font-family:""Cairo"",sans-serif;'>تاريخ التحليل</td>
                            <td style='color:{ColorText};font-weight:700;font-size:14px;text-align:left;padding:10px 0;border-top:1px solid #dbeafe;font-family:""Cairo"",sans-serif;'>{testDate:dd MMMM yyyy}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;border-top:1px solid #dbeafe;font-family:""Cairo"",sans-serif;'>التقرير</td>
                            <td style='color:#0369a1;font-weight:700;font-size:14px;text-align:left;padding:10px 0;border-top:1px solid #dbeafe;font-family:""Cairo"",sans-serif;'>📎 مرفق بهذا البريد (PDF)</td>
                        </tr>
                    </table>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fffbeb;border-right:4px solid {ColorWarning};border-radius:12px;margin-top:16px;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:#92400e;font-size:14px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                ⚕️ <strong>تنبيه طبي:</strong> هذا التقرير لأغراض المعلومات فقط.
                                يُرجى مراجعة طبيبك لتفسير النتائج وتقديم العلاج المناسب.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "نتيجة التحليل");
        }

        // ────────────────────────────────────────────────────────────────
        // 4. APPOINTMENT CONFIRMATION
        // ────────────────────────────────────────────────────────────────
        public static string AppointmentConfirmation(
            string toName, string doctorName, string patientName,
            DateTime date, TimeSpan start, TimeSpan end, string? notes, bool isDoctor)
        {
            var otherParty = isDoctor ? $"المريض: {patientName}" : $"الطبيب: د. {doctorName}";
            var greeting   = isDoctor ? $"تم حجز موعد جديد مع مريضك {patientName}" : $"تم تأكيد موعدك مع الدكتور {doctorName}";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>📅</span>
                    </div>
                    <h1 style='color:{ColorPrimary};font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تأكيد الموعد</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>{greeting}</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{toName}</strong>،</p>

                <div style='background:{ColorBg};border:2px solid {ColorAccent};border-radius:16px;padding:24px;margin:24px 0;'>
                    <h3 style='color:{ColorPrimary};margin:0 0 16px;font-size:17px;font-family:""Cairo"",sans-serif;'>📋 تفاصيل الموعد</h3>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>⏰ الوقت</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{start:hh\\:mm} – {end:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>👤 {(isDoctor ? "المريض" : "الطبيب")}</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{otherParty}</td>
                        </tr>
                        {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>📝 ملاحظات</td>
                            <td style='color:{ColorText};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{notes}</td>
                        </tr>")}
                    </table>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fffbeb;border-right:4px solid {ColorWarning};border-radius:12px;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:#92400e;font-size:14px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                ⏰ ستصلك تذكيرات تلقائية قبل الموعد بـ <strong>يوم كامل</strong> وقبله بـ <strong>ساعتين</strong>.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تأكيد الموعد");
        }

        // ────────────────────────────────────────────────────────────────
        // 5. APPOINTMENT REMINDER
        // ────────────────────────────────────────────────────────────────
        public static string AppointmentReminder(
            string toName, string doctorName, string patientName,
            DateTime date, TimeSpan start, string timeLabel)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorWarning},#fbbf24);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>⏰</span>
                    </div>
                    <h1 style='color:#92400e;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تذكير بموعدك</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>موعدك بعد <strong>{timeLabel}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    مرحباً <strong>{toName}</strong>، هذا تذكير بموعدك القادم:
                </p>

                <div style='background:#fffbeb;border:2px solid {ColorWarning};border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fde68a;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>⏰ الوقت</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{start:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fde68a;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>🩺 الطبيب</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>د. {doctorName}</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;font-family:""Cairo"",sans-serif;'>
                    يُرجى الحضور قبل موعدك بـ 10 دقائق على الأقل. 🙏
                </p>
            ", "تذكير بالموعد");
        }

        // ────────────────────────────────────────────────────────────────
        // 6. APPOINTMENT CANCELLATION
        // ────────────────────────────────────────────────────────────────
        public static string AppointmentCancellation(
            string toName, string doctorName, string patientName,
            DateTime date, TimeSpan start, bool isDoctor)
        {
            var otherParty = isDoctor ? patientName : $"د. {doctorName}";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>❌</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تم إلغاء الموعد</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    مرحباً <strong>{toName}</strong>، نأسف لإبلاغك بأنه تم إلغاء موعدك مع <strong>{otherParty}</strong>.
                </p>

                <div style='background:#fef2f2;border:1px solid #fecaca;border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ المُلغى</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;text-decoration:line-through;padding:10px 0;font-family:""Cairo"",sans-serif;'>{date:dd/MM/yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>⏰ الوقت</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;text-decoration:line-through;padding:10px 0;font-family:""Cairo"",sans-serif;'>{start:hh\\:mm}</td>
                        </tr>
                    </table>
                </div>

                <div style='text-align:center;margin:28px 0;'>
                    <a href='/NearbyProviders'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:14px 36px;border-radius:12px;font-size:16px;font-weight:700;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 4px 12px rgba(26,107,90,0.25);'>
                        📅 احجز موعداً جديداً
                    </a>
                </div>
            ", "إلغاء الموعد");
        }

        // ────────────────────────────────────────────────────────────────
        // 7. RISK ALERT
        // ────────────────────────────────────────────────────────────────
        public static string RiskAlert(
            string toName, string patientName, string riskLevel,
            decimal riskScore, List<string> recommendations, bool isFamilyMember)
        {
            var riskPercent = (int)(riskScore * 100);
            var (riskColor, riskBg, riskEmoji) = riskLevel switch
            {
                "Emergency" or "طارئ"         => (ColorDanger, "#fef2f2", "🚨"),
                "High"      or "عالي"          => (ColorWarning, "#fffbeb", "⚠️"),
                _                              => ("#3b82f6", "#eff6ff", "📊"),
            };

            var recs = recommendations.Count > 0
                ? string.Join("", recommendations.Select(r =>
                    $"<li style='color:{ColorText};font-size:14px;padding:6px 0;font-family:\"Cairo\",sans-serif;'>{r}</li>"))
                : $"<li style='color:{ColorMuted};font-size:14px;font-family:\"Cairo\",sans-serif;'>لا توجد توصيات محددة</li>";

            var contextMsg = isFamilyMember
                ? $"<strong>تنبيه:</strong> مستوى الخطر الصحي لأحد أفراد عائلتك <strong>{patientName}</strong> قد ارتفع."
                : "مستوى الخطر الصحي لديك قد ارتفع. يُرجى اتخاذ الإجراءات اللازمة.";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='font-size:56px;margin-bottom:12px;'>{riskEmoji}</div>
                    <h1 style='color:{riskColor};font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تنبيه: ارتفاع مستوى الخطر</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>
                        {(isFamilyMember ? $"المريض: {patientName}" : "حالتك الصحية")}
                    </p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    مرحباً <strong>{toName}</strong>،<br>{contextMsg}
                </p>

                <div style='background:{riskBg};border:2px solid {riskColor};border-radius:16px;padding:24px;margin:24px 0;text-align:center;'>
                    <div style='font-size:48px;font-weight:900;color:{riskColor};font-family:""Cairo"",sans-serif;'>{riskPercent}%</div>
                    <div style='font-size:20px;font-weight:700;color:{riskColor};margin-top:4px;font-family:""Cairo"",sans-serif;'>مستوى الخطر: {riskLevel}</div>
                    <div style='background:#e5e7eb;border-radius:50px;height:10px;margin:16px auto;max-width:280px;overflow:hidden;'>
                        <div style='background:{riskColor};border-radius:50px;height:10px;width:{riskPercent}%;'></div>
                    </div>
                </div>

                <div style='background:{ColorBg};border-radius:16px;padding:20px;margin-top:16px;border:1px solid rgba(26,107,90,0.1);'>
                    <h3 style='color:{ColorPrimary};font-size:16px;margin:0 0 12px;font-family:""Cairo"",sans-serif;'>💡 التوصيات الطبية</h3>
                    <ul style='margin:0;padding-right:20px;'>
                        {recs}
                    </ul>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fef2f2;border-right:4px solid {ColorDanger};border-radius:12px;margin-top:16px;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:#991b1b;font-size:14px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                🚑 في حالة الطوارئ، اتصل فوراً بالإسعاف أو توجه لأقرب مستشفى.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تنبيه صحي");
        }

        // ────────────────────────────────────────────────────────────────
        // 8. PASSWORD RESET
        // ────────────────────────────────────────────────────────────────
        public static string PasswordReset(string name, string resetLink)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,#7c3aed,#a78bfa);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>🔒</span>
                    </div>
                    <h1 style='color:#5b21b6;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>إعادة تعيين كلمة المرور</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    تلقينا طلباً لإعادة تعيين كلمة مرور حسابك في منصة اطمئن.
                    اضغط على الزر أدناه لاختيار كلمة مرور جديدة:
                </p>

                <div style='text-align:center;margin:36px 0;'>
                    <a href='{resetLink}'
                       style='background:linear-gradient(135deg,#7c3aed,#a78bfa);color:#fff;text-decoration:none;
                              padding:16px 40px;border-radius:12px;font-size:17px;font-weight:700;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 4px 15px rgba(124,58,237,0.35);'>
                        🔑 إعادة تعيين كلمة المرور
                    </a>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f3ff;border-right:4px solid #7c3aed;border-radius:12px;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:#6d28d9;font-size:13px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                ⏱️ هذا الرابط صالح لمدة <strong>ساعتين</strong> فقط.<br>
                                إذا لم تطلب ذلك، تجاهل هذا البريد — حسابك بأمان تام.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "إعادة تعيين كلمة المرور");
        }

        // ────────────────────────────────────────────────────────────────
        // 9. EMERGENCY CONFIRMATION
        // ────────────────────────────────────────────────────────────────
        public static string EmergencyConfirmation(string name, string emergencyType, DateTime requestTime)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 16px;'>
                        <span style='font-size:32px;'>🚨</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تم استلام طلب الطوارئ</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    تم استلام طلب طوارئك بنجاح وجاري التعامل معه فوراً.
                </p>

                <div style='background:#fef2f2;border:2px solid {ColorDanger};border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>🆘 نوع الطوارئ</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{emergencyType}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>🕐 وقت الطلب</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>{requestTime:dd/MM/yyyy — HH:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;font-family:""Cairo"",sans-serif;'>📌 الحالة</td>
                            <td style='color:#dc2626;font-weight:700;font-size:15px;padding:10px 0;font-family:""Cairo"",sans-serif;'>⚡ قيد المعالجة</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                    في انتظار المساعدة، ابقَ هادئاً ومكانك. سيتواصل معك فريقنا في أقرب وقت. 🙏
                </p>
            ", "تأكيد طلب الطوارئ");
        }

        // ────────────────────────────────────────────────────────────────
        // 10. DOCTOR APPROVAL/REJECTION
        // ────────────────────────────────────────────────────────────────
        public static string DoctorApproval(string name, bool isApproved, string? reason)
        {
            var (emoji, color, title, msg) = isApproved
                ? ("✅", ColorPrimary, "تم قبول طلبك كطبيب!",
                   "يسعدنا إبلاغك بأن طلب تسجيلك كطبيب في منصة اطمئن قد تم قبوله. يمكنك الآن تسجيل الدخول وبدء الاستفادة من جميع ميزات المنصة.")
                : ("❌", ColorDanger, "تم رفض طلب التسجيل",
                   "نأسف لإبلاغك بأن طلب تسجيلك كطبيب في منصة اطمئن لم يتم قبوله في الوقت الحالي.");

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='font-size:56px;margin-bottom:12px;'>{emoji}</div>
                    <h1 style='color:{color};font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>{title}</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>مرحباً دكتور <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;font-family:""Cairo"",sans-serif;'>{msg}</p>

                {(!isApproved && !string.IsNullOrWhiteSpace(reason) ? $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fef2f2;border-right:4px solid {ColorDanger};border-radius:12px;margin:20px 0;'>
                    <tr>
                        <td style='padding:16px;'>
                            <p style='color:#991b1b;font-size:14px;margin:0;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                                <strong>سبب الرفض:</strong> {reason}
                            </p>
                        </td>
                    </tr>
                </table>" : "")}

                {(isApproved ? $@"
                <div style='text-align:center;margin:32px 0;'>
                    <a href='/Account/Login'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 44px;border-radius:12px;font-size:17px;font-weight:700;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 4px 15px rgba(26,107,90,0.35);'>
                        🚀 تسجيل الدخول الآن
                    </a>
                </div>" : $@"
                <p style='color:{ColorMuted};font-size:14px;text-align:center;font-family:""Cairo"",sans-serif;'>
                    للاستفسار عن هذا القرار، تواصل معنا على <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>support@etmen.com</a>
                </p>")}
            ", isApproved ? "قبول التسجيل" : "نتيجة طلب التسجيل");
        }

        // ────────────────────────────────────────────────────────────────
        // Shared wrapper — responsive Arabic HTML shell
        // ────────────────────────────────────────────────────────────────
        private static string Wrap(string content, string previewText)
        {
            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{previewText} — منصة اطمئن</title>
    <link rel='preconnect' href='https://fonts.googleapis.com'>
    <link rel='preconnect' href='https://fonts.gstatic.com' crossorigin>
    <link href='https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700;800;900&display=swap' rel='stylesheet'>
    <!--[if mso]>
    <noscript>
    <xml>
      <o:OfficeDocumentSettings>
        <o:PixelsPerInch>96</o:PixelsPerInch>
      </o:OfficeDocumentSettings>
    </xml>
    </noscript>
    <![endif]-->
    <style>
        body, table, td, a {{
            font-family: 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif !important;
        }}
    </style>
</head>
<body style='margin:0;padding:0;background-color:{ColorBg};font-family:""Cairo"", ""Segoe UI"", Arial, sans-serif;direction:rtl;'>

    <!-- Preview text (shown in email clients) -->
    <div style='display:none;max-height:0;overflow:hidden;color:{ColorBg};'>{previewText} — منصة اطمئن الطبية</div>

    <table width='100%' cellpadding='0' cellspacing='0' style='background:{ColorBg};min-height:100vh;'>
      <tr>
        <td align='center' style='padding:40px 16px;'>

          <!-- Card Wrapper with soft shadow and borders -->
          <table width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%;background:#ffffff;border-radius:24px;border:1px solid rgba(229,231,235,0.6);box-shadow:0 15px 30px rgba(0,0,0,0.04);overflow:hidden;'>

            <!-- Header -->
            <tr>
              <td style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorAccent} 100%);padding:36px 40px;text-align:center;'>
                <span style='color:#fff;font-size:28px;font-weight:900;letter-spacing:1px;font-family:""Cairo"",sans-serif;'>🏥 منصة اطمئن</span>
                <p style='color:rgba(255,255,255,0.9);font-size:13px;margin:6px 0 0;font-family:""Cairo"",sans-serif;font-weight:600;'>نظام الصحة الرقمية المتكامل لإدارة الأزمات</p>
              </td>
            </tr>

            <!-- Body -->
            <tr>
              <td style='background:{ColorCard};padding:40px 48px;'>
                {content}
              </td>
            </tr>

            <!-- Footer -->
            <tr>
              <td style='background:linear-gradient(135deg,#f8fafc,#f0fdf4);padding:28px 40px;text-align:center;border-top:1px solid #e5e7eb;'>
                <p style='color:{ColorMuted};font-size:13px;margin:0 0 8px;line-height:1.5;font-family:""Cairo"",sans-serif;'>
                    هذا البريد أُرسل تلقائياً من <strong style=""color:{ColorPrimary}"">منصة اطمئن</strong>.<br>
                    يُرجى عدم الرد على هذا البريد مباشرةً.
                </p>
                <p style='color:{ColorMuted};font-size:12px;margin:0;font-family:""Cairo"",sans-serif;'>
                    للدعم والمساعدة: <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>support@etmen.com</a>
                    &nbsp;|&nbsp; © {DateTime.UtcNow.Year} منصة اطمئن — جميع الحقوق محفوظة
                </p>
              </td>
            </tr>

          </table>
        </td>
      </tr>
    </table>
</body>
</html>";
        }
    }
}
