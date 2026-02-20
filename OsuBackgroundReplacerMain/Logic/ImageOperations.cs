using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace OsuBackgroundReplacerMain.Logic
{
    internal class ImageOperations
    {
        public static string SelectedImagePath { get; set; }

        public static async Task ChooseImageManually(Window window)
        {
            try
            {
                var openPicker = new FileOpenPicker();

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".jpeg");
                openPicker.FileTypeFilter.Add(".png");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    SelectedImagePath = file.Path;
                }
            }
            catch (Exception exception)
            {
                await MainWindow.ShowDialogAsync(exception.Message, "Image Error");
            }
        }

        public static async Task DragAndDropImage(DragEventArgs e)
        {
            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var file = items[0] as StorageFile;
                        if (file != null)
                        {
                            string type = file.FileType.ToLower();
                            if (type == ".jpg" || type == ".jpeg" || type == ".png")
                            {
                                SelectedImagePath = file.Path;
                            }
                            else
                            {
                                await MainWindow.ShowDialogAsync("The dropped item is not a valid image file.", "Error");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                await MainWindow.ShowDialogAsync(exception.Message, "Error selecting image");
            }
        }
    }
}