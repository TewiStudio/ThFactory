using TMPro;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine.UI;

public class TestNetworkManagerController : MonoBehaviour
{
    public NetworkManager networkManager;
    private LocalConnectionState _serverState = LocalConnectionState.Stopped;
    private LocalConnectionState _clientState = LocalConnectionState.Stopped;

    public TextMeshProUGUI serverText;
    public TextMeshProUGUI clientText;

    public Image serverButtonImage;
    public Image clientButtonImage;

    private void Start()
    {
        networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        _clientState = obj.ConnectionState;
        if (_clientState == LocalConnectionState.Stopped)
        {
            clientText.text = "启动客户端";

            serverButtonImage.color = Color.white;
            clientButtonImage.color = Color.white;
        }
        else if (_clientState == LocalConnectionState.Starting)
        {
            clientText.text = "正在启动客户端";

            serverButtonImage.color = Color.white;
            clientButtonImage.color = Color.white;
        }
        else
        {
            clientText.text = "停止客户端";

            serverButtonImage.color = new Color(1f, 1f, 1f, .5f);
            clientButtonImage.color = new Color(1f, 1f, 1f, .5f);
        }
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        _serverState = obj.ConnectionState;
        if (_serverState == LocalConnectionState.Stopped)
            serverText.text = "启动服务器";
        else
            serverText.text = "停止服务器";
    }

    public void OnClick_Server()
    {
        if (networkManager == null)
            return;

        if (_serverState != LocalConnectionState.Stopped)
            networkManager.ServerManager.StopConnection(true);
        else
            networkManager.ServerManager.StartConnection();
    }

    public void OnClick_Client()
    {
        if (networkManager == null)
            return;

        if (_clientState != LocalConnectionState.Stopped)
        {
            networkManager.ClientManager.StopConnection();
            clientText.text = "启动客户端";
        }
        else
        {
            networkManager.ClientManager.StartConnection();
            clientText.text = "停止客户端";
        }
    }
}
