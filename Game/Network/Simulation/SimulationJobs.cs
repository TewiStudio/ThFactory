using Tewi.Game.Network.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Tewi.Game.Network.Simulation
{
    [BurstCompile(CompileSynchronously = true)]
    public struct SimulationTickJob : IJobParallelFor
    {
        public NativeArray<NodeState> Nodes;

        [ReadOnly] public NativeArray<RecipeData> RecipeTable;

        public void Execute(int index)
        {
            NodeState node = Nodes[index];
            if (node.currentStatus == Status.NoPower || node.recipeId == 0) return;

            RecipeData recipe = RecipeTable[node.recipeId];

            switch (node.currentStatus)
            {
                case Status.Idle:
                    TryStartNextCraft(ref node, ref recipe);
                    break;

                case Status.Working:
                    node.progressTicks++;
                    if (node.progressTicks >= recipe.durationTicks)
                    {
                        // 生产完成，尝试放入产物
                        if (CheckOutputSpace(ref node, ref recipe))
                        {
                            ProduceOutputs(ref node, ref recipe);
                            // 尝试开启下一个生产
                            TryStartNextCraft(ref node, ref recipe);
                        }
                        else
                        {
                            // 产物放不下了，机器堵塞
                            node.currentStatus = Status.Blocked;
                        }
                    }
                    break;

                case Status.Blocked:
                    // 堵塞状态下，每帧只检查空间是否腾出来了
                    if (CheckOutputSpace(ref node, ref recipe))
                    {
                        ProduceOutputs(ref node, ref recipe);
                        TryStartNextCraft(ref node, ref recipe);
                    }
                    break;
            }

            Nodes[index] = node;
        }

        // 尝试开启生产
        private void TryStartNextCraft(ref NodeState node, ref RecipeData recipe)
        {
            if (CheckInputs(ref node, ref recipe) && CheckOutputSpace(ref node, ref recipe))
            {
                // 材料够且有空间，立刻扣除材料，进入工作状态
                ConsumeInputs(ref node, ref recipe);
                node.currentStatus = Status.Working;
                node.progressTicks = 0;
            }
            else
            {
                // 没材料了，或者产物刚好满了放不下下一个，进入待机状态
                node.currentStatus = Status.Idle;
                node.progressTicks = 0;
            }
        }

        // 检查输入是否满足配方
        private bool CheckInputs(ref NodeState node, ref RecipeData recipe)
        {
            if (recipe.in1.id != 0 && (node.in1.id != recipe.in1.id || node.in1.amount < recipe.in1.amount)) return false;
            if (recipe.in2.id != 0 && (node.in2.id != recipe.in2.id || node.in2.amount < recipe.in2.amount)) return false;
            // todo
            return true;
        }

        // 扣除材料
        private void ConsumeInputs(ref NodeState node, ref RecipeData recipe)
        {
            if (recipe.in1.id != 0)
            {
                node.in1.amount -= recipe.in1.amount;
                if (node.in1.amount == 0) node.in1.id = 0;
            }
            if (recipe.in2.id != 0)
            {
                node.in2.amount -= recipe.in2.amount;
                if (node.in2.amount == 0) node.in2.id = 0;
            }
            // todo
        }

        // 检查产出是否有空间
        private bool CheckOutputSpace(ref NodeState node, ref RecipeData recipe)
        {
            if (recipe.out1.id != 0)
            {
                if (node.out1.id != 0 && node.out1.id != recipe.out1.id) return false; // 槽位被异物占据
                if (node.out1.amount + recipe.out1.amount > 100) return false; // 放不下了
            }
            if (recipe.out2.id != 0)
            {
                if (node.out2.id != 0 && node.out2.id != recipe.out2.id) return false; // 槽位被异物占据
                if (node.out2.amount + recipe.out2.amount > 100) return false; // 放不下了
            }
            // todo
            return true;
        }

        // 放入产物
        private void ProduceOutputs(ref NodeState node, ref RecipeData recipe)
        {
            if (recipe.out1.id != 0)
            {
                node.out1.id = recipe.out1.id;
                node.out1.amount += recipe.out1.amount;
            }
            if (recipe.out2.id != 0)
            {
                node.out2.id = recipe.out2.id;
                node.out2.amount += recipe.out2.amount;
            }
            // todo
        }
    }
}
