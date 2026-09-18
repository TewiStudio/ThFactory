using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI.Views
{
    public sealed class NetworkView : ViewUI<VisualElement>
    {
        public override string ElementName => "NetworkTestControllerContainer";
        public NetworkManager networkManager;
        private Button _serverButton;
        private Button _clientButton;

        protected override void OnBind()
        {
            networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

            _serverButton = Element.Q<Button>("ServerButton");
            _clientButton = Element.Q<Button>("ClientButton");

            _serverButton.RegisterCallback<ClickEvent>(OnServerClicked);
            _clientButton.RegisterCallback<ClickEvent>(OnClientClicked);
        }

        protected override void OnUnbind()
        {
            networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;

            if (_serverButton != null)
                _serverButton.UnregisterCallback<ClickEvent>(OnServerClicked);

            if (_clientButton != null)
                _clientButton.UnregisterCallback<ClickEvent>(OnClientClicked);
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            Debug.Log("ClientManager_OnClientConnectionState: " + obj.ConnectionState);
            _clientState = obj.ConnectionState;
            if (_clientState == LocalConnectionState.Stopped)
            {
                _clientButton.text = "启动客户端";
            }
            else if (_clientState == LocalConnectionState.Starting)
            {
                _clientButton.text = "正在启动客户端";
            }
            else
            {
                _clientButton.text = "停止客户端";
            }
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            Debug.Log("ServerManager_OnServerConnectionState: " + obj.ConnectionState);
            _serverState = obj.ConnectionState;
            if (_serverState == LocalConnectionState.Stopped)
                _serverButton.text = "启动服务器";
            else
                _serverButton.text = "停止服务器";
        }

        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private void OnServerClicked(ClickEvent evt)
        {
            if (networkManager == null)
                return;

            if (_serverState != LocalConnectionState.Stopped)
                networkManager.ServerManager.StopConnection(true);
            else
                networkManager.ServerManager.StartConnection();
        }

        private void OnClientClicked(ClickEvent evt)
        {
            if (networkManager == null)
                return;

            if (_clientState != LocalConnectionState.Stopped)
            {
                networkManager.ClientManager.StopConnection();
            }
            else
            {
                networkManager.ClientManager.StartConnection();
            }
        }
    }
}