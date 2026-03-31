using TMPro;
using Tewi.Game.Player;
using Tewi.Game.Interactable;

namespace Tewi.Game.Home
{
    public class StartButton : InteractableItem
    {
        public TMP_InputField goWorldText;
        bool isPressed = true;
        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);

            //player.gameManager.goWorld = goWorldText.text.Trim();
            //player.land(isPressed);
            isPressed = !isPressed;
            //player.gameManager.StartWorld();
        }

        void Start()
        {

        }

        void Update()
        {

        }
    }
}