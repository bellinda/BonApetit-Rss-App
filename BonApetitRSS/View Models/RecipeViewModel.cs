using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonApetitRSS.View_Models
{
    public class RecipeViewModel : ViewModelBase
    {
        public string Title { get; set; }

        public string Ingredients { get; set; }

        public string PreparationWay { get; set; }

        public string Time { get; set; }

        public string ImageURL { get; set; }

        public RecipeViewModel(Recipe recipe)
        {
            this.Title = recipe.Title;
            this.Time = recipe.Time;
            this.PreparationWay = recipe.PreparationWay;
            this.Ingredients = recipe.Ingredients;
            this.ImageURL = recipe.ImageURL;
        }

        public Recipe ConvertIntoRecipe()
        {
            Recipe newRecipe = new Recipe();
            newRecipe.Title = this.Title;
            newRecipe.Time = this.Time;
            newRecipe.PreparationWay = this.PreparationWay;
            newRecipe.Ingredients = this.Ingredients;
            newRecipe.ImageURL = this.ImageURL;

            return newRecipe;
        }
    }
}
