using Microsoft.AspNetCore.Http;

namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Handles file persistence to wwwroot/uploads/.
    /// Generates a GUID-based filename to prevent naming collisions.
    /// Used by LabService (OCR images) and PatientService (profile pictures).
    /// </summary>
    public static class FileUploadHelper
    {
        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };

        /// <summary>
        /// Saves <paramref name="file"/> under <paramref name="uploadsRoot"/>/uploads/
        /// and returns the relative web path (e.g. "/uploads/abc123.jpg").
        /// Returns null when the file is null, empty, or has a disallowed extension.
        /// </summary>
        public static async Task<string?> SaveAsync(IFormFile? file, string uploadsRoot)
        {
            if (file is null || file.Length == 0)
                return null;

            var ext = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(ext))
                return null;

            var fileName  = $"{Guid.NewGuid():N}{ext}";
            var directory = Path.Combine(uploadsRoot, "uploads");

            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, fileName);
            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            return $"/uploads/{fileName}";
        }

        /// <summary>Deletes a previously saved upload given its relative web path.</summary>
        public static void Delete(string? relativePath, string uploadsRoot)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var fullPath = Path.Combine(uploadsRoot, relativePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
