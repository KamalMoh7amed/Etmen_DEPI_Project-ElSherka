using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Generates professional Arabic PDF reports using QuestPDF.
    /// Covers lab results, risk assessments, and appointment confirmations.
    /// </summary>
    public sealed class PdfReportService : IServices.IPdfReportService
    {
        private static readonly string Green    = "#1a6b5a";
        private static readonly string Accent   = "#22c55e";
        private static readonly string Warning  = "#f59e0b";
        private static readonly string Danger   = "#ef4444";
        private static readonly string LightBg  = "#f0fdf4";
        private static readonly string TextGray = "#6b7280";
        private static readonly string BlueBg   = "#eff6ff";
        private static readonly string BlueText = "#0369a1";
        private static readonly string YellowBg = "#fefce8";
        private static readonly string YellowBorder = "#fde68a";
        private static readonly string YellowText = "#92400e";

        static PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ─────────────────────────────────────────────────────────────────
        // 1. LAB RESULT PDF
        // ─────────────────────────────────────────────────────────────────
        public Task<byte[]> GenerateLabReportPdfAsync(
            string patientName, string testName, DateTime testDate,
            string? results, string? ocrData)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(40);
                    page.MarginVertical(32);
                    page.ContentFromRightToLeft();
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(c => BuildHeader(c, "تقرير نتيجة التحليل"));

                    page.Content().PaddingVertical(16).Column(col =>
                    {
                        // Patient info card
                        col.Item().Background(LightBg).Padding(16).Column(inner =>
                        {
                            inner.Item().Text("بيانات المريض").Bold().FontSize(13).FontColor(Green);
                            inner.Item().Height(8);
                            inner.Item().Row(row =>
                            {
                                InfoCell(row, "اسم المريض", patientName);
                                InfoCell(row, "اسم التحليل", testName);
                                InfoCell(row, "تاريخ التحليل", testDate.ToString("dd/MM/yyyy"));
                            });
                        });

                        col.Item().Height(16);

                        col.Item().Border(1).BorderColor(Accent).Padding(16).Column(inner =>
                        {
                            inner.Item().Text("نتيجة التحليل").Bold().FontSize(13).FontColor(Green);
                            inner.Item().Height(8);
                            if (string.IsNullOrWhiteSpace(results))
                            {
                                inner.Item().Text("لم يتم إدخال النتيجة بعد.").FontSize(11).FontColor("#374151");
                            }
                            else
                            {
                                RenderMarkdownContent(inner, results);
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(ocrData))
                        {
                            col.Item().Height(12);
                            col.Item().Background(BlueBg).Padding(16).Column(inner =>
                            {
                                inner.Item().Text("البيانات المستخرجة (OCR)").Bold().FontSize(13).FontColor(BlueText);
                                inner.Item().Height(8);
                                RenderMarkdownContent(inner, ocrData);
                            });
                        }

                        col.Item().Height(20);
                        col.Item().Background(YellowBg).Border(1).BorderColor(YellowBorder).Padding(12)
                            .Text("تنبيه: هذا التقرير لأغراض المعلومات فقط. يُرجى مراجعة طبيبك لتفسير النتائج.")
                            .FontSize(10).FontColor(YellowText).Italic();
                    });

                    page.Footer().Element(c => BuildFooter(c));
                });
            });

            return Task.FromResult(pdf.GeneratePdf());
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. RISK ASSESSMENT PDF
        // ─────────────────────────────────────────────────────────────────
        public Task<byte[]> GenerateRiskReportPdfAsync(
            string patientName, string riskLevel, decimal riskScore,
            List<string> recommendations, List<string> triggeredSymptoms,
            DateTime assessmentDate, bool isEmergency)
        {
            var riskPercent = (int)(riskScore * 100);
            var riskColor = riskLevel is "Emergency" or "طارئ" ? Danger
                          : riskLevel is "High" or "عالي"      ? Warning
                          : Green;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(40);
                    page.MarginVertical(32);
                    page.ContentFromRightToLeft();
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(c => BuildHeader(c, "تقرير تقييم مستوى الخطر"));

                    page.Content().PaddingVertical(16).Column(col =>
                    {
                        if (isEmergency)
                        {
                            col.Item().Background(Danger).Padding(12)
                                .Text("حالة طوارئ — يُرجى التوجه فوراً لأقرب مستشفى أو الاتصال بالإسعاف")
                                .FontSize(12).Bold().FontColor(Colors.White);
                            col.Item().Height(12);
                        }

                        col.Item().Background(LightBg).Padding(16).Column(inner =>
                        {
                            inner.Item().Text("بيانات المريض").Bold().FontSize(13).FontColor(Green);
                            inner.Item().Height(8);
                            inner.Item().Row(row =>
                            {
                                InfoCell(row, "اسم المريض", patientName);
                                InfoCell(row, "تاريخ التقييم", assessmentDate.ToString("dd/MM/yyyy HH:mm"));
                            });
                        });

                        col.Item().Height(16);

                        col.Item().Border(2).BorderColor(riskColor).Padding(20).AlignCenter().Column(inner =>
                        {
                            inner.Item().Text($"{riskPercent}%").FontSize(48).Bold().FontColor(riskColor);
                            inner.Item().Text($"مستوى الخطر: {riskLevel}").FontSize(16).Bold().FontColor(riskColor);
                        });

                        col.Item().Height(16);

                        if (triggeredSymptoms.Count > 0)
                        {
                            col.Item().Background("#fef2f2").Padding(16).Column(inner =>
                            {
                                inner.Item().Text("الأعراض المُثيرة للخطر").Bold().FontSize(13).FontColor(Danger);
                                inner.Item().Height(8);
                                foreach (var s in triggeredSymptoms)
                                    inner.Item().Text($"• {s}").FontSize(11).FontColor("#374151");
                            });
                            col.Item().Height(12);
                        }

                        col.Item().Background(LightBg).Padding(16).Column(inner =>
                        {
                            inner.Item().Text("التوصيات الطبية").Bold().FontSize(13).FontColor(Green);
                            inner.Item().Height(8);
                            if (recommendations.Count == 0)
                                inner.Item().Text("لا توجد توصيات محددة.").FontSize(11).FontColor(TextGray);
                            else
                                foreach (var r in recommendations)
                                    inner.Item().Text($"- {r}").FontSize(11).FontColor("#374151");
                        });

                        col.Item().Height(20);
                        col.Item().Background(YellowBg).Border(1).BorderColor(YellowBorder).Padding(12)
                            .Text("هذا التقرير للمعلومات العامة فقط ولا يُعد بديلاً عن الاستشارة الطبية المتخصصة.")
                            .FontSize(10).FontColor(YellowText).Italic();
                    });

                    page.Footer().Element(c => BuildFooter(c));
                });
            });

            return Task.FromResult(pdf.GeneratePdf());
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. APPOINTMENT PDF
        // ─────────────────────────────────────────────────────────────────
        public Task<byte[]> GenerateAppointmentPdfAsync(
            string patientName, string doctorName, string? specialization,
            DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime,
            string? notes)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(40);
                    page.MarginVertical(32);
                    page.ContentFromRightToLeft();
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Element(c => BuildHeader(c, "تأكيد حجز الموعد"));

                    page.Content().PaddingVertical(16).Column(col =>
                    {
                        col.Item().Background(LightBg).Padding(20).Column(inner =>
                        {
                            inner.Item().Text("تفاصيل الموعد").Bold().FontSize(14).FontColor(Green);
                            inner.Item().Height(12);
                            inner.Item().Row(row =>
                            {
                                InfoCell(row, "المريض", patientName);
                                InfoCell(row, "الطبيب", $"د. {doctorName}");
                            });
                            inner.Item().Height(8);
                            inner.Item().Row(row =>
                            {
                                InfoCell(row, "التخصص", specialization ?? "—");
                                InfoCell(row, "تاريخ الموعد", appointmentDate.ToString("dd/MM/yyyy"));
                            });
                            inner.Item().Height(8);
                            inner.Item().Row(row =>
                            {
                                InfoCell(row, "وقت البدء", startTime.ToString(@"hh\:mm"));
                                InfoCell(row, "وقت الانتهاء", endTime.ToString(@"hh\:mm"));
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(notes))
                        {
                            col.Item().Height(16);
                            col.Item().Border(1).BorderColor(Accent).Padding(16).Column(inner =>
                            {
                                inner.Item().Text("ملاحظات").Bold().FontSize(13).FontColor(Green);
                                inner.Item().Height(6);
                                inner.Item().Text(notes).FontSize(11).FontColor("#374151");
                            });
                        }

                        col.Item().Height(24);

                        col.Item().Background(YellowBg).Border(1).BorderColor(YellowBorder).Padding(14).Column(inner =>
                        {
                            inner.Item().Text("تذكير تلقائي").Bold().FontSize(12).FontColor(YellowText);
                            inner.Item().Height(4);
                            inner.Item().Text("ستصلك رسائل تذكير قبل الموعد بيوم كامل وقبله بساعتين.")
                                .FontSize(11).FontColor(YellowText);
                        });

                        col.Item().Height(12);
                        col.Item().Background(LightBg).Padding(12)
                            .Text("يُرجى الحضور قبل الموعد بـ 10 دقائق على الأقل.")
                            .FontSize(10).FontColor(TextGray).Italic();
                    });

                    page.Footer().Element(c => BuildFooter(c));
                });
            });

            return Task.FromResult(pdf.GeneratePdf());
        }

        // ─────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────

        private static void BuildHeader(IContainer container, string title)
        {
            container.Background(Green).Padding(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("منصة اطمئن").FontSize(18).Bold().FontColor(Colors.White);
                    col.Item().Text("نظام الصحة الرقمية المتكامل").FontSize(10).FontColor("#d1fae5");
                });
                row.ConstantItem(200).AlignRight().AlignMiddle()
                    .Text(title).FontSize(13).Bold().FontColor(Colors.White);
            });
        }

        private static void BuildFooter(IContainer container)
        {
            container.BorderTop(1).BorderColor("#e5e7eb").PaddingTop(8).Row(row =>
            {
                row.RelativeItem()
                    .Text($"© {DateTime.UtcNow.Year} منصة اطمئن — جميع الحقوق محفوظة")
                    .FontSize(9).FontColor(TextGray);
                row.ConstantItem(120).AlignRight()
                    .Text(x =>
                    {
                        x.Span("صفحة ").FontSize(9).FontColor(TextGray);
                        x.CurrentPageNumber().FontSize(9).FontColor(TextGray);
                        x.Span(" / ").FontSize(9).FontColor(TextGray);
                        x.TotalPages().FontSize(9).FontColor(TextGray);
                    });
            });
        }

        private static void InfoCell(RowDescriptor row, string label, string value)
        {
            row.RelativeItem().Padding(4).Column(col =>
            {
                col.Item().Text(label).FontSize(9).FontColor(TextGray);
                col.Item().Text(value).FontSize(12).Bold().FontColor("#1f2937");
            });
        }

        private void RenderMarkdownContent(ColumnDescriptor col, string? markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return;

            var lines = markdown.Split('\n', StringSplitOptions.TrimEntries);
            
            var tableRows = new List<List<string>>();
            bool inTable = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("|") && line.EndsWith("|"))
                {
                    if (line.Replace(" ", "").Replace("-", "").Replace("|", "").Replace(":", "").Length == 0)
                    {
                        continue;
                    }

                    var cells = line.Split('|', StringSplitOptions.TrimEntries)
                                    .Skip(1)
                                    .Take(line.Split('|').Length - 2)
                                    .ToList();

                    if (cells.Count > 0)
                    {
                        tableRows.Add(cells);
                    }
                    inTable = true;
                    continue;
                }
                else
                {
                    if (inTable && tableRows.Count > 0)
                    {
                        RenderQuestTable(col, tableRows);
                        tableRows.Clear();
                        inTable = false;
                    }
                }

                if (string.IsNullOrEmpty(line))
                {
                    col.Item().Height(4);
                    continue;
                }

                if (line.StartsWith("###"))
                {
                    var text = line.Substring(3).Trim();
                    col.Item().PaddingTop(8).PaddingBottom(4).Text(text).Bold().FontSize(12).FontColor(Green);
                }
                else if (line.StartsWith("##"))
                {
                    var text = line.Substring(2).Trim();
                    col.Item().PaddingTop(10).PaddingBottom(6).Text(text).Bold().FontSize(14).FontColor(Green);
                }
                else if (line.StartsWith("#"))
                {
                    var text = line.Substring(1).Trim();
                    col.Item().PaddingTop(12).PaddingBottom(8).Text(text).Bold().FontSize(16).FontColor(Green);
                }
                else if (line.StartsWith("-") || line.StartsWith("*"))
                {
                    var text = line.Substring(1).Trim();
                    col.Item().PaddingBottom(2).Row(row =>
                    {
                        row.AutoItem().Text("• ").Bold().FontSize(11).FontColor(Green);
                        row.RelativeItem().Text(t => ParseBoldText(t, text));
                    });
                }
                else
                {
                    col.Item().PaddingBottom(4).Text(t => ParseBoldText(t, line));
                }
            }

            if (inTable && tableRows.Count > 0)
            {
                RenderQuestTable(col, tableRows);
            }
        }

        private void RenderQuestTable(ColumnDescriptor col, List<List<string>> rows)
        {
            col.Item().PaddingVertical(8).Table(table =>
            {
                int colCount = rows[0].Count;
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        columns.RelativeColumn();
                    }
                });

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var cells = rows[rowIndex];
                    bool isHeader = rowIndex == 0;

                    for (int colIndex = 0; colIndex < cells.Count; colIndex++)
                    {
                        var cellText = cells[colIndex];
                        
                        string bgColor = "#FFFFFF";
                        string textColor = "#374151";
                        bool isBold = false;
                        
                        if (isHeader)
                        {
                            bgColor = "#F8FAFC";
                            textColor = "#1E293B";
                            isBold = true;
                        }
                        else if (cellText == "طبيعي" || cellText == "Normal")
                        {
                            bgColor = "#DCFCE7";
                            textColor = "#15803D";
                            isBold = true;
                        }
                        else if (cellText.Contains("مرتفع") || cellText.Contains("منخفض") || cellText.Contains("غير طبيعي") || cellText.Contains("Abnormal"))
                        {
                            bgColor = "#FEE2E2";
                            textColor = "#B91C1C";
                            isBold = true;
                        }

                        table.Cell().Row((uint)(rowIndex + 1)).Column((uint)(colIndex + 1))
                             .BorderBottom(1).BorderColor("#E5E7EB")
                             .Background(bgColor)
                             .Padding(6)
                             .AlignRight()
                             .Text(t =>
                             {
                                 var span = t.Span(cellText).FontSize(9).FontColor(textColor);
                                 if (isBold)
                                 {
                                     span.Bold();
                                 }
                             });
                    }
                }
            });
        }

        private void ParseBoldText(TextDescriptor textDescriptor, string text)
        {
            var parts = text.Split("**");
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i % 2 == 1)
                {
                    textDescriptor.Span(part).Bold().FontColor("#1E293B");
                }
                else
                {
                    textDescriptor.Span(part).FontColor("#374151");
                }
            }
        }
    }
}
