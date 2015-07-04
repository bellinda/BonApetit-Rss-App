using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonApetitRSS.View_Models
{
    public class RssNew
    {
        public string Title { get; set; }

        public string PublicationDate { get; set; }

        public string Link { get; set; }

        public string Description { get; set; }

        public string ShortDescription { get; set; }

        public string ImageUrl { get; set; }

        public RssNew(string title, string description, string link, string publicationDate, string imageUrl, string shortDescription)
        {
            this.Title = title;
            this.Description = description;
            this.ShortDescription = shortDescription;
            this.Link = link;
            this.PublicationDate = publicationDate;
            this.ImageUrl = imageUrl;
        }

        public RssNew()
        {

        }
    }
}
