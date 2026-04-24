using FishNet;
using Tewi.Game.Network;
using Tewi.Game.Factory.Authoring;
using Tewi.Game.Factory.Core;

namespace Tewi.Game.Factory.Utils
{
    public static class IDHelper
    {
        private static NetworkGameManager _networkGameManager = null;
        private static NetworkGameManager GetGameManager()
        {
            if (_networkGameManager == null)
            {
                _networkGameManager = InstanceFinder.GetInstance<NetworkGameManager>();
            }
            return _networkGameManager;

        }

        public static void GetRecipeStringID(this int id, out string stringId)
        {
            GetGameManager().resourcesDatabase.recipeDB.GetStringId(id, out stringId);
        }

        public static void GetResourceStringID(this int id, out string stringId)
        {
            GetGameManager().resourcesDatabase.resourceDB.GetStringId(id, out stringId);
        }

        public static string GetRecipeStringID(this int id)
        {
            id.GetRecipeStringID(out var stringID);
            return stringID;
        }

        public static string GetResourceStringID(this int id)
        {
            id.GetResourceStringID(out var stringID);
            return stringID;
        }

        public static RecipeSO GetRecipe(this int id)
        {
            return GetGameManager().resourcesDatabase.recipeDB.GetRecipeSO(id);
        }

        public static RecipeData GetRecipeData(this int id)
        {
            return GetGameManager().resourcesDatabase.recipeTable[id];
        }
    }
}
