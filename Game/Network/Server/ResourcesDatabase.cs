using FishNet.Object;
using System;
using Tewi.Game.Network.Server.Recipe;
using Tewi.Game.Network.Server.ResourceDatas;
using Unity.Collections;

namespace Tewi.Game.Network.Server
{
    public class ResourcesDatabase : NetworkBehaviour
    {
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

            int maxResourceId = 0;
            foreach (var r in resourceDB.resources) maxResourceId = Math.Max(maxResourceId, r.id);
            resourceTable = new(maxResourceId + 1, Allocator.Persistent);

            // SO -> Struct
            foreach (var so in resourceDB.resources)
            {
                var data = new ResourceData
                {
                    id = so.id,
                    maxStack = so.maxStack,
                    tags = so.tags,
                };

                resourceTable[so.id] = data;
            }
        }

        private void InitRecipe()
        {
            recipeDB.Init();

            int maxRecipeId = 0;
            foreach (var r in recipeDB.recipes) maxRecipeId = Math.Max(maxRecipeId, r.id);
            recipeTable = new(maxRecipeId + 1, Allocator.Persistent);

            // SO -> Struct
            foreach (var so in recipeDB.recipes)
            {
                var data = new RecipeData
                {
                    id = so.id,
                    duration = so.duration
                };

                // 将 List 里的内容填入槽位
                if (so.inputs.Count > 0) data.input1 = so.inputs[0];
                if (so.inputs.Count > 1) data.input2 = so.inputs[1];
                if (so.inputs.Count > 2) data.input3 = so.inputs[2];
                if (so.inputs.Count > 3) data.input4 = so.inputs[3];
                if (so.inputs.Count > 4) data.input5 = so.inputs[4];
                if (so.inputs.Count > 5) data.input6 = so.inputs[5];

                if (so.outputs.Count > 0) data.output1 = so.outputs[0];
                if (so.outputs.Count > 1) data.output2 = so.outputs[1];
                if (so.outputs.Count > 2) data.output3 = so.outputs[2];
                if (so.outputs.Count > 3) data.output4 = so.outputs[3];
                if (so.outputs.Count > 4) data.output5 = so.outputs[4];
                if (so.outputs.Count > 5) data.output6 = so.outputs[5];

                recipeTable[so.id] = data;
            }
        }
    }
}
