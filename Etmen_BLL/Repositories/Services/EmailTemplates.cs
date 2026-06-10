namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Generates beautiful, responsive Arabic HTML email templates for all Etmen platform events.
    /// All templates use inline CSS for maximum email-client compatibility.
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
        private const string ColorMuted    = "#6b7280";

        // ────────────────────────────────────────────────────────────────
        // 1. ACCOUNT ACTIVATION
        // ────────────────────────────────────────────────────────────────
        public static string AccountActivation(string name, string activationLink, string role)
        {
            var roleAr = role == "Doctor" ? "طبيب" : "مريض";
            return Wrap($@"
                <div style='text-align:center; margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>✉️</span>
                    </div>
                    <h1 style='color:{ColorPrimary};font-size:26px;margin:0 0 8px;'>تفعيل حسابك</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;'>منصة اطمئن الطبية</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    شكراً لتسجيلك في <strong>منصة اطمئن</strong> كـ<strong>{roleAr}</strong>.
                    خطوة واحدة فقط تفصلك عن الوصول لحسابك — اضغط على الزر أدناه لتفعيل بريدك الإلكتروني.
                </p>

                <div style='text-align:center;margin:36px 0;'>
                    <a href='{activationLink}'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 40px;border-radius:50px;font-size:17px;font-weight:700;
                              display:inline-block;box-shadow:0 4px 15px rgba(26,107,90,0.35);'>
                        ✅ تفعيل الحساب الآن
                    </a>
                </div>

                <div style='background:#f8fafc;border-radius:12px;padding:16px;margin-top:24px;'>
                    <p style='color:{ColorMuted};font-size:13px;margin:0;text-align:center;'>
                        ⏱️ هذا الرابط صالح لمدة <strong>24 ساعة</strong> فقط.<br>
                        إذا لم تقم بالتسجيل، يمكنك تجاهل هذا البريد بأمان.
                    </p>
                </div>
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

            var featureCards = string.Join("", features.Select(f => $@"
                <div style='background:{ColorBg};border-radius:12px;padding:16px 20px;margin-bottom:12px;display:flex;align-items:flex-start;gap:12px;'>
                    <span style='font-size:24px;'>{f.Item1}</span>
                    <div>
                        <div style='color:{ColorPrimary};font-weight:700;font-size:15px;'>{f.Item2}</div>
                        <div style='color:{ColorMuted};font-size:13px;margin-top:2px;'>{f.Item3}</div>
                    </div>
                </div>
            "));

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='font-size:56px;margin-bottom:12px;'>🎉</div>
                    <h1 style='color:{ColorPrimary};font-size:28px;margin:0 0 8px;'>أهلاً بك يا {name}!</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;'>انضم الآن إلى مجتمع اطمئن الطبي كـ<strong>{roleAr}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    تم تفعيل حسابك بنجاح 🎊 — إليك ما يمكنك فعله الآن:
                </p>

                {featureCards}

                <div style='text-align:center;margin:32px 0;'>
                    <a href='/'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 44px;border-radius:50px;font-size:17px;font-weight:700;
                              display:inline-block;box-shadow:0 4px 15px rgba(26,107,90,0.35);'>
                        🚀 ابدأ الآن
                    </a>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;'>
                    إذا واجهتك أي مشكلة، تواصل معنا على <a href='mailto:support@etmen.com' style='color:{ColorPrimary};'>support@etmen.com</a>
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
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,#0ea5e9,#38bdf8);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>🔬</span>
                    </div>
                    <h1 style='color:#0369a1;font-size:26px;margin:0 0 8px;'>نتيجة تحليلك جاهزة</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    نتيجة تحليلك <strong>«{testName}»</strong> بتاريخ <strong>{testDate:dd/MM/yyyy}</strong>
                    أصبحت جاهزة. يمكنك الاطلاع على التقرير الكامل في المرفق.
                </p>

                <div style='background:#eff6ff;border:1px solid #bfdbfe;border-radius:12px;padding:20px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:8px 0;'>اسم التحليل</td>
                            <td style='color:{ColorText};font-weight:700;font-size:14px;text-align:left;'>{testName}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:8px 0;border-top:1px solid #dbeafe;'>تاريخ التحليل</td>
                            <td style='color:{ColorText};font-weight:700;font-size:14px;text-align:left;border-top:1px solid #dbeafe;'>{testDate:dd MMMM yyyy}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:8px 0;border-top:1px solid #dbeafe;'>التقرير</td>
                            <td style='color:#0369a1;font-weight:700;font-size:14px;text-align:left;border-top:1px solid #dbeafe;'>📎 مرفق بهذا البريد (PDF)</td>
                        </tr>
                    </table>
                </div>

                <div style='background:#fefce8;border:1px solid #fde68a;border-radius:12px;padding:16px;margin-top:16px;'>
                    <p style='color:#92400e;font-size:14px;margin:0;'>
                        ⚕️ <strong>تنبيه طبي:</strong> هذا التقرير لأغراض المعلومات فقط.
                        يُرجى مراجعة طبيبك لتفسير النتائج وتقديم العلاج المناسب.
                    </p>
                </div>
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
                    <div style='font-size:56px;margin-bottom:12px;'>📅</div>
                    <h1 style='color:{ColorPrimary};font-size:26px;margin:0 0 8px;'>تأكيد الموعد</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;'>{greeting}</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً <strong>{toName}</strong>،</p>

                <div style='background:{ColorBg};border:2px solid {ColorAccent};border-radius:16px;padding:24px;margin:24px 0;'>
                    <h3 style='color:{ColorPrimary};margin:0 0 16px;font-size:17px;'>📋 تفاصيل الموعد</h3>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>⏰ الوقت</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{start:hh\\:mm} – {end:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>👤 {(isDoctor ? "المريض" : "الطبيب")}</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{otherParty}</td>
                        </tr>
                        {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
                        <tr style='border-top:1px solid #d1fae5;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>📝 ملاحظات</td>
                            <td style='color:{ColorText};font-size:14px;'>{notes}</td>
                        </tr>")}
                    </table>
                </div>

                <div style='background:#fffbeb;border:1px solid #fde68a;border-radius:12px;padding:16px;'>
                    <p style='color:#92400e;font-size:14px;margin:0;'>
                        ⏰ ستصلك تذكيرات تلقائية قبل الموعد بـ <strong>يوم كامل</strong> وقبله بـ <strong>ساعتين</strong>.
                    </p>
                </div>
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
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorWarning},#fbbf24);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>⏰</span>
                    </div>
                    <h1 style='color:#92400e;font-size:26px;margin:0 0 8px;'>تذكير بموعدك</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;'>موعدك بعد <strong>{timeLabel}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    مرحباً <strong>{toName}</strong>، هذا تذكير بموعدك القادم:
                </p>

                <div style='background:#fffbeb;border:2px solid {ColorWarning};border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fde68a;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>⏰ الوقت</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{start:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fde68a;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>🩺 الطبيب</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>د. {doctorName}</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;'>
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
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>❌</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;margin:0 0 8px;'>تم إلغاء الموعد</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    مرحباً <strong>{toName}</strong>، نأسف لإبلاغك بأنه تم إلغاء موعدك مع <strong>{otherParty}</strong>.
                </p>

                <div style='background:#fef2f2;border:1px solid #fecaca;border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>📅 التاريخ المُلغى</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;text-decoration:line-through;'>{date:dd/MM/yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>⏰ الوقت</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;text-decoration:line-through;'>{start:hh\\:mm}</td>
                        </tr>
                    </table>
                </div>

                <div style='text-align:center;margin:28px 0;'>
                    <a href='/NearbyProviders'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:14px 36px;border-radius:50px;font-size:16px;font-weight:700;display:inline-block;'>
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
                    $"<li style='color:{ColorText};font-size:14px;padding:6px 0;'>{r}</li>"))
                : "<li style='color:{ColorMuted};font-size:14px;'>لا توجد توصيات محددة</li>";

            var contextMsg = isFamilyMember
                ? $"<strong>تنبيه:</strong> مستوى الخطر الصحي لأحد أفراد عائلتك <strong>{patientName}</strong> قد ارتفع."
                : "مستوى الخطر الصحي لديك قد ارتفع. يُرجى اتخاذ الإجراءات اللازمة.";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='font-size:56px;margin-bottom:12px;'>{riskEmoji}</div>
                    <h1 style='color:{riskColor};font-size:26px;margin:0 0 8px;'>تنبيه: ارتفاع مستوى الخطر</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;'>
                        {(isFamilyMember ? $"المريض: {patientName}" : "حالتك الصحية")}
                    </p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    مرحباً <strong>{toName}</strong>،<br>{contextMsg}
                </p>

                <div style='background:{riskBg};border:2px solid {riskColor};border-radius:16px;padding:24px;margin:24px 0;text-align:center;'>
                    <div style='font-size:48px;font-weight:900;color:{riskColor};'>{riskPercent}%</div>
                    <div style='font-size:20px;font-weight:700;color:{riskColor};margin-top:4px;'>مستوى الخطر: {riskLevel}</div>
                    <div style='background:#e5e7eb;border-radius:50px;height:10px;margin:16px auto;max-width:280px;'>
                        <div style='background:{riskColor};border-radius:50px;height:10px;width:{riskPercent}%;'></div>
                    </div>
                </div>

                <div style='background:{ColorBg};border-radius:12px;padding:20px;margin-top:16px;'>
                    <h3 style='color:{ColorPrimary};font-size:16px;margin:0 0 12px;'>💡 التوصيات الطبية</h3>
                    <ul style='margin:0;padding-right:20px;'>
                        {recs}
                    </ul>
                </div>

                <div style='background:#fef2f2;border:1px solid #fecaca;border-radius:12px;padding:16px;margin-top:16px;'>
                    <p style='color:#991b1b;font-size:14px;margin:0;'>
                        🚑 في حالة الطوارئ، اتصل فوراً بالإسعاف أو توجه لأقرب مستشفى.
                    </p>
                </div>
            ", "تنبيه صحي");
        }

        // ────────────────────────────────────────────────────────────────
        // 8. PASSWORD RESET
        // ────────────────────────────────────────────────────────────────
        public static string PasswordReset(string name, string resetLink)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,#7c3aed,#a78bfa);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>🔒</span>
                    </div>
                    <h1 style='color:#5b21b6;font-size:26px;margin:0 0 8px;'>إعادة تعيين كلمة المرور</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    تلقينا طلباً لإعادة تعيين كلمة مرور حسابك في منصة اطمئن.
                    اضغط على الزر أدناه لاختيار كلمة مرور جديدة:
                </p>

                <div style='text-align:center;margin:36px 0;'>
                    <a href='{resetLink}'
                       style='background:linear-gradient(135deg,#7c3aed,#a78bfa);color:#fff;text-decoration:none;
                              padding:16px 40px;border-radius:50px;font-size:17px;font-weight:700;
                              display:inline-block;box-shadow:0 4px 15px rgba(124,58,237,0.35);'>
                        🔑 إعادة تعيين كلمة المرور
                    </a>
                </div>

                <div style='background:#f5f3ff;border:1px solid #ddd6fe;border-radius:12px;padding:16px;'>
                    <p style='color:#6d28d9;font-size:13px;margin:0;text-align:center;'>
                        ⏱️ هذا الرابط صالح لمدة <strong>2 ساعة</strong> فقط.<br>
                        إذا لم تطلب ذلك، تجاهل هذا البريد — حسابك بأمان تام.
                    </p>
                </div>
            ", "إعادة تعيين كلمة المرور");
        }

        // ────────────────────────────────────────────────────────────────
        // 9. EMERGENCY CONFIRMATION
        // ────────────────────────────────────────────────────────────────
        public static string EmergencyConfirmation(string name, string emergencyType, DateTime requestTime)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:32px;'>
                    <div style='width:72px;height:72px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;'>
                        <span style='font-size:32px;'>🚨</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;margin:0 0 8px;'>تم استلام طلب الطوارئ</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>
                    تم استلام طلب طوارئك بنجاح وجاري التعامل معه فوراً.
                </p>

                <div style='background:#fef2f2;border:2px solid {ColorDanger};border-radius:16px;padding:24px;margin:24px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>🆘 نوع الطوارئ</td>
                            <td style='color:#991b1b;font-weight:700;font-size:15px;'>{emergencyType}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>🕐 وقت الطلب</td>
                            <td style='color:{ColorText};font-weight:700;font-size:15px;'>{requestTime:dd/MM/yyyy — HH:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fecaca;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:10px 0;'>📌 الحالة</td>
                            <td style='color:#dc2626;font-weight:700;font-size:15px;'>⚡ قيد المعالجة</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;'>
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
                    <h1 style='color:{color};font-size:26px;margin:0 0 8px;'>{title}</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>مرحباً دكتور <strong>{name}</strong>،</p>
                <p style='color:{ColorText};font-size:16px;line-height:1.7;'>{msg}</p>

                {(!isApproved && !string.IsNullOrWhiteSpace(reason) ? $@"
                <div style='background:#fef2f2;border:1px solid #fecaca;border-radius:12px;padding:16px;margin:20px 0;'>
                    <p style='color:#991b1b;font-size:14px;margin:0;'>
                        <strong>سبب الرفض:</strong> {reason}
                    </p>
                </div>" : "")}

                {(isApproved ? $@"
                <div style='text-align:center;margin:32px 0;'>
                    <a href='/Account/Login'
                       style='background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});color:#fff;text-decoration:none;
                              padding:16px 44px;border-radius:50px;font-size:17px;font-weight:700;display:inline-block;'>
                        🚀 تسجيل الدخول الآن
                    </a>
                </div>" : @"
                <p style='color:{ColorMuted};font-size:14px;text-align:center;'>
                    للاستفسار عن هذا القرار، تواصل معنا على <a href='mailto:support@etmen.com' style='color:{ColorPrimary};'>support@etmen.com</a>
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
    <!--[if mso]><noscript><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml></noscript><![endif]-->
</head>
<body style='margin:0;padding:0;background-color:{ColorBg};font-family:""Segoe UI"",Arial,sans-serif;direction:rtl;'>

    <!-- Preview text (shown in email clients) -->
    <div style='display:none;max-height:0;overflow:hidden;color:{ColorBg};'>{previewText} — منصة اطمئن الطبية</div>

    <table width='100%' cellpadding='0' cellspacing='0' style='background:{ColorBg};min-height:100vh;'>
      <tr>
        <td align='center' style='padding:40px 16px;'>

          <!-- Card -->
          <table width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%;'>

            <!-- Header -->
            <tr>
              <td style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorAccent} 100%);border-radius:20px 20px 0 0;padding:28px 36px;text-align:center;'>
                <span style='color:#fff;font-size:26px;font-weight:900;letter-spacing:1px;'>🏥 منصة اطمئن</span>
                <p style='color:rgba(255,255,255,0.85);font-size:13px;margin:6px 0 0;'>نظام الصحة الرقمية المتكامل</p>
              </td>
            </tr>

            <!-- Body -->
            <tr>
              <td style='background:{ColorCard};padding:36px 40px;border-radius:0;'>
                {content}
              </td>
            </tr>

            <!-- Footer -->
            <tr>
              <td style='background:linear-gradient(135deg,#f8fafc,#f0fdf4);border-radius:0 0 20px 20px;padding:24px 36px;text-align:center;border-top:1px solid #e5e7eb;'>
                <p style='color:{ColorMuted};font-size:13px;margin:0 0 8px;'>
                    هذا البريد أُرسل تلقائياً من <strong style=""color:{ColorPrimary}"">منصة اطمئن</strong>.
                    يُرجى عدم الرد على هذا البريد مباشرةً.
                </p>
                <p style='color:{ColorMuted};font-size:12px;margin:0;'>
                    للدعم: <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;'>support@etmen.com</a>
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
