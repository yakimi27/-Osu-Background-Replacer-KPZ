using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OsuBackgroundReplacerMain.Logic
{
    internal class Operations
    {
        public static async Task<List<string>> Replacement(IProgress<int>? progress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ImageOperations.SelectedImagePath) ||
                    string.IsNullOrEmpty(FolderOperations.SelectedFolderPath) ||
                    !File.Exists(ImageOperations.SelectedImagePath) ||
                    !Directory.Exists(FolderOperations.SelectedFolderPath))
                {
                    throw new InvalidOperationException("File or folder does not exist.");
                }

                List<string> replacedFiles = new List<string>();

                var allImageFiles = await Task.Run(() =>
                    Directory.GetDirectories(FolderOperations.SelectedFolderPath)
                    .SelectMany(folder => Directory.GetFiles(folder, "*.*")
                        .Where(f => Constants.IsSupportedImage(f)))
                    .ToList());

                int total = allImageFiles.Count;
                int current = 0;

                var folders = Directory.GetDirectories(FolderOperations.SelectedFolderPath);

                foreach (var folder in folders)
                {
                    var imageFiles = await Task.Run(() => Directory.GetFiles(folder, "*.*")
                        .Where(f => Constants.IsSupportedImage(f))
                        .ToList());

                    if (imageFiles.Count > 0)
                    {
                        foreach (var imageFile in imageFiles)
                        {
                            string newFileName = Path.GetFileName(imageFile);
                            string newFilePath = Path.Combine(folder, newFileName);

                            try
                            {
                                await Task.Run(() => File.Copy(ImageOperations.SelectedImagePath, newFilePath, true));
                                replacedFiles.Add(newFilePath);
                            }
                            catch (Exception exception)
                            {
                                throw new InvalidOperationException($"Issue: {exception.Message}");
                            }

                            current++;
                            int percent = (total > 0) ? (int)((double)current / total * 100) : 0;
                            progress?.Report(percent);
                        }
                    }
                }

                return replacedFiles;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(exception.Message);
            }
        }
    }
}