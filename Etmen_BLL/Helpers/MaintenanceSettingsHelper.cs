using System;
using System.IO;
using System.Text.Json;

namespace Etmen_BLL.Helpers
{
    public static class MaintenanceSettingsHelper
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maintenance_settings.json");

        public static bool IsPatientMaintenanceActive { get; set; } = false;
        public static string PatientMaintenanceMessage { get; set; } = "النظام تحت الصيانة حالياً. نعتذر عن الإزعاج وسنعود قريباً.";
        public static bool IsStaffMaintenanceActive { get; set; } = false;
        public static string StaffMaintenanceMessage { get; set; } = "نظام موظفي المستشفى تحت الصيانة حالياً. يرجى مراجعة الإدارة.";

        static MaintenanceSettingsHelper()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), "maintenance_settings.json");
                }
                if (!File.Exists(path))
                {
                    // Fallback to project root directory
                    var parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
                    while (parent != null)
                    {
                        var candidate = Path.Combine(parent.FullName, "Etmen_PL", "maintenance_settings.json");
                        if (File.Exists(candidate))
                        {
                            path = candidate;
                            break;
                        }
                        parent = parent.Parent;
                    }
                }

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("IsPatientMaintenanceActive", out var pAct))
                        IsPatientMaintenanceActive = pAct.GetBoolean();
                    if (doc.RootElement.TryGetProperty("PatientMaintenanceMessage", out var pMsg))
                        PatientMaintenanceMessage = pMsg.GetString() ?? "النظام تحت الصيانة حالياً. نعتذر عن الإزعاج وسنعود قريباً.";
                    if (doc.RootElement.TryGetProperty("IsStaffMaintenanceActive", out var sAct))
                        IsStaffMaintenanceActive = sAct.GetBoolean();
                    if (doc.RootElement.TryGetProperty("StaffMaintenanceMessage", out var sMsg))
                        StaffMaintenanceMessage = sMsg.GetString() ?? "نظام موظفي المستشفى تحت الصيانة حالياً. يرجى مراجعة الإدارة.";
                }
            }
            catch { }
        }

        public static void Save(bool isPatientActive, string patientMessage, bool isStaffActive, string staffMessage)
        {
            try
            {
                IsPatientMaintenanceActive = isPatientActive;
                PatientMaintenanceMessage = patientMessage;
                IsStaffMaintenanceActive = isStaffActive;
                StaffMaintenanceMessage = staffMessage;

                var json = JsonSerializer.Serialize(new
                {
                    IsPatientMaintenanceActive,
                    PatientMaintenanceMessage,
                    IsStaffMaintenanceActive,
                    StaffMaintenanceMessage
                }, new JsonSerializerOptions { WriteIndented = true });

                // Try saving in multiple places to ensure persistence
                File.WriteAllText(FilePath, json);

                var localPath = Path.Combine(Directory.GetCurrentDirectory(), "maintenance_settings.json");
                File.WriteAllText(localPath, json);

                var parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
                while (parent != null)
                {
                    var candidateDir = Path.Combine(parent.FullName, "Etmen_PL");
                    if (Directory.Exists(candidateDir))
                    {
                        File.WriteAllText(Path.Combine(candidateDir, "maintenance_settings.json"), json);
                        break;
                    }
                    parent = parent.Parent;
                }
            }
            catch { }
        }
    }
}
