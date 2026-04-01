using UnityEngine;
using Unity.Collections;
using FishNet.Object;
using Tewi.Game.Network.Core;
using Tewi.Game.Network.Registry;
using Tewi.Game.Network.Authoring;
using Tewi.Game.Network.Server;

namespace Tewi.Game.Network.Simulation
{
    public class ResourcesDatabase : NetworkBehaviour
    {
        public NetworkGameManager networkGameManager;
        public ResourceDatabase resourceDB;
        public RecipeDatabase recipeDB;

        public NativeArray<RecipeData> recipeTable;
        public NativeArray<ResourceData> resourceTable;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Init();
        }

        private void Init()
        {
            InitResource();
            InitRecipe();
        }

        private void InitResource()
        {
            resourceDB.Init();

            int totalCount = resourceDB.GetTotalResourceCount();
            resourceTable = new NativeArray<ResourceData>(totalCount + 1, Allocator.Persistent);

            // SO -> Struct
            foreach (var so in resourceDB.resources)
            {
                string stringId = so.id.cachedFullID;

                int runtimeId = resourceDB.GetRuntimeId(stringId);

                if (runtimeId == 0)
                {
                    continue;
                }

                var data = new ResourceData
                {
                    id = runtimeId,
                    maxStack = so.maxStack,
                    tags = so.tags,
                };

                resourceTable[runtimeId] = data;
            }

            Debug.Log($"[ResourcesDatabase] Inited {resourceTable.Length} resources.");
        }
        private void InitRecipe()
        {
            recipeDB.Init();

            int totalCount = recipeDB.GetTotalRecipeCount();
            recipeTable = new NativeArray<RecipeData>(totalCount + 1, Allocator.Persistent);

            // SO -> Struct
            foreach (var so in recipeDB.recipes)
            {
                string recipeStringId = so.id.cachedFullID;
                int runtimeRecipeId = recipeDB.GetRuntimeId(recipeStringId);
                if (runtimeRecipeId == 0) continue;

                var data = new RecipeData
                {
                    id = runtimeRecipeId,
                    durationTicks = (ushort)Mathf.CeilToInt(so.duration * networkGameManager.simulationManager.ticksPerSecond)
                };

                if (so.inputs.Count > 0) data.in1 = ConvertToRuntimeStack(so.inputs[0]);
                if (so.inputs.Count > 1) data.in2 = ConvertToRuntimeStack(so.inputs[1]);
                if (so.inputs.Count > 2) data.in3 = ConvertToRuntimeStack(so.inputs[2]);
                if (so.inputs.Count > 3) data.in4 = ConvertToRuntimeStack(so.inputs[3]);
                if (so.inputs.Count > 4) data.in5 = ConvertToRuntimeStack(so.inputs[4]);
                if (so.inputs.Count > 5) data.in6 = ConvertToRuntimeStack(so.inputs[5]);

                if (so.outputs.Count > 0) data.out1 = ConvertToRuntimeStack(so.outputs[0]);
                if (so.outputs.Count > 1) data.out2 = ConvertToRuntimeStack(so.outputs[1]);
                if (so.outputs.Count > 2) data.out3 = ConvertToRuntimeStack(so.outputs[2]);
                if (so.outputs.Count > 3) data.out4 = ConvertToRuntimeStack(so.outputs[3]);
                if (so.outputs.Count > 4) data.out5 = ConvertToRuntimeStack(so.outputs[4]);
                if (so.outputs.Count > 5) data.out6 = ConvertToRuntimeStack(so.outputs[5]);

                recipeTable[runtimeRecipeId] = data;
            }

            Debug.Log($"[ResourcesDatabase] Inited {recipeTable.Length} recipes.");
        }

        private ResourceStack ConvertToRuntimeStack(AuthoringResourceStack authStack)
        {
            string itemStringId = authStack.id.cachedFullID;
            int runtimeItemId = resourceDB.GetRuntimeId(itemStringId);

            if (runtimeItemId == 0)
            {
                Debug.LogError($"[ResourcesDatabase.ConvertToRuntimeStack] Resource ID not found: {itemStringId}.");
            }

            return new ResourceStack
            {
                id = (ushort)runtimeItemId,
                amount = (ushort)authStack.amount
            };
        }
    }
}
