using FishNet;
using System;
using System.Collections.Generic;
using System.Text;
using Tewi.Game.Network.Server;

namespace Tewi.Game.Network.Utils
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

        public static string GetRecipeStringID(this int id)
        {
            return GetGameManager().resourcesDatabase.recipeDB.GetStringId(id);
        }

        public static string GetRecipeStringID(this ushort id)
        {
            return GetGameManager().resourcesDatabase.recipeDB.GetStringId(id);
        }

        public static string GetResourceStringID(this int id)
        {
            return GetGameManager().resourcesDatabase.resourceDB.GetStringId(id);
        }

        public static string GetResourceStringID(this ushort id)
        {
            return GetGameManager().resourcesDatabase.resourceDB.GetStringId(id);
        }
    }
}
