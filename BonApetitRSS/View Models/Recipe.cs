using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonApetitRSS.View_Models
{
    public class Recipe
    {
        public string Title { get; set; }

        public string Ingredients { get; set; }

        public string PreparationWay { get; set; }

        public string Time { get; set; }

        public string ImageURL { get; set; }
    }
}
