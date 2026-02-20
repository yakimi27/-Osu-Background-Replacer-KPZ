using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OsuBackgroundReplacerMain.Logic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace OsuBackgroundReplacerMain
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Current = this;
            ExtendsContentIntoTitleBar = true;

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 1000;
            presenter.PreferredMinimumHeight = 860;
            this.AppWindow.SetPresenter(presenter);
        }

        private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            await FolderOperations.ChooseFolderManually(this);

            FolderPathTextBlock.Text = FolderOperations.SelectedFolderPath ?? "No selection";
            ToolTipService.SetToolTip(FolderPathTextBlock, FolderOperations.SelectedFolderPath);
            FolderPathTextBlock.Visibility = Visibility.Visible;
        }

        private async void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            await ImageOperations.ChooseImageManually(this);

            ImagePathTextBlock.Text = ImageOperations.SelectedImagePath ?? "No selection";
            ToolTipService.SetToolTip(ImagePathTextBlock, ImageOperations.SelectedImagePath);
            ImagePathTextBlock.Visibility = Visibility.Visible;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void DropFolder(object sender, DragEventArgs e)
        {
            await FolderOperations.DragAndDropFolder(e);

            FolderPathTextBlock.Text = FolderOperations.SelectedFolderPath ?? "No selection";
            ToolTipService.SetToolTip(FolderPathTextBlock, FolderOperations.SelectedFolderPath);
            FolderPathTextBlock.Visibility = Visibility.Visible;
        }

        private async void DropFile(object sender, DragEventArgs e)
        {
            await ImageOperations.DragAndDropImage(e);

            ImagePathTextBlock.Text = ImageOperations.SelectedImagePath ?? "No selection";
            ToolTipService.SetToolTip(ImagePathTextBlock, ImageOperations.SelectedImagePath);
            ImagePathTextBlock.Visibility = Visibility.Visible;
        }

        private async void Replace_Click(object sender, RoutedEventArgs e)
        {

            var confirmation = await ShowDialogAsync(
                "You are going to replace all images in folders inside {} folder to {} image. Is all right?",
                "Confirmation", "Yes", "No");
            if (confirmation != ContentDialogResult.Primary) return;

            if (string.IsNullOrEmpty(FolderOperations.SelectedFolderPath) ||
                !FolderOperations.SelectedFolderPath.Contains("osu!\\Songs", StringComparison.OrdinalIgnoreCase))
            {
                var result = await ShowDialogAsync(
                    "The selected path doesn't contain the \"osu!\\Songs\" folder. Are you sure you want to continue?",
                    "Warning", "Yes", "No");

                if (result != ContentDialogResult.Primary) return;
            }

            var progress = new Progress<int>(p => ReplacingProgressBar.Value = p);

            ReplacingProgressBar.Visibility = Visibility.Visible;
            List<string> replacedFiles = await Operations.Replacement(progress);
            ReplacingProgressBar.Visibility = Visibility.Collapsed;

            ActivityLog.ItemsSource = replacedFiles;
        }

        public static async Task<ContentDialogResult> ShowDialogAsync(string content, string title, string primaryBtnText = "OK", string closeBtnText = null)
        {
            if (Current.DispatcherQueue.HasThreadAccess)
            {
                return await ShowDialogInternal(content, title, primaryBtnText, closeBtnText);
            }
            else
            {
                var tcs = new TaskCompletionSource<ContentDialogResult>();
                Current.DispatcherQueue.TryEnqueue(async () =>
                {
                    var result = await ShowDialogInternal(content, title, primaryBtnText, closeBtnText);
                    tcs.SetResult(result);
                });
                return await tcs.Task;
            }
        }

        private static async Task<ContentDialogResult> ShowDialogInternal(string content, string title, string primaryBtnText, string closeBtnText)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = Current.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = content,
                PrimaryButtonText = primaryBtnText,
                CloseButtonText = closeBtnText
            };
            return await dialog.ShowAsync();
        }

        private void ListBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var listBox = sender as ListBox;
            var scrollViewer = GetScrollViewer(listBox);

            if (scrollViewer == null) return;

            var pointerPoint = e.GetCurrentPoint(listBox);
            int wheelDelta = pointerPoint.Properties.MouseWheelDelta;

            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            bool isCtrlDown = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

            if (isCtrlDown)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - wheelDelta);
                e.Handled = true;
            }
            else
            {
                // scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - wheelDelta);
                // e.Handled = true;
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject dep)
        {
            if (dep is ScrollViewer sv) return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
            {
                var child = VisualTreeHelper.GetChild(dep, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}