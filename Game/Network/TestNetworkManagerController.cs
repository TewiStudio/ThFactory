using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestNetworkManagerController : MonoBehaviour
{
    public NetworkManager networkManager;
    private LocalConnectionState _serverState = LocalConnectionState.Stopped;
    private LocalConnectionState _clientState = LocalConnectionState.Stopped;

    public RectTransform background;
    public RectTransform customUI;

    public TMP_InputField ServerIPText;
    public TMP_InputField ServerPortText;

    public TMP_InputField ClientIPText;
    public TMP_InputField ClientPortText;

    public TextMeshProUGUI serverText;
    public TextMeshProUGUI clientText;

    public Image customButtonImage;
    public Image serverButtonImage;
    public Image clientButtonImage;

    private void Start()
    {
        GetComponent<Canvas>().enabled = true;
        networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        _clientState = obj.ConnectionState;
        if (_clientState == LocalConnectionState.Stopped)
        {
            clientText.text = "启动客户端";
            Cursor.lockState = CursorLockMode.None;
            background.gameObject.SetActive(true);

            customButtonImage.color = Color.white;
            serverButtonImage.color = Color.white;
            clientButtonImage.color = Color.white;
        }
        else if (_clientState == LocalConnectionState.Starting)
        {
            clientText.text = "正在启动客户端";
            background.gameObject.SetActive(false);
            customUI.gameObject.SetActive(false);

            customButtonImage.color = Color.white;
            serverButtonImage.color = Color.white;
            clientButtonImage.color = Color.white;
        }
        else
        {
            clientText.text = "停止客户端";

            customButtonImage.color = new Color(1f, 1f, 1f, .5f);
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

    public void OnClick_Custom()
    {
        customUI.gameObject.SetActive(!customUI.gameObject.activeSelf);
    }

    public void OnClick_CustomApply()
    {
        if (_clientState != LocalConnectionState.Stopped || _serverState != LocalConnectionState.Stopped) return;

        var transport = (Tugboat)InstanceFinder.TransportManager.Transport;
        if (transport == null) return;

        // Server Settings
        if (!string.IsNullOrEmpty(ServerPortText.text) && ushort.TryParse(ServerPortText.text, out ushort serverPort))
        {
            transport.SetPort(serverPort);
        }
        if (!string.IsNullOrEmpty(ServerIPText.text))
        {
            transport.SetServerBindAddress(ServerIPText.text, IPAddressType.IPv4);
        }

        // Client Settings
        if (!string.IsNullOrEmpty(ClientIPText.text))
        {
            transport.SetClientAddress(ClientIPText.text);
        }
        if (!string.IsNullOrEmpty(ClientPortText.text) && ushort.TryParse(ClientPortText.text, out ushort clientPort))
        {
            transport.SetPort(clientPort);
        }
    }
}
