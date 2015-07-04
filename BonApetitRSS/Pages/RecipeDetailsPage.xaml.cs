using BonApetitRSS.Common;
using BonApetitRSS.View_Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class RecipeDetailsPage : Page
    {
        public static RecipeViewModel viewModel;

        private const string dbName = "foodFavourite.db";

        //public static WebView myWebView;

        public static int navigationCounter = 0;

        //public static FacebookClient fb = new FacebookClient();
        public static dynamic parameters = new ExpandoObject();

        public static Image photoPlaceholder;

        public static MediaCapture mediacapture = new MediaCapture();
        private static string ExtendedPermissions = "user_status,user_photos,manage_pages";

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();


        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public RecipeDetailsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
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
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var item = await ViewModel.GetRecipeByTitle((string)e.NavigationParameter);
            viewModel = new RecipeViewModel(item);
            this.DataContext = viewModel;
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

        private async void AddToFavourites_Click(object sender, RoutedEventArgs e)
        {
            Recipe currentRecipe = (this.DataContext as RecipeViewModel).ConvertIntoRecipe();
            bool dbExists = await CheckDbAsync(dbName);
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);

            if (!dbExists)
            {
                await conn.CreateTableAsync<FavouriteRecipe>();
            }
            await conn.InsertAsync(new FavouriteRecipe(currentRecipe));
            SendNotification("Database info", "The recipe was added", "ïnto your favourites' list", "/Images/star.png");
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

        private void startSessionButton_Click(object sender, RoutedEventArgs e)
        {
            this.startSessionButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.additionalButtonsGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void readyButton_Click(object sender, RoutedEventArgs e)
        {
            this.additionalButtonsGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.takePictureButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void giveUpButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Pages.RestaurantsPage));
        }

        private async void takePictureButton_Click(object sender, RoutedEventArgs e)
        {
            await this.CapturePhoto();
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
            await mediacapture.CapturePhotoToStorageFileAsync(imageEncodingProperties, photoStorageFile);

            SendNotification("Photo captured", "check your images folder", "to view it", "Images/photo.png");

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

            this.shareOnFacebook.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //SharePhotoInFacebook(photoStorageFile.Path);
        }

        private static async void SharePhotoInFacebook(string filePath)
        {

            //parameters.client_id = "662815490501841";
            //parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";
            //parameters.response_type = "token";
            ////parametersTry.display = "popup";
            //if (!string.IsNullOrWhiteSpace(ExtendedPermissions))
            //{
            //    parameters.scope = ExtendedPermissions;
            //}
            ////fb = new FacebookClient();
            //var access_token = App.FacebookSessionClient.CurrentSession.AccessToken;
            ////var fbSessionCLient = new FacebookSessionClient(parameters.client_id);
            ////await fbSessionCLient.LoginAsync(ExtendedPermissions);
            ////fb.AccessToken = fbSessionCLient.CurrentSession.AccessToken;
            //Uri loginUrl = fb.GetLoginUrl(parameters);
            //parameters.access_token = fb.AccessToken;

            ////await fb.PostTaskAsync("me/feed?message=Trying to post something on facebook", parametersTry);


            //myWebView.Navigate(new Uri(loginUrl.AbsoluteUri, UriKind.Absolute));

            //fb.AppId = "662815490501841";
            //fb.AppSecret = "8db5f3acd5ff48acc1d436d30d8409fa";
            //dynamic loginParams = new ExpandoObject();
            //loginParams.AppId = "662815490501841";
            //loginParams.AppSecret = "8db5f3acd5ff48acc1d436d30d8409fa";
            //loginParams.redirect_url = "http://localhost/Facebook/oauth/oauth-redirect.aspx";

            //var loginUri = fb.GetLoginUrl(loginParams);
        }

        private void shareOnFacebook_Click(object sender, RoutedEventArgs e)
        {
            SharePhotoInFacebook(photoPlaceholder.Source.ToString());
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Pages.NoTimePage));
        }
    }
}
