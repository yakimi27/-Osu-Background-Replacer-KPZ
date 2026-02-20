using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace OsuBackgroundReplacerMain.Logic
{
    class FolderOperations
    {
        public static string SelectedFolderPath { get; set; }

        public static async Task ChooseFolderManually(Window window)
        {
            try
            {
                var folderPicker = new FolderPicker();

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    SelectedFolderPath = folder.Path;
                }
            }
            catch (Exception exception)
            {
                await MainWindow.ShowDialogAsync(exception.Message, "Folder Error");
            }
        }

        public static async Task DragAndDropFolder(DragEventArgs e)
        {
            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var folder = items[0] as StorageFolder;
                        if (folder != null)
                        {
                            SelectedFolderPath = folder.Path;
                        }
                        else
                        {
                            await MainWindow.ShowDialogAsync("The dropped item is not a folder.", "Error");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                await MainWindow.ShowDialogAsync(exception.Message, "Error selecting folder");
            }
        }
    }
}