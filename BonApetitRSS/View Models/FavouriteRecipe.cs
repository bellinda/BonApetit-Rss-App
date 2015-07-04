using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonApetitRSS.View_Models
{
    public class FavouriteRecipe
    {
        public string Title { get; set; }

        public string Ingredients { get; set; }

        public string PreparationWay { get; set; }

        public string Time { get; set; }

        public string ImageURL { get; set; }

        public DateTime AddingDate { get; set; }

        public FavouriteRecipe()
        {

        }

        public FavouriteRecipe(Recipe recipe)
        {
            this.Time = recipe.Time;
            this.Title = recipe.Title;
            this.Ingredients = recipe.Ingredients;
            this.PreparationWay = recipe.PreparationWay;
            this.ImageURL = recipe.ImageURL;
            this.AddingDate = DateTime.Now;
        }
    }
}
