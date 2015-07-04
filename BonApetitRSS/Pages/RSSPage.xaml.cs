using BonApetitRSS.View_Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class RSSPage : Page
    {
        private List<RssNew> news = new List<RssNew>();

        public RSSPage()
        {
            this.InitializeComponent();

            getRSSDataFromUrl("http://www.bonapeti.bg/rss.xml");
        }

        public async void getRSSDataFromUrl(string url)
        {
            XmlDocument doc1 = await XmlDocument.LoadFromUriAsync(new Uri(url));

            XmlNodeList itemNodes = doc1.GetElementsByTagName("item");
            if (itemNodes.Count > 0)
            {
                foreach (XmlElement node in itemNodes)
                {
                    IXmlNode n = node.GetElementsByTagName("title").ElementAt(0);
                    String title = n.InnerText;
                    if (title == "")
                    {
                        continue;
                    }

                    n = node.GetElementsByTagName("description").ElementAt(0);
                    string description = n.InnerText;
                    description = Regex.Replace(description, @"<[^>]+>|&nbsp;|&bdquo;|&ldquo;|\n\n", "").Trim();

                    n = node.GetElementsByTagName("link").ElementAt(0);
                    string link = n.InnerText;

                    n = node.GetElementsByTagName("enclosure").ElementAt(0);
                    string imageUrl = n.Attributes[0].InnerText;

                    n = node.GetElementsByTagName("pubDate").ElementAt(0);
                    string pubDate = n.InnerText;
                    RssNew currNew;

                    if (description.Length > 150)
                    {
                        currNew = new RssNew(title, description, link, pubDate, imageUrl, description.Substring(0, 150) + "...");
                    }
                    else
                    {
                        currNew = new RssNew(title, description, link, pubDate, imageUrl, description);
                    }
                    news.Add(currNew);
                }
            }

            this.listView.ItemsSource = news;
        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (RssNew)e.ClickedItem;
            this.titlTextBlock.Text = item.Title;
            this.timeTextBlock.Text = item.PublicationDate;
            this.newsImage.Source = new BitmapImage(new Uri(this.BaseUri, item.ImageUrl));
            this.descriptionTextBlock.Text = item.Description;
            this.linkTextBlock.Text = item.Link;

            this.detailsScrollView.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
