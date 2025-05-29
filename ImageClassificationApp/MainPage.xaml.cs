using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageClassificationApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private async void PickPicture_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker imagePicker = new FileOpenPicker();

            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".jpeg");
            imagePicker.FileTypeFilter.Add(".png");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            StorageFile imageFile = await imagePicker.PickSingleFileAsync();
            if (imageFile != null)
            {
                ImageShow.Source = null;
                DragAvailableIndicator.Visibility = Visibility.Collapsed;
                RecognizingIndicator.Visibility = Visibility.Visible;
                IRandomAccessStream imageStream = await imageFile.OpenAsync(FileAccessMode.Read);
                IRandomAccessStream imageStreamForClassify = imageStream.CloneStream();
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(imageStream);
                ImageShow.Source = bitmapImage;
                ClassifyImage(imageStreamForClassify);
                RecognizingIndicator.Visibility = Visibility.Collapsed;
                DragAvailableIndicator.Visibility = Visibility.Visible;
            }
        }

        private async void ImageDragSystem_Drop(object sender, DragEventArgs e)
        {
            RecognizingIndicator.Visibility = Visibility.Visible;
            DragAvailableIndicator.Visibility = Visibility.Collapsed;
            var defer = e.GetDeferral();
            try
            {
                DataPackageView packageView = e.DataView;
                if (packageView.Contains(StandardDataFormats.StorageItems))
                {
                    List<StorageFile> draggedImages = new List<StorageFile>();
                    var imageFiles = await packageView.GetStorageItemsAsync();
                    if (imageFiles.Count > 1)
                        throw new NotSupportedException();
                    if (imageFiles[0].IsOfType(StorageItemTypes.File))
                    {
                        ImageShow.Source = null;
                        StorageFile imageFile = imageFiles[0] as StorageFile;
                        IRandomAccessStream imageStream = await imageFile.OpenAsync(FileAccessMode.Read);
                        IRandomAccessStream imageStreamForClassify = imageStream.CloneStream();
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(imageStream);
                        ImageShow.Source = bitmapImage;
                        ClassifyImage(imageStreamForClassify);
                    }
                    else throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "出现错误",
                    Content = "请确保您拖入了一个图片文件，错误信息" + ex.Message,
                    CloseButtonText = "关闭"
                };


                ContentDialogResult result = await errorDialog.ShowAsync();
            }
            finally
            {
                defer.Complete();
                RecognizingIndicator.Visibility = Visibility.Collapsed;
                DragAvailableIndicator.Visibility = Visibility.Visible;
            }
        }

        private void ImageDragSystem_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;

            e.Handled = true;
        }

        private async void ClassifyImage(IRandomAccessStream imageStream)
        {

            String ClassificationAnalytics = String.Empty;
            String ClassificationResult = String.Empty;
            var imageBytes = new byte[imageStream.Size];
            await imageStream.ReadAsync(imageBytes.AsBuffer(), (uint)imageStream.Size, Windows.Storage.Streams.InputStreamOptions.None);
            ChineseMedicineClassificationModel.ChineseMedicineClassification.ModelInput sampleData = new ChineseMedicineClassificationModel.ChineseMedicineClassification.ModelInput()
            {
                ImageSource = imageBytes,
            };

            var sortedScoresWithLabel = ChineseMedicineClassificationModel.ChineseMedicineClassification.PredictAllLabels(sampleData);
            ClassificationAnalytics += ($"{"中药种类",-40}{"可能性",-20}");
            ClassificationAnalytics += '\n';
            ClassificationAnalytics += ($"{"-----",-40}{"-----",-20}");
            ClassificationResult = "最有可能的种类是：" +
                sortedScoresWithLabel.FirstOrDefault(score => score.Value == sortedScoresWithLabel.Max(score => score.Value)).Key;
            foreach (var score in sortedScoresWithLabel)
            {
                ClassificationAnalytics += '\n';
                ClassificationAnalytics += ($"{score.Key,-40}{score.Value,-20}");

            }
            ContentDialog resultDialog = new ContentDialog
            {
                Title = "识别结果",
                Content = ClassificationResult + '\n' + ClassificationAnalytics,
                CloseButtonText = "关闭"
            };


            ContentDialogResult result = await resultDialog.ShowAsync();

        }

        private void ClearPicture_Click(object sender, RoutedEventArgs e)
        {
            ImageShow.Source = null;
        }
    }
}
