using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using KitsuneCommand.Core;

namespace KitsuneCommand.Services
{
    /// <summary>
    /// Manages server mods in the Mods/ directory. Supports listing, uploading (ZIP),
    /// deleting, and enabling/disabling mods.
    /// </summary>
    public class ModManagerService
    {
        private string _modsPath;

        /// <summary>
        /// Gets the path to the Mods directory.
        /// </summary>
        public string GetModsPath()
        {
            if (_modsPath != null && Directory.Exists(_modsPath))
                return _modsPath;

            // Derive game root from mod path: Mods/KitsuneCommand -> ../../
            var gameDir = Path.GetFullPath(Path.Combine(ModEntry.ModPath, "..", ".."));

            var candidates = new[]
            {
                Path.Combine(gameDir, "Mods"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Mods"),
            };

            foreach (var path in candidates)
            {
                if (Directory.Exists(path))
                {
                    _modsPath = Path.GetFullPath(path);
                    return _modsPath;
                }
            }

            // Create default location under game root
            _modsPath = Path.GetFullPath(Path.Combine(gameDir, "Mods"));
            Directory.CreateDirectory(_modsPath);
            return _modsPath;
        }

        /// <summary>
        /// Lists all installed mods with metadata from ModInfo.xml.
        /// </summary>
        public List<ModInfo> GetMods()
        {
            var modsPath = GetModsPath();
            var mods = new List<ModInfo>();

            if (!Directory.Exists(modsPath))
                return mods;

            foreach (var dir in Directory.GetDirectories(modsPath))
            {
                var folderName = Path.GetFileName(dir);
                var isDisabled = folderName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                var mod = new ModInfo
                {
                    FolderName = folderName,
                    DisplayName = isDisabled ? folderName.Substring(0, folderName.Length - 9) : folderName,
                    IsEnabled = !isDisabled,
                    FolderSize = GetDirectorySize(dir),
                    IsProtected = folderName.Equals("KitsuneCommand", StringComparison.OrdinalIgnoreCase),
                };

                // Try to read ModInfo.xml
                var modInfoPath = Path.Combine(dir, "ModInfo.xml");
                if (File.Exists(modInfoPath))
                {
                    try
                    {
                        var doc = XDocument.Load(modInfoPath);
                        mod.DisplayName = doc.Descendants("Name").FirstOrDefault()?.Attribute("value")?.Value ?? mod.DisplayName;
                        mod.Version = doc.Descendants("Version").FirstOrDefault()?.Attribute("value")?.Value;
                        mod.Author = doc.Descendants("Author").FirstOrDefault()?.Attribute("value")?.Value;
                        mod.Description = doc.Descendants("Description").FirstOrDefault()?.Attribute("value")?.Value;
                        mod.Website = doc.Descendants("Website").FirstOrDefault()?.Attribute("value")?.Value;
                    }
                    catch { /* Ignore malformed ModInfo.xml */ }
                }

                mods.Add(mod);
            }

            return mods.OrderBy(m => m.DisplayName).ToList();
        }

        /// <summary>
        /// Uploads and extracts a ZIP file as a new mod.
        /// </summary>
        public ModInfo UploadMod(Stream zipStream, string fileName)
        {
            var modsPath = GetModsPath();

            // Extract to a temp directory first
            var tempDir = Path.Combine(Path.GetTempPath(), "kc_mod_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract ZIP
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Security: prevent path traversal
                        var entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        if (entryPath.Contains(".."))
                            throw new InvalidOperationException("ZIP contains path traversal entries.");

                        var destPath = Path.Combine(tempDir, entryPath);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // Directory entry
                            Directory.CreateDirectory(destPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            entry.ExtractToFile(destPath, true);
                        }
                    }
                }

                // Determine if there's a single root folder or multiple items
                var topItems = Directory.GetFileSystemEntries(tempDir);
                string sourceDir;

                if (topItems.Length == 1 && Directory.Exists(topItems[0]))
                {
                    // Single root folder — use it directly
                    sourceDir = topItems[0];
                }
                else
                {
                    // Multiple items — wrap in a folder named after the ZIP
                    var modName = Path.GetFileNameWithoutExtension(fileName);
                    var wrapperDir = Path.Combine(tempDir, "_wrapped_" + modName);
                    Directory.CreateDirectory(wrapperDir);
                    foreach (var item in topItems)
                    {
                        var dest = Path.Combine(wrapperDir, Path.GetFileName(item));
                        if (Directory.Exists(item))
                            CopyDirectory(item, dest);
                        else
                            File.Copy(item, dest);
                    }
                    sourceDir = wrapperDir;
                }

                var modFolderName = Path.GetFileName(sourceDir);
                if (modFolderName.StartsWith("_wrapped_"))
                    modFolderName = modFolderName.Substring(9);

                var destDir = Path.Combine(modsPath, modFolderName);

                // If mod already exists, remove it first
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, true);

                CopyDirectory(sourceDir, destDir);

                // Return info about the uploaded mod
                return GetMods().FirstOrDefault(m => m.FolderName.Equals(modFolderName, StringComparison.OrdinalIgnoreCase))
                       ?? new ModInfo { FolderName = modFolderName, DisplayName = modFolderName, IsEnabled = true };
            }
            finally
            {
                // Clean up temp directory
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        /// <summary>
        /// Deletes a mod folder. Cannot delete KitsuneCommand itself.
        /// </summary>
        public void DeleteMod(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
                throw new ArgumentException("Mod name is required.");

            // Safety: prevent deleting KitsuneCommand
            if (modName.Equals("KitsuneCommand", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete KitsuneCommand.");

            // Safety: prevent path traversal
            if (modName.Contains("..") || modName.Contains(Path.DirectorySeparatorChar.ToString())
                || modName.Contains(Path.AltDirectorySeparatorChar.ToString()))
                throw new InvalidOperationException("Invalid mod name.");

            var modPath = Path.Combine(GetModsPath(), modName);
            if (!Directory.Exists(modPath))
                throw new FileNotFoundException($"Mod '{modName}' not found.");

            Directory.Delete(modPath, true);
        }

        /// <summary>
        /// Toggles a mod enabled/disabled by renaming with .disabled suffix.
        /// </summary>
        public void ToggleMod(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
                throw new ArgumentException("Mod name is required.");

            if (modName.Equals("KitsuneCommand", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot disable KitsuneCommand.");

            if (modName.Contains(".."))
                throw new InvalidOperationException("Invalid mod name.");

            var modsPath = GetModsPath();
            var modPath = Path.Combine(modsPath, modName);

            if (!Directory.Exists(modPath))
                throw new FileNotFoundException($"Mod '{modName}' not found.");

            if (modName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
            {
                // Enable: remove .disabled suffix
                var newName = modName.Substring(0, modName.Length - 9);
                Directory.Move(modPath, Path.Combine(modsPath, newName));
            }
            else
            {
                // Disable: add .disabled suffix
                Directory.Move(modPath, Path.Combine(modsPath, modName + ".disabled"));
            }
        }

        private static long GetDirectorySize(string path)
        {
            try
            {
                return new DirectoryInfo(path)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }
            catch { return 0; }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    public class ModInfo
    {
        public string FolderName { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public long FolderSize { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsProtected { get; set; }
    }
}
