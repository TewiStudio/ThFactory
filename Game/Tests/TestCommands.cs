using System.Collections;
using UnityEngine;
using Tewi.Console;
using Tewi.Game.Network;

namespace Tewi.Game.Tests
{
    public class TestCommands : MonoBehaviour
    {
        public NetworkGameManager gameManager;

        [ConsoleCommand("bench_node", "Create nodes for benchmarking. default is 0(small).")]
        private void TestCommand(int arg)
        {
            Debug.Log($"Bench starting.");
            StartCoroutine(Bench(arg));
        }

        public IEnumerator Bench(int arg)
        {
            if (arg == 0)
            {
                Debug.Log($"Bench will create 10x10 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 10 10 2 1 0,0,0");

                Debug.Log($"Bench will create 20x20 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 20 20 2 1 0,2,0");

                Debug.Log($"Bench will create 30x30 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 30 30 2 1 0,4,0");

                Debug.Log($"Bench will create 40x40 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 40 40 2 1 0,6,0");

                Debug.Log($"Bench will create 50x50 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 50 50 2 1 0,8,0");
            }
            else if (arg == 1)
            {
                Debug.Log($"Bench will create 100x100 nodes 1 of 2.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 50 50 2 1 0,0,0");

                Debug.Log($"Bench will create 100x100 nodes 2 of 2.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 50 50 2 1 0,1,0");
            }
            else if (arg == 2)
            {
                Debug.Log($"Bench will create 1000x1000 nodes.");
                yield return new WaitForSeconds(1);
                gameManager.CommandProcessor.Execute("spawn_node_rect 1000 1000 2 1 0,8,0");
            }

            Debug.Log($"Bench will set all node recipes to 2.");
            yield return new WaitForSeconds(1);
            gameManager.CommandProcessor.Execute($"set_node_recipe_range 1 {gameManager.SimulationManager.NodesSnapshot.Length} 2");

            Debug.Log($"Bench will set all node recipes to 2.");
            yield return new WaitForSeconds(1);
            gameManager.CommandProcessor.Execute($"set_node_recipe_range 1 {gameManager.SimulationManager.NodesSnapshot.Length} 2");

            Debug.Log($"Bench will set all node resources in1 to 1x1000.");
            yield return new WaitForSeconds(1);
            gameManager.CommandProcessor.Execute($"set_node_res_range 1 {gameManager.SimulationManager.NodesSnapshot.Length} in1 1 1000");

            Debug.Log($"Bench will set all node resources in2 to 2x1000.");
            yield return new WaitForSeconds(1);
            gameManager.CommandProcessor.Execute($"set_node_res_range 1 {gameManager.SimulationManager.NodesSnapshot.Length} in2 2 1000");

            if (gameManager.SimulationManager.NodesSnapshot.Length != 5500)
                Debug.LogWarning($"Bench node count should be 5500, but {gameManager.SimulationManager.NodesSnapshot.Length}.");
            else
                Debug.Log("Bench node count is 5500, target 5500");
        }
    }
}
