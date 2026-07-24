using System;
using System.Collections.Generic;
using System.Linq;

namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Generates ultra-luxurious, responsive Arabic HTML email templates for all Etmen platform events.
    /// Designed with high-end modern typography, elegant gradients, table-based Outlook compatibility,
    /// and premium visual styling.
    /// </summary>
    internal static class EmailTemplates
    {
        // ── Shared brand color palette ──────────────────────────────────
        private const string ColorPrimary   = "#0f766e"; // Deep Teal Green
        private const string ColorSecondary = "#0d9488"; // Radiant Teal
        private const string ColorAccent    = "#14b8a6"; // Vibrant Light Teal
        private const string ColorGold      = "#d97706"; // Luxurious Amber Gold
        private const string ColorWarning   = "#ea580c"; // Warm Orange
        private const string ColorDanger    = "#dc2626"; // Crimson Red
        private const string ColorInfo      = "#0284c7"; // Sky Blue
        private const string ColorBg        = "#f8fafc"; // Soft Slate BG
        private const string ColorCard      = "#ffffff";
        private const string ColorText      = "#0f172a"; // Dark Slate
        private const string ColorMuted     = "#475569"; // Slate Muted
        private const string ColorBorder    = "#e2e8f0";

        // ────────────────────────────────────────────────────────────────
        // 1. ACCOUNT ACTIVATION
        // ────────────────────────────────────────────────────────────────
        public static string AccountActivation(string name, string activationLink, string role)
        {
            var roleAr = role == "Doctor" ? "طبيب معتمد" : "مريض";
            return Wrap($@"
                <div style='text-align:center; margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(15,118,110,0.25);'>
                        <span style='font-size:38px;'>✉️</span>
                    </div>
                    <h1 style='color:{ColorText};font-size:26px;font-weight:800;margin:0 0 10px;font-family:""Cairo"",sans-serif;letter-spacing:-0.5px;'>تفعيل حسابك في المنصة</h1>
                    <div style='display:inline-block;background:linear-gradient(135deg,#f0fdf4,#ccfbf1);color:{ColorPrimary};font-size:14px;font-weight:700;padding:6px 18px;border-radius:50px;border:1px solid #99f6e4;font-family:""Cairo"",sans-serif;'>
                        نوع الحساب: {roleAr} ✦
                    </div>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;margin-bottom:16px;'>مرحباً بك <strong>{name}</strong>،</p>
                <p style='color:{ColorMuted};font-size:15px;line-height:1.8;font-family:""Cairo"",sans-serif;margin-bottom:28px;'>
                    يسعدنا انضمامك إلى <strong>منصة إطمن</strong>. تفصلك خطوة واحدة فقط لبدء استخدام المنصة والتأكد من أمان بياناتك الصحية. يُرجى تفعيل حسابك عن طريق الضغط على الزر الذهبي أدناه:
                </p>

                <div style='text-align:center;margin:38px 0;'>
                    <a href='{activationLink}'
                       style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorSecondary} 100%);color:#ffffff;text-decoration:none;
                              padding:18px 46px;border-radius:14px;font-size:17px;font-weight:800;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 10px 25px -5px rgba(15,118,110,0.4);letter-spacing:0.3px;'>
                        ✅ تفعيل الحساب الآن
                    </a>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fffbeb;border-right:4px solid {ColorGold};border-radius:14px;margin-top:28px;'>
                    <tr>
                        <td style='padding:18px 20px;'>
                            <p style='color:#b45309;font-size:13.5px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                ⏱️ <strong>ملاحظة أمان:</strong> هذا الرابط المخصص للتفعيل صالح لمدة <strong>24 ساعة</strong> فقط.<br>
                                إذا لم تقم بتسجيل هذا الحساب، يمكنك تجاهل هذه الرسالة بأمان تام.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تفعيل حسابك في منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 2. WELCOME EMAIL
        // ────────────────────────────────────────────────────────────────
        public static string Welcome(string name, string role)
        {
            var isDoctor = role == "Doctor";
            var roleAr = isDoctor ? "الطبيب المعالج" : "المريض المكرم";
            var features = isDoctor
                ? new[]
                {
                    ("📅", "إدارة جدول المواعيد",    "متابعة المواعيد القادمة وحجوزات المرضى بدقة وسلاسة"),
                    ("👥", "الملف الطبي الرقمي",     "استعراض التاريخ الطبي الكامل والتحاليل الفورية للمريض"),
                    ("⚠️", "نظام الإنذار المبكر",    "تلقي تنبيهات ذكية فورية عند ارتفاع أي علامة حيوية مقلقة"),
                    ("💬", "استشارات آمنة",          "تواصل طبي محمي ومباشر مع مرضاك طوال اليوم"),
                }
                : new[]
                {
                    ("🏥", "حجز الكشوفات الفورية",  "تأكيد وحجز المواعيد مع نخبة من أفضل الأطباء والمراكز"),
                    ("🔬", "نتائج التحاليل الذكية",   "قراءة وتقييم التحاليل والأشعة عبر الذكاء الاصطناعي"),
                    ("📊", "مؤشر الرعاية التنبؤي",    "متابعة دقيقة ومستمرة للضغط والسكر وكافة العلامات الحيوية"),
                    ("👨‍👩‍👧", "متابعة صحة العائلة",     "ربط حسابات الوالدين والأبناء للاطمئنان عليهم أولاً بأول"),
                };

            var featureCards = string.Join("", features.Select(f => $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f8fafc;border-radius:14px;margin-bottom:14px;border:1px solid #e2e8f0;'>
                    <tr>
                        <td style='padding:18px;width:44px;vertical-align:middle;text-align:center;'>
                            <div style='width:42px;height:42px;background:#ffffff;border-radius:12px;display:inline-flex;align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(0,0,0,0.06);font-size:22px;'>
                                {f.Item1}
                            </div>
                        </td>
                        <td style='padding:16px 12px 16px 18px;vertical-align:middle;'>
                            <div style='color:{ColorPrimary};font-weight:800;font-size:15.5px;font-family:""Cairo"",sans-serif;'>{f.Item2}</div>
                            <div style='color:{ColorMuted};font-size:13.5px;margin-top:3px;font-family:""Cairo"",sans-serif;line-height:1.5;'>{f.Item3}</div>
                        </td>
                    </tr>
                </table>
            "));

            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='font-size:58px;margin-bottom:12px;'>🎉</div>
                    <h1 style='color:{ColorPrimary};font-size:28px;font-weight:900;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>أهلاً ومرحباً بك، {name}!</h1>
                    <p style='color:{ColorMuted};font-size:15px;margin:0;font-family:""Cairo"",sans-serif;'>يسرنا انضمامك إلى مجتمع إطمن بصفتك <strong>{roleAr}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;margin-bottom:20px;'>
                    تم تفعيل حسابك بنجاح 🎊 — أصبح بإمكانك الآن الاستفادة الكاملة من كافة خدمات ومميزات المنصة:
                </p>

                {featureCards}

                <div style='text-align:center;margin:36px 0 24px;'>
                    <a href='/'
                       style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorSecondary} 100%);color:#ffffff;text-decoration:none;
                              padding:18px 48px;border-radius:14px;font-size:17px;font-weight:800;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 10px 25px -5px rgba(15,118,110,0.4);'>
                        🚀 ابدأ رحلتك الصحية الآن
                    </a>
                </div>

                <p style='color:{ColorMuted};font-size:13.5px;text-align:center;font-family:""Cairo"",sans-serif;margin-top:20px;'>
                    إذا كان لديك أي استفسار، يرجى مراسلتنا دائماً عبر <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>support@etmen.com</a>
                </p>
            ", "مرحباً بك في منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 3. LAB RESULT NOTIFICATION
        // ────────────────────────────────────────────────────────────────
        public static string LabResult(string name, string testName, DateTime testDate)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,#0284c7,#38bdf8);border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(2,132,199,0.25);'>
                        <span style='font-size:38px;'>🔬</span>
                    </div>
                    <h1 style='color:#0369a1;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>نتيجة التحليل الطبي جاهزة</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>تم إصدار التقرير الرقمي المعتمد</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorMuted};font-size:15px;line-height:1.8;font-family:""Cairo"",sans-serif;'>
                    نود إعلامك أن نتيجة الفحص الطبي الخاص بك <strong>«{testName}»</strong> أجريت بتاريخ <strong>{testDate:dd/MM/yyyy}</strong> وقد أصبحت متوفرة الآن للاطلاع. تجد نسخة مفصلة بصيغة PDF مرفقة مع هذا البريد.
                </p>

                <div style='background:linear-gradient(135deg,#f0f9ff,#e0f2fe);border:1px solid #bae6fd;border-radius:18px;padding:24px;margin:28px 0;'>
                    <h3 style='color:#0369a1;margin:0 0 16px;font-size:16.5px;font-weight:800;font-family:""Cairo"",sans-serif;'>📊 ملخص بيانات الفحص</h3>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>اسم الفحص الطبي</td>
                            <td style='color:{ColorText};font-weight:800;font-size:14.5px;text-align:left;padding:12px 0;font-family:""Cairo"",sans-serif;'>{testName}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;border-top:1px solid #e0f2fe;font-family:""Cairo"",sans-serif;'>تاريخ الفحص</td>
                            <td style='color:{ColorText};font-weight:800;font-size:14.5px;text-align:left;padding:12px 0;border-top:1px solid #e0f2fe;font-family:""Cairo"",sans-serif;'>{testDate:dd MMMM yyyy}</td>
                        </tr>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;border-top:1px solid #e0f2fe;font-family:""Cairo"",sans-serif;'>التقرير الرقمي</td>
                            <td style='color:#0284c7;font-weight:800;font-size:14.5px;text-align:left;padding:12px 0;border-top:1px solid #e0f2fe;font-family:""Cairo"",sans-serif;'>📎 مرفق بالبريد (تقرير-{testName}.pdf)</td>
                        </tr>
                    </table>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fffbeb;border-right:4px solid {ColorGold};border-radius:14px;margin-top:20px;'>
                    <tr>
                        <td style='padding:18px;'>
                            <p style='color:#b45309;font-size:13.5px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                ⚕️ <strong>إرشاد طبي:</strong> هذه النتائج مخصصة للاستئناس الشخصي. يُنصح دائماً بمراجعة طبيبك المعالج لتفسير القيم بدقة وتوجيه الخطة العلاجية المناسبة.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "نتيجة تحليلك الطبي جاهزة — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 4. APPOINTMENT CONFIRMATION
        // ────────────────────────────────────────────────────────────────
        public static string AppointmentConfirmation(
            string toName, string doctorName, string patientName,
            DateTime date, TimeSpan start, TimeSpan end, string? notes, bool isDoctor)
        {
            var otherPartyTitle = isDoctor ? "المريض" : "الطبيب المتابع";
            var otherPartyVal   = isDoctor ? patientName : $"د. {doctorName}";
            var greeting       = isDoctor ? $"تم تأكيد موعد كشف جديد لدى مرضاك {patientName}" : $"تم تأكيد حجز موعدك بنجاح مع الدكتور {doctorName}";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,{ColorPrimary},{ColorAccent});border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(15,118,110,0.25);'>
                        <span style='font-size:38px;'>📅</span>
                    </div>
                    <h1 style='color:{ColorPrimary};font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تأكيد موعد الحجز</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>{greeting}</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{toName}</strong>،</p>

                <div style='background:linear-gradient(135deg,#f0fdf4,#ccfbf1);border:1.5px solid #99f6e4;border-radius:18px;padding:24px;margin:28px 0;'>
                    <h3 style='color:{ColorPrimary};margin:0 0 16px;font-size:16.5px;font-weight:800;font-family:""Cairo"",sans-serif;'>📋 تفاصيل وتوقيت الموعد</h3>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #a7f3d0;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>⏰ توقيت الجلسة</td>
                            <td style='color:{ColorPrimary};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>من {start:hh\\:mm} حتى {end:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #a7f3d0;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>👤 {otherPartyTitle}</td>
                            <td style='color:{ColorText};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{otherPartyVal}</td>
                        </tr>
                        {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
                        <tr style='border-top:1px solid #a7f3d0;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>📝 ملاحظات إضافية</td>
                            <td style='color:{ColorText};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{notes}</td>
                        </tr>")}
                    </table>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f0fdf4;border-right:4px solid {ColorSecondary};border-radius:14px;'>
                    <tr>
                        <td style='padding:18px;'>
                            <p style='color:#065f46;font-size:13.5px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                ⏰ ستصلك تذكيرات آلمية تلقائية قبل الموعد بـ <strong>24 ساعة</strong> وقبله بـ <strong>ساعتين</strong> لتسليم كافة التفاصيل.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تأكيد الموعد الطبي — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 5. APPOINTMENT REMINDER
        // ────────────────────────────────────────────────────────────────
        public static string AppointmentReminder(
            string toName, string doctorName, string patientName,
            DateTime date, TimeSpan start, string timeLabel)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,{ColorGold},#f59e0b);border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(217,119,6,0.25);'>
                        <span style='font-size:38px;'>⏰</span>
                    </div>
                    <h1 style='color:#b45309;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تذكير بموعد الكشف القادم</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>موعدك المتبقي عليه: <strong style='color:#b45309;'>{timeLabel}</strong></p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{toName}</strong>، نود تذكيرك بموعدك الطبي المقيم قريباً:</p>

                <div style='background:#fffbeb;border:1.5px solid #fde68a;border-radius:18px;padding:24px;margin:28px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ</td>
                            <td style='color:{ColorText};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{date:dddd، dd MMMM yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fef3c7;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>⏰ الموعد المحدد</td>
                            <td style='color:#b45309;font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{start:hh\\:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fef3c7;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>🩺 الطبيب المعالج</td>
                            <td style='color:{ColorText};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>د. {doctorName}</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;font-family:""Cairo"",sans-serif;line-height:1.6;'>
                    يرجى الحضور قبل الموعد بـ 15 دقيقة أو فتح منصة الاستشارة الافتراضية في الوقت المحدد. نتمنى لك دوام الصحة والعافية. 🌿
                </p>
            ", "تذكير بموعدك الطبي — منصة إطمن");
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
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(220,38,38,0.25);'>
                        <span style='font-size:38px;'>❌</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>إلغاء موعد الحجز</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>تم تحديث حالة الموعد إلى ملغى</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>
                    مرحباً <strong>{toName}</strong>، نود إعلامك بأنه قد تم إلغاء الموعد المبرمج سابقاً مع <strong>{otherParty}</strong>.
                </p>

                <div style='background:#fef2f2;border:1.5px solid #fecaca;border-radius:18px;padding:24px;margin:28px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>📅 التاريخ الملغى</td>
                            <td style='color:#991b1b;font-weight:800;font-size:15px;text-decoration:line-through;padding:12px 0;font-family:""Cairo"",sans-serif;'>{date:dd/MM/yyyy}</td>
                        </tr>
                        <tr style='border-top:1px solid #fee2e2;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>⏰ توقيت الموعد</td>
                            <td style='color:#991b1b;font-weight:800;font-size:15px;text-decoration:line-through;padding:12px 0;font-family:""Cairo"",sans-serif;'>{start:hh\\:mm}</td>
                        </tr>
                    </table>
                </div>

                <div style='text-align:center;margin:32px 0;'>
                    <a href='/NearbyProviders'
                       style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorSecondary} 100%);color:#ffffff;text-decoration:none;
                              padding:16px 40px;border-radius:14px;font-size:16px;font-weight:800;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 8px 20px rgba(15,118,110,0.3);'>
                        📅 حجز موعد جديد
                    </a>
                </div>
            ", "إلغاء الموعد — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 7. RISK ALERT
        // ────────────────────────────────────────────────────────────────
        public static string RiskAlert(
            string toName, string patientName, string riskLevel,
            decimal riskScore, List<string> recommendations, bool isFamilyMember)
        {
            var riskPercent = (int)(riskScore * 100);
            var (riskColor, riskBg, riskBorder, riskEmoji) = riskLevel switch
            {
                "Emergency" or "طارئ" => (ColorDanger, "#fef2f2", "#fca5a5", "🚨"),
                "High"      or "عالي"  => (ColorWarning, "#fffbeb", "#fde68a", "⚠️"),
                _                      => (ColorInfo,    "#f0f9ff", "#bae6fd", "📊"),
            };

            var recs = recommendations.Count > 0
                ? string.Join("", recommendations.Select(r =>
                    $"<li style='color:{ColorText};font-size:14.5px;padding:8px 0;font-family:\"Cairo\",sans-serif;line-height:1.6;'>{r}</li>"))
                : $"<li style='color:{ColorMuted};font-size:14px;font-family:\"Cairo\",sans-serif;'>اتبع النمط الصحي المتوازن وراجع الطبيب بانتظام</li>";

            var contextMsg = isFamilyMember
                ? $"<strong>تنبيه هائم:</strong> تم رصد ارتفاع في مؤشر الخطر الصحي الخاص بفرد عائلتك المرتبط <strong>({patientName})</strong>."
                : "تم رصد تغير طارئ وارتفاع في مؤشر الخطر الصحي الخاص بك بناءً على القراءات الأخيرة.";

            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='font-size:62px;margin-bottom:12px;'>{riskEmoji}</div>
                    <h1 style='color:{riskColor};font-size:27px;font-weight:900;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>تنبيه صحي: ارتفاع مستوى الخطر</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>
                        {(isFamilyMember ? $"الحالة المتعلقة بـ: {patientName}" : "نظام المتابعة الحيوية المستمرة")}
                    </p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>
                    مرحباً <strong>{toName}</strong>،<br>{contextMsg}
                </p>

                <div style='background:{riskBg};border:2px solid {riskBorder};border-radius:20px;padding:28px;margin:28px 0;text-align:center;'>
                    <div style='font-size:52px;font-weight:900;color:{riskColor};font-family:""Cairo"",sans-serif;line-height:1;'>{riskPercent}%</div>
                    <div style='font-size:19px;font-weight:800;color:{riskColor};margin-top:8px;font-family:""Cairo"",sans-serif;'>تصنيف الخطر: {riskLevel}</div>
                    <div style='background:#e2e8f0;border-radius:50px;height:12px;margin:18px auto 0;max-width:320px;overflow:hidden;'>
                        <div style='background:{riskColor};border-radius:50px;height:12px;width:{riskPercent}%;'></div>
                    </div>
                </div>

                <div style='background:#ffffff;border-radius:18px;padding:22px;margin-top:20px;border:1px solid #e2e8f0;box-shadow:0 4px 12px rgba(0,0,0,0.03);'>
                    <h3 style='color:{ColorPrimary};font-size:16.5px;margin:0 0 14px;font-weight:800;font-family:""Cairo"",sans-serif;'>💡 التوصيات الطبية العاجلة</h3>
                    <ul style='margin:0;padding-right:22px;'>
                        {recs}
                    </ul>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fef2f2;border-right:4px solid {ColorDanger};border-radius:14px;margin-top:20px;'>
                    <tr>
                        <td style='padding:18px;'>
                            <p style='color:#991b1b;font-size:14px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                🚑 <strong>خط الطوارئ العاجل:</strong> في حال وجود أعراض حادة أو ضيق تنفس، اتصل فوراً بفرق الإسعاف أو توجه لأقرب مركز طوارئ.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "تنبيه صحي عاجل — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 8. PASSWORD RESET
        // ────────────────────────────────────────────────────────────────
        public static string PasswordReset(string name, string resetLink)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,#7c3aed,#a78bfa);border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(124,58,237,0.25);'>
                        <span style='font-size:38px;'>🔒</span>
                    </div>
                    <h1 style='color:#5b21b6;font-size:26px;font-weight:800;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>طلب تعيين كلمة المرور</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>حماية واستعادة الحساب الرقمي</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorMuted};font-size:15px;line-height:1.8;font-family:""Cairo"",sans-serif;'>
                    تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك على منصة إطمن. يمكنك إنشاء كلمة مرور جديدة وآمنة من خلال النقر على الزر أدناه:
                </p>

                <div style='text-align:center;margin:38px 0;'>
                    <a href='{resetLink}'
                       style='background:linear-gradient(135deg,#7c3aed 0%,#6d28d9 100%);color:#ffffff;text-decoration:none;
                              padding:18px 46px;border-radius:14px;font-size:17px;font-weight:800;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 10px 25px -5px rgba(124,58,237,0.4);'>
                        🔑 تعيين كلمة المرور الجديدة
                    </a>
                </div>

                <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f3ff;border-right:4px solid #7c3aed;border-radius:14px;'>
                    <tr>
                        <td style='padding:18px;'>
                            <p style='color:#5b21b6;font-size:13.5px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                ⏱️ <strong>صلاحية الرابط:</strong> ينتهي هذا الرابط تلقائياً بعد <strong>ساعتين</strong>.<br>
                                إذا لم تقم بطلب إعادة التعيين بنفسك، يرجى تجاهل هذا البريد؛ حسابك ينعم بالحماية الكاملة.
                            </p>
                        </td>
                    </tr>
                </table>
            ", "إعادة تعيين كلمة المرور — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 9. EMERGENCY CONFIRMATION
        // ────────────────────────────────────────────────────────────────
        public static string EmergencyConfirmation(string name, string emergencyType, DateTime requestTime)
        {
            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='width:84px;height:84px;background:linear-gradient(135deg,{ColorDanger},#f87171);border-radius:24px;display:inline-flex;align-items:center;justify-content:center;margin:0 auto 20px;box-shadow:0 12px 24px rgba(220,38,38,0.25);'>
                        <span style='font-size:38px;'>🚨</span>
                    </div>
                    <h1 style='color:#991b1b;font-size:26px;font-weight:900;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>استلام بلاغ الطوارئ</h1>
                    <p style='color:{ColorMuted};font-size:14.5px;margin:0;font-family:""Cairo"",sans-serif;'>فريق الاستجابة السريعة يتعامل مع البلاغ الآن</p>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً <strong>{name}</strong>،</p>
                <p style='color:{ColorMuted};font-size:15px;line-height:1.8;font-family:""Cairo"",sans-serif;'>
                    تم توثيق واستلام طلب الطوارئ الخاص بك بنجاح، ويتم توجيهه وحجز الإسناد الطبي المطلوب فوراً.
                </p>

                <div style='background:#fef2f2;border:2px solid {ColorDanger};border-radius:18px;padding:24px;margin:28px 0;'>
                    <table style='width:100%;border-collapse:collapse;'>
                        <tr>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>🆘 نوع البلاغ الطارئ</td>
                            <td style='color:#991b1b;font-weight:800;font-size:15.5px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{emergencyType}</td>
                        </tr>
                        <tr style='border-top:1px solid #fee2e2;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>🕐 وقت تسجيل الطلب</td>
                            <td style='color:{ColorText};font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>{requestTime:dd/MM/yyyy — HH:mm}</td>
                        </tr>
                        <tr style='border-top:1px solid #fee2e2;'>
                            <td style='color:{ColorMuted};font-size:14px;padding:12px 0;font-family:""Cairo"",sans-serif;'>📌 حالة البلاغ الحالية</td>
                            <td style='color:#dc2626;font-weight:800;font-size:15px;padding:12px 0;font-family:""Cairo"",sans-serif;'>⚡ قيد المتابعة والإرسال الفوري</td>
                        </tr>
                    </table>
                </div>

                <p style='color:{ColorMuted};font-size:14px;text-align:center;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                    ابقَ هادئاً وفي موقعك الموصف. سيتواصل معك مسعف أو مسعفة من الغرفة المركزية خلال لحظات. 🙏
                </p>
            ", "تم استلام طلب الطوارئ — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // 10. DOCTOR APPROVAL/REJECTION
        // ────────────────────────────────────────────────────────────────
        public static string DoctorApproval(string name, bool isApproved, string? reason)
        {
            var (emoji, color, title, msg) = isApproved
                ? ("✅", ColorPrimary, "تم واعتماد طلب انضمامك كطبيب!",
                   "يسعدنا إبلاغك بأنه قد تم فحص واعتماد طلب اعتمادك كطبيب في منصة إطمن. يمكنك الآن دخول لوحة التحكم والبدء في استقبل المرضى وإدارة العيادة.")
                : ("❌", ColorDanger, "نتيجة مراجعة طلب الاعتماد",
                   "نأسف لإبلاغك بأن طلب انضمامك كطبيب في منصة إطمن لم يحصل على موافقة الاعتماد الفوري في الوقت الحالي.");

            return Wrap($@"
                <div style='text-align:center;margin-bottom:36px;'>
                    <div style='font-size:62px;margin-bottom:12px;'>{emoji}</div>
                    <h1 style='color:{color};font-size:26px;font-weight:900;margin:0 0 8px;font-family:""Cairo"",sans-serif;'>{title}</h1>
                </div>

                <p style='color:{ColorText};font-size:16px;line-height:1.8;font-family:""Cairo"",sans-serif;'>مرحباً دكتور <strong>{name}</strong>،</p>
                <p style='color:{ColorMuted};font-size:15px;line-height:1.8;font-family:""Cairo"",sans-serif;'>{msg}</p>

                {(!isApproved && !string.IsNullOrWhiteSpace(reason) ? $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background:#fef2f2;border-right:4px solid {ColorDanger};border-radius:14px;margin:24px 0;'>
                    <tr>
                        <td style='padding:18px;'>
                            <p style='color:#991b1b;font-size:14px;margin:0;line-height:1.7;font-family:""Cairo"",sans-serif;'>
                                <strong>توضيح أسباب اللجنة:</strong> {reason}
                            </p>
                        </td>
                    </tr>
                </table>" : "")}

                {(isApproved ? $@"
                <div style='text-align:center;margin:38px 0;'>
                    <a href='/Account/Login'
                       style='background:linear-gradient(135deg,{ColorPrimary} 0%,{ColorSecondary} 100%);color:#ffffff;text-decoration:none;
                              padding:18px 48px;border-radius:14px;font-size:17px;font-weight:800;font-family:""Cairo"",sans-serif;
                              display:inline-block;box-shadow:0 10px 25px -5px rgba(15,118,110,0.4);'>
                        🚀 دخول لوحة التحكم والبدء
                    </a>
                </div>" : $@"
                <p style='color:{ColorMuted};font-size:14px;text-align:center;font-family:""Cairo"",sans-serif;margin-top:24px;'>
                    للمزيد من الاستفسارات أو إعادة إرسال المستندات، تواصل مع فريق الشؤون الطبية عبر <a href='mailto:doctors@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>doctors@etmen.com</a>
                </p>")}
            ", isApproved ? "قبول اعتمادك كطبيب — منصة إطمن" : "تحديث بشأن طلب انضمامك — منصة إطمن");
        }

        // ────────────────────────────────────────────────────────────────
        // Shared Master Wrapper — Responsive Luxurious Arabic HTML Shell
        // ────────────────────────────────────────────────────────────────
        private static string Wrap(string content, string previewText)
        {
            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{previewText} — منصة إطمن</title>
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
<body style='margin:0;padding:0;background-color:{ColorBg};font-family:""Cairo"", ""Segoe UI"", Arial, sans-serif;direction:rtl;-webkit-font-smoothing:antialiased;'>

    <!-- Preview text for inbox preview list -->
    <div style='display:none;max-height:0;overflow:hidden;color:{ColorBg};'>{previewText} — منصة إطمن الرعاية الصحية الرقمية المتقدمة</div>

    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:{ColorBg};min-height:100vh;'>
      <tr>
        <td align='center' style='padding:40px 16px;'>

          <!-- Main Card Wrapper -->
          <table width='620' cellpadding='0' cellspacing='0' style='max-width:620px;width:100%;background:#ffffff;border-radius:24px;border:1px solid #e2e8f0;box-shadow:0 20px 40px -15px rgba(15,118,110,0.12);overflow:hidden;'>

            <!-- Luxurious Header Section -->
            <tr>
              <td style='background:linear-gradient(135deg, #0d9488 0%, #0f766e 50%, #115e59 100%);padding:40px 44px;text-align:center;'>
                <table align='center' cellpadding='0' cellspacing='0' style='margin:0 auto;'>
                    <tr>
                        <td style='background:rgba(255,255,255,0.15);padding:10px 24px;border-radius:50px;border:1px solid rgba(255,255,255,0.25);'>
                            <span style='color:#ffffff;font-size:24px;font-weight:900;letter-spacing:0.5px;font-family:""Cairo"",sans-serif;'>🏥 إطمن &nbsp;|&nbsp; ETMEN</span>
                        </td>
                    </tr>
                </table>
                <p style='color:rgba(255,255,255,0.92);font-size:13.5px;margin:12px 0 0;font-family:""Cairo"",sans-serif;font-weight:600;letter-spacing:0.3px;'>
                    منظومة الرعاية الصحية الرقمية المتكاملة ✦
                </p>
              </td>
            </tr>

            <!-- Body Section -->
            <tr>
              <td style='background:{ColorCard};padding:44px 50px;'>
                {content}
              </td>
            </tr>

            <!-- Luxurious Footer Section -->
            <tr>
              <td style='background:linear-gradient(180deg,#f8fafc 0%,#f1f5f9 100%);padding:32px 44px;text-align:center;border-top:1px solid #e2e8f0;'>
                
                <!-- Trust & Security Badges -->
                <table align='center' cellpadding='0' cellspacing='0' style='margin:0 auto 20px;'>
                    <tr>
                        <td style='color:{ColorMuted};font-size:12px;font-weight:700;font-family:""Cairo"",sans-serif;padding:0 8px;'>
                            🔒 تشفير 256-bit آمن
                        </td>
                        <td style='color:{ColorMuted};font-size:12px;'>•</td>
                        <td style='color:{ColorMuted};font-size:12px;font-weight:700;font-family:""Cairo"",sans-serif;padding:0 8px;'>
                            🏥 رعاية طبية معتمدة
                        </td>
                        <td style='color:{ColorMuted};font-size:12px;'>•</td>
                        <td style='color:{ColorMuted};font-size:12px;font-weight:700;font-family:""Cairo"",sans-serif;padding:0 8px;'>
                            ⚡ خدمة 24/7
                        </td>
                    </tr>
                </table>

                <p style='color:{ColorMuted};font-size:13px;margin:0 0 10px;line-height:1.6;font-family:""Cairo"",sans-serif;'>
                    هذا البريد أُرسل بصورة آلية معتمدة من <strong style=""color:{ColorPrimary}"">منصة إطمن الطبية</strong>.<br>
                    يرجى عدم الرد على هذا البريد بشكل مباشر.
                </p>
                <p style='color:#94a3b8;font-size:12px;margin:0;font-family:""Cairo"",sans-serif;'>
                    مركز الدعم الفني: <a href='mailto:support@etmen.com' style='color:{ColorPrimary};text-decoration:none;font-weight:700;'>support@etmen.com</a>
                    &nbsp;|&nbsp; © {DateTime.UtcNow.Year} منصة إطمن — جميع الحقوق محفوظة
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
