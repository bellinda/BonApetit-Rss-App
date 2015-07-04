using BonApetitRSS.Common;
using BonApetitRSS.View_Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BonApetitRSS.Pages
{
    public sealed partial class NoTimePage : Page
    {
        public static List<string> imageURLs = new List<string>();

        private const string dbName = "food9.db";

        public List<Recipe> recipes { get; set; }

        public ViewModel viewModel { get; set; }

        public NoTimePage()
        {
            this.InitializeComponent();

            viewModel = new ViewModel();

            this.DataContext = viewModel;

            LoadTheRecipes();
        }

        private async void LoadTheRecipes()
        {
            bool dbExists = await CheckDbAsync(dbName);
            if (!dbExists)
            {
                await CreateDataBaseAsync();
                await AddRecipesAsync();
            }

            SQLiteAsyncConnection dbCon = new SQLiteAsyncConnection(dbName);
            var query = dbCon.Table<Recipe>();
            recipes = await query.ToListAsync();

            viewModel.Recipes = recipes;
        }

        private async Task AddRecipesAsync()
        {
            var list = await GetAllRecepiesFromHttpRequest();

            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);
            await conn.InsertAllAsync(list);
        }

        private async Task CreateDataBaseAsync()
        {
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(dbName);
            await conn.CreateTableAsync<Recipe>();
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


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int hours = 0;
            int minutes = 0;
            if (int.TryParse(this.availableHoursTextBox.Text, out hours) == false || int.TryParse(this.availableMinutesTextBox.Text, out minutes) == false)
            {
                SendNotification("Invalid input", "No empty fields are", "possible", "/Images/input.png");
            }
            else
            {
                if (hours > 23 || hours < 0)
                {
                    SendNotification("Invalid input", "Hours should be between ", "0 and 23", "/Images/input.png");
                    this.availableHoursTextBox.Text = "";
                }
                else if (minutes < 0 || minutes > 59)
                {
                    SendNotification("Invalid input", "Minutes should be between ", "0 and 59", "/Images/input.png");
                    this.availableMinutesTextBox.Text = "";
                }
                else
                {
                    int allMinutes = hours * 60 + minutes;
                    List<Recipe> appropriateRecipes = new List<Recipe>();

                    foreach (var recipe in viewModel.Recipes)
                    {
                        if (ParseStringTime(recipe.Time) <= allMinutes)
                        {
                            appropriateRecipes.Add(recipe);
                        }
                    }
                    this.listView.ItemsSource = appropriateRecipes;
                }
            }
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

        private static int ParseStringTime(string time)
        {
            int minutes = 0;

            if (!time.Contains("час") && !time.Contains("ч."))
            {
                time = time.Replace("- ", "-");

                string[] timeParts = time.Split(' ');
                if (timeParts[0].Contains("-"))
                {
                    string[] splitedNumbers = timeParts[0].Split('-');
                    minutes = int.Parse(splitedNumbers[1].Trim().Replace("мин.", ""));
                }
                else
                {
                    minutes = int.Parse(timeParts[0].Replace("мин.", ""));
                }
            }
            else if (!time.Contains("минути") && !time.Contains("мин."))
            {
                string[] timeParts = time.Split(' ');
                minutes = int.Parse(timeParts[0].Replace("ч.", "")) * 60;
            }
            else
            {
                string[] timeParts = time.Split(' ');
                for (int i = 0; i < timeParts.Length; i++)
                {
                    if (timeParts[i].Contains("час"))
                    {
                        minutes += int.Parse(timeParts[i - 1].Replace("ч.", "")) * 60;
                    }
                    else if (timeParts[i].Contains("ч."))
                    {
                        minutes += int.Parse(timeParts[i].Replace("ч.", "")) * 60;
                    }

                    if (timeParts[i].Contains("минути"))
                    {
                        minutes += int.Parse(timeParts[i - 1].Replace("мин.", ""));
                        break;
                    }
                    else if (timeParts[i].Contains("мин."))
                    {
                        minutes += int.Parse(timeParts[i].Replace("мин.", ""));
                        break;
                    }
                }
            }

            return minutes;
        }

        public static async Task<List<Recipe>> GetAllRecepiesFromHttpRequest()
        {
            List<Recipe> recipes = new List<Recipe>();
            string url = "http://www.bonapeti.bg/recepti/";

            var htmlDoc = new HtmlAgilityPack.HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            var recHtmlDoc = new HtmlAgilityPack.HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            for (int i = 1; i <= 3; i++)
            {
                string data = await GetResponseString(url + "?page=" + i);
                htmlDoc.LoadHtml(data);

                var titles = htmlDoc.DocumentNode.DescendantsAndSelf("a").Where(x => x.Attributes["title"] != null && x.ChildNodes.Count > 1
                    && x.Attributes["class"].Value.Contains("recipe_link"));

                var imageUrls = htmlDoc.DocumentNode.Descendants("div").Where(x => x.Attributes["class"] != null 
                    && (x.Attributes["class"].Value == "recipe_container" || x.Attributes["class"].Value == "user_recipe_container"));

                Recipe recipe = new Recipe();

                foreach (var imgUrl in imageUrls)
                {
                    if(imgUrl.InnerHtml.Contains("<img "))
                    {
                        string value = imgUrl.InnerHtml.Substring(imgUrl.InnerHtml.IndexOf("src=\""), 
                            imgUrl.InnerHtml.IndexOf("\" width") - imgUrl.InnerHtml.IndexOf("src=\"")).Replace("src=\"", "");
                        imageURLs.Add(value);
                    }
                    else
                    {
                        imageURLs.Add("http://www.bonapeti.bg/images/user_nodish_big.jpg");
                    }
                }

                foreach (var title in titles)
                {
                    recipe = new Recipe();
                    recipe.Title = title.Attributes["title"].Value;
                    string recipeLink = title.Attributes["href"].Value;

                    string recData = await GetResponseString(recipeLink);
                    recHtmlDoc.LoadHtml(recData);
                    var timeNode = recHtmlDoc.DocumentNode.Descendants("div").Where(x => x.Attributes["itemprop"] 
                        != null && x.Attributes["itemprop"].Value == "cookTime"); //or SelectSingleNode("//*[@id=\"printable_area\"]/div[4]/div[1]/div[2]");
                    foreach (var time in timeNode)
                    {
                        recipe.Time = time.InnerText.Trim();
                        break;
                    }
                    var ingredients = recHtmlDoc.DocumentNode.Descendants("table").Where(x => x.Attributes["class"] != null 
                        && x.Attributes["class"].Value == "tbl_products");           //or ("//*[contains(@class,'last')]");
                    string ingredientsText = "";
                    foreach (var ingr in ingredients)
                    {
                        ingredientsText += ingr.InnerHtml.Trim();
                    }
                    recipe.Ingredients = ingredientsText.Replace("&nbsp;", "\n").
                                            Replace("\t", "").
                                            Replace("\n", "").
                                            Replace("<tr>", "").
                                            Replace("</tr>", "").
                                            Replace("<td class=\"last\">", "").
                                            Replace("<td class=\"last\" colspan=\"1\">", "").
                                            Replace("<td class=\"last\" colspan=\"2\">", "").
                                            Replace("<td class=\"last\" colspan=\"3\">", "").
                                            Replace("<td>", "").
                                            Replace("</td>", "").
                                            Replace("<br>", "\n").
                                            Replace("<span itemprop=\"ingredients\">", "").
                                            Replace("</span>", "").
                                            Replace("<b>", "\n").
                                            Replace("</b>", "").
                                            Replace("\n ", "\n").
                                            TrimEnd().TrimStart(',');

                    var preparations = recHtmlDoc.DocumentNode.Descendants("td").Where(x => x.Attributes["class"] != null 
                        && x.Attributes["class"].Value == "stepDescription");  //or ("//*[contains(@class,'stepDescription')]");
                    StringBuilder prepWay = new StringBuilder();
                    foreach (var prep in preparations)
                    {
                        prepWay.Append(prep.InnerText.Trim());
                    }
                    recipe.PreparationWay = prepWay.ToString();

                    recipes.Add(recipe);
                }
            }
            for (int j = 0; j < recipes.Count; j++)
            {
                if (j < imageURLs.Count)
                {
                    recipes[j].ImageURL = imageURLs[j];
                }
                else
                {
                    recipes[j].ImageURL = "http://www.bonapeti.bg/images/user_nodish_big.jpg";
                }
                
            }


            return recipes;
        }

        public static async Task<string> GetResponseString(string url)
        {
            string recieved = "";

            if (!IsConnectedToNetwork())
            {
                SendNotification("No internet connection!", "Turn it on to see", "a list of restorants near you", "/Images/connection.png");
            }
            else
            {
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
            }
            return recieved;
        }

        public static bool IsConnectedToNetwork()
        {
            bool hasConnection = true;
            hasConnection = NetworkInterface.GetIsNetworkAvailable();
            return hasConnection;
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = ((Recipe)e.ClickedItem).Title;
            this.Frame.Navigate(typeof(Pages.RecipeDetailsPage), item);
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
