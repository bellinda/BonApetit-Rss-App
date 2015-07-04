using BonApetitRSS.Common;
using BonApetitRSS.View_Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class FavouritesPage : Page
    {
        private const string dbName = "foodFavourite.db";

        public ViewModel viewModel { get; set; }

        public List<FavouriteRecipe> recipes = new List<FavouriteRecipe>();

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

        public FavouritesPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            viewModel = new ViewModel();

            this.DataContext = viewModel;

            LoadTheRecipes();
        }

        private async void LoadTheRecipes()
        {
            bool dbExists = await CheckDbAsync(dbName);
            if (!dbExists)
            {
                this.noRecipesInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.listView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            SQLiteAsyncConnection dbCon = new SQLiteAsyncConnection(dbName);
            var query = dbCon.Table<FavouriteRecipe>();
            recipes = await query.ToListAsync();

            viewModel.FavouriteRecipes = recipes;
            this.listView.ItemsSource = viewModel.FavouriteRecipes;
        }

        private async Task<bool> CheckDbAsync(string dbName)
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (FavouriteRecipe)e.ClickedItem;
            this.titlTextBlock.Text = item.Title;
            this.timeTextBlock.Text = "Необходимо време: " + item.Time;
            this.recipeImage.Source = new BitmapImage(new Uri(this.BaseUri, item.ImageURL));
            this.ingrediantsTextBlock.Text = item.Ingredients;
            this.preparTextBlock.Text = item.PreparationWay;

            this.detailsScrollView.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
