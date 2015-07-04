using BonApetitRSS.Common;
using BonApetitRSS.View_Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BonApetitRSS.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RestaurantsPage : Page
    {
        public static List<Restaurant> restaurants = new List<Restaurant>();

        public static List<AddressNode> addresses = new List<AddressNode>();
        public static List<string> titles = new List<string>();

        public static ViewModel viewModel;

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


        public RestaurantsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            viewModel = new ViewModel();

            this.DataContext = viewModel;

            this.GetAllReastaurantsInCurrentPlace();
        }

        public static async Task<string> GetCurrentPlaceName()
        {
            string AppName = "TestBla";
            Geolocator Locator = new Geolocator();
            Geoposition Position = await Locator.GetGeopositionAsync();

            HttpClient Client = new HttpClient();
            string Result = await Client.GetStringAsync(new Uri(
                string.Format("http://nominatim.openstreetmap.org/reverse?format=xml&zoom=18&lat={0}&lon={1}&application={2}", 
                Position.Coordinate.Latitude.ToString(CultureInfo.InvariantCulture), 
                Position.Coordinate.Longitude.ToString(CultureInfo.InvariantCulture), AppName)));

            XDocument ResultDocument = XDocument.Parse(Result);
            XElement AddressElement = ResultDocument.Root.Element("addressparts");

            string city = "";

            if (AddressElement.Element("city") == null && Position.Coordinate.Longitude >= 27.6 && Position.Coordinate.Longitude <= 28.6)
            {
                if (AddressElement.Element("suburb") != null)
                {
                    city = AddressElement.Element("suburb").Value;
                }
                else
                {
                    city = AddressElement.Element("town").Value;
                }
            }
            else if (AddressElement.Element("city") == null)
            {
                if (AddressElement.Element("town") != null)
                {
                    city = AddressElement.Element("town").Value;
                }
                string region = AddressElement.Element("county").Value.Substring(AddressElement.Element("county").Value.IndexOf(" ") + 1);
            }
            else
            {
                if (AddressElement.Element("county").Value.Contains("София-Град"))
                {
                    city = "София";
                }
                else
                {
                    city = AddressElement.Element("city").Value;
                }
            }

            string Country = AddressElement.Element("country").Value;

            return city;
        }

        public static string ConvertCyrillicLettersIntoLatin(string expression)
        {
            if (expression == "Велико Търново")
            {
                expression = "Veliko Tarnovo";
            }
            else if (expression == "София")
            {
                expression = "Sofia";
            }
            else
            {
                Dictionary<string, string> letters = new Dictionary<string, string>();
                letters.Add("А", "A");
                letters.Add("Б", "B");
                letters.Add("В", "V");
                letters.Add("Г", "G");
                letters.Add("Д", "D");
                letters.Add("Е", "E");
                letters.Add("Ж", "Zh");
                letters.Add("З", "Z");
                letters.Add("И", "I");
                letters.Add("Й", "I");
                letters.Add("К", "K");
                letters.Add("Л", "L");
                letters.Add("М", "M");
                letters.Add("Н", "N");
                letters.Add("О", "O");
                letters.Add("П", "P");
                letters.Add("Р", "R");
                letters.Add("У", "U");
                letters.Add("Ф", "F");
                letters.Add("Х", "H");
                letters.Add("Ц", "Tz");
                letters.Add("Ч", "Ch");
                letters.Add("Ш", "Sh");
                letters.Add("Щ", "Sht");
                letters.Add("С", "S");
                letters.Add("Т", "T");
                letters.Add("Ю", "Ju");
                letters.Add("Я", "Ya");
                letters.Add("а", "a");
                letters.Add("б", "b");
                letters.Add("в", "v");
                letters.Add("г", "g");
                letters.Add("д", "d");
                letters.Add("е", "e");
                letters.Add("ж", "zh");
                letters.Add("з", "z");
                letters.Add("и", "i");
                letters.Add("й", "i");
                letters.Add("к", "k");
                letters.Add("л", "l");
                letters.Add("м", "m");
                letters.Add("н", "n");
                letters.Add("о", "o");
                letters.Add("п", "p");
                letters.Add("р", "r");
                letters.Add("с", "s");
                letters.Add("т", "t");
                letters.Add("у", "u");
                letters.Add("ф", "f");
                letters.Add("х", "h");
                letters.Add("ц", "tz");
                letters.Add("ч", "ch");
                letters.Add("ш", "sh");
                letters.Add("щ", "sht");
                letters.Add("ъ", "y");
                letters.Add("ю", "ju");
                letters.Add("я", "ya");

                foreach (KeyValuePair<string, string> letter in letters)
                {
                    expression = expression.Replace(letter.Key, letter.Value);
                }
            }
            expression = expression.Replace(" ", "-");
            return expression;
        }

        public static bool IsConnectedToNetwork()
        {
            bool hasConnection = true;
            hasConnection = NetworkInterface.GetIsNetworkAvailable();
            return hasConnection;
        }

        public static async Task<string> GetResponseString(string url)
        {
            string recieved = "";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    Encoding eCofidication = Encoding.UTF8;
                    using (var sr = new StreamReader(responseStream, eCofidication))
                    {
                        recieved = await sr.ReadToEndAsync();
                    }
                }
            }

            return recieved;
        }

        public async void GetAllReastaurantsInCurrentPlace()
        {
            var city = await GetCurrentPlaceName();

            city = ConvertCyrillicLettersIntoLatin(city);

            string url = "http://www.restaurant.bg/restoranti/v-" + city;

            if (IsConnectedToNetwork())
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument
                {
                    OptionFixNestedTags = true,
                    OptionAutoCloseOnEnd = true
                };

                string data = await GetResponseString(url);

                htmlDoc.LoadHtml(data);

                if (htmlDoc.DocumentNode != null)
                {
                    var titleNodes = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(
                            x => x.Attributes["title"] != null && x.HasChildNodes && x.ParentNode.OriginalName == "h3");
                    var descriptions = htmlDoc.DocumentNode.DescendantsAndSelf("p").
                        Where(x => x.HasChildNodes && x.FirstChild.OriginalName == "span");

                    var resultsCount = htmlDoc.DocumentNode.Descendants("p").
                        Where(x => x.ParentNode.Name == "div" && x.ParentNode.Attributes["class"] != null && 
                            x.ParentNode.Attributes["class"].Value == "main-content-design");
                    int resultsNumber = 0;
                    foreach (var count in resultsCount)
                    {
                        resultsNumber = int.Parse(count.InnerText.Split(' ')[count.InnerText.Split(' ').Count() - 1]);
                        break;
                    }
                    int pagesNumber = resultsNumber / 10;
                    if (resultsNumber % 10 != 0)
                    {
                        pagesNumber++;
                    }

                    int counter = 0;
                    //StringBuilder address = new StringBuilder();
                    AddressNode addressNode = new AddressNode();

                    for (int j = 0; j < descriptions.Count(); j++)
                    {
                        if (counter == 0)
                        {
                            addressNode.Address = descriptions.ElementAt(j).InnerText;
                        }
                        else if (counter == 1)
                        {
                            addressNode.Phone = descriptions.ElementAt(j).InnerText;
                        }
                        else if (counter == 2 || j == descriptions.Count() - 1)
                        {
                            addressNode.Email = descriptions.ElementAt(j).InnerText;
                            addresses.Add(addressNode);
                            addressNode = new AddressNode();
                            counter = -1;
                        }
                        counter++;
                    }

                    foreach (var htmlNode in titleNodes)
                    {
                        titles.Add(htmlNode.Attributes["title"].Value.Replace("&quot;", "\"").Replace("&amp;", "&"));
                    }

                    if (pagesNumber > 1)
                    {
                        for (int i = 2; i <= 10; i++)  //for (int i = 2; i <= pagesNumber; i++)
                        {
                            url += "/page:" + i;
                            data = await GetResponseString(url);
                            htmlDoc.LoadHtml(data);

                            titleNodes = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(
                                                        x => x.Attributes["title"] != null && x.HasChildNodes && x.ParentNode.OriginalName == "h3");

                            descriptions = htmlDoc.DocumentNode.DescendantsAndSelf("p").Where(x => x.HasChildNodes && 
                                x.FirstChild.OriginalName == "span");

                            counter = 0;
                            addressNode = new AddressNode();

                            for (int j = 0; j < descriptions.Count(); j++)
                            {
                                if (counter == 0)
                                {
                                    addressNode.Address = descriptions.ElementAt(j).InnerText;
                                }
                                else if (counter == 1)
                                {
                                    addressNode.Phone = descriptions.ElementAt(j).InnerText;
                                }
                                else if (counter == 2 || j == descriptions.Count() - 1)
                                {
                                    addressNode.Email = descriptions.ElementAt(j).InnerText;
                                    addresses.Add(addressNode);
                                    addressNode = new AddressNode();
                                    counter = -1;
                                }
                                counter++;
                            }

                            foreach (var htmlNode in titleNodes)
                            {
                                titles.Add(htmlNode.Attributes["title"].Value.Replace("&quot;", "\"").Replace("&amp;", "&"));
                            }

                            url = url.Replace("/page:" + i, "");
                        }
                    }

                    for (int i = 0; i < titles.Count; i++)
                    {
                        //Console.WriteLine("{0} : {1}", titles[i], addresses[i]);
                        Restaurant rest = new Restaurant();
                        rest.Title = titles[i];
                        rest.Address = addresses[i].Address;
                        rest.Phone = addresses[i].Phone;
                        rest.Email = addresses[i].Email;
                        restaurants.Add(rest);
                    }
                }

                viewModel.Restaurants = restaurants;
            }
            else
            {
                //notify that there is no connection and can not load the restaurants
                SendNotification("No internet connection!", "Turn it on to see", "a list of restorants near you", "Images/connection.png");
            }

            viewModel.Restaurants = restaurants;

            this.gridView.ItemsSource = viewModel.Restaurants;
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
            notificationXml.LoadXml(toastXmlString);         //var toeastElement = notificationXml.GetElementsByTagName("text");
            //toeastElement[0].AppendChild(notificationXml.CreateTextNode("This is Notification Message"));
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
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

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
