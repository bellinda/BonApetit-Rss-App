using BonApetitRSS.Common;
using BonApetitRSS.View_Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BonApetitRSS.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MyRecepies : Page
    {
        private static Image photoPlaceholder;

        private string newPicturePath;

        private static MediaCapture mediacapture = new MediaCapture();

        private const string dbName = "myRecipies.db";
        private const string baseDbName = "food9.db";

        private static List<Recipe> myRecipes = new List<Recipe>();

        private static ViewModel viewModel;

        private NavigationHelper navigationHelper;
        //private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        //public ObservableDictionary DefaultViewModel
        //{
        //    get { return this.defaultViewModel; }
        //}

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public MyRecepies()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            //RegisterAudioBackgroundTask();

            viewModel = new ViewModel();

            this.DataContext = viewModel;

            this.GetMyRecipes();
        }


        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void captureButton_Click(object sender, RoutedEventArgs e)
        {
             await this.CapturePhoto();
        }

        private async void submitButton_Click(object sender, RoutedEventArgs e)
        {
            Recipe currentRecipe = new Recipe();
            currentRecipe.Title = this.titleextBox.Text;
            currentRecipe.Time = this.timeTextBox.Text;
            currentRecipe.Ingredients = this.ingredientsTextBox.Text;
            currentRecipe.PreparationWay = this.descriptionTextBox.Text;
            if (photoPlaceholder == null)
            {
                currentRecipe.ImageURL = "http://www.bonapeti.bg/images/user_nodish_big.jpg";
            }
            else
            {
                currentRecipe.ImageURL = newPicturePath;
            }            

            bool dbExists = await CheckDbAsync(dbName);
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);

            if (!dbExists)
            {
                await conn.CreateTableAsync<Recipe>();
            }
            await conn.InsertAsync(currentRecipe);

            
            SQLiteAsyncConnection baseConn = new SQLiteAsyncConnection(baseDbName);
            await baseConn.InsertAsync(currentRecipe);

            SendNotification("Database info", "The recipe was added", "ïnto your own list", "/Images/star.png");
        }

        private async void GetMyRecipes()
        {
            bool dbExists = await CheckDbAsync(dbName);
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);

            if (!dbExists)
            {
                await conn.CreateTableAsync<Recipe>();
            }
            var query = conn.Table<Recipe>();
            myRecipes = await query.ToListAsync();
            viewModel.MyRecipes = new ObservableCollection<Recipe>(myRecipes);

            this.listView.ItemsSource = viewModel.MyRecipes;
        }

        private static async Task<bool> CheckDbAsync(string dbName)
        {
            bool dbExists = true;

            try
            {
                StorageFile sf = await ApplicationData.Current.LocalFolder.GetFileAsync(dbName);
            }
            catch (Exception)
            {
                dbExists = false;
            }

            return dbExists;
        }

        private static void SendNotification(string mainMessage, string secondMessage, string thirdMessage, string imageSrc)
        {
            var notificationXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
            string toastXmlString = "<toast>"
                               + "<visual version='1'>"
                               + "<binding template='toastImageAndText04'>"
                               + "<text id='1'>" + mainMessage + "</text>"     //No internet connection!
                               + "<text id='2'>" + secondMessage + "</text>" //Turn it on to see 
                               + "<text id='3'>" + thirdMessage + "</text>"   //a list of restorants near you
                               + "<image id='1' src='" + imageSrc + "' alt='image placeholder'/>"
                               + "</binding>"
                               + "</visual>"
                               + "</toast>";
            notificationXml.LoadXml(toastXmlString);
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }



        public async Task CapturePhoto()
        {
            await mediacapture.InitializeAsync();
            //create photo encoding properties as JPEG and set the size that should be used for capturing
            var imageEncodingProperties = ImageEncodingProperties.CreateJpeg();
            imageEncodingProperties.Width = 640;
            imageEncodingProperties.Height = 480;

            //create new unique file in the pictures library and capture photo into it
            var photoStorageFile = await KnownFolders.PicturesLibrary.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);
            newPicturePath = photoStorageFile.Path;
            await mediacapture.CapturePhotoToStorageFileAsync(imageEncodingProperties, photoStorageFile);

            //show the captured picture in an <Image />
            using (IRandomAccessStream fileStream = await photoStorageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                photoPlaceholder = this.PhotoPreview;
                // Set the image source to the selected bitmap 
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.DecodePixelWidth = 600; //match the target Image.Width, not shown
                await bitmapImage.SetSourceAsync(fileStream);
                photoPlaceholder.Source = bitmapImage;
                photoPlaceholder.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async void Image_Holding(object sender, HoldingRoutedEventArgs e)
        {
            await this.CapturePhoto();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = ((Recipe)e.ClickedItem).Title;
            this.Frame.Navigate(typeof(Pages.RecipeDetailsPage), item);
        }
    }
}
