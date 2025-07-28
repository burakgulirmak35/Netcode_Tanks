using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System.Collections;

public class NetworkManagerUI : MonoBehaviour
{
    public static NetworkManagerUI Instance { get; private set; }

    [Header("LoginPanel")]
    [SerializeField] private GameObject panelLogin;

    [Header("LoginButtons")]
    [SerializeField] private Button btnServer;
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnClient;
    [SerializeField] private TMP_InputField InputIp;
    [SerializeField] private TextMeshProUGUI txtFeedback;

    [Header("Build Ayarları")]
    [SerializeField] private bool isServerBuild;
    public event Action onLoginEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (isServerBuild)
        {
            // UI kapat
            panelLogin.SetActive(false);
            BtnServer(); // otomatik başlat
            Debug.Log("Server build ile başlatıldı.");
            return;
        }

        // IP göster
        string localIP = GetLocalIPAddress();
        txtFeedback.text = $"Cihaz IP: {localIP}";

        panelLogin.gameObject.SetActive(true);
        btnServer.onClick.AddListener(BtnServer);
        btnHost.onClick.AddListener(BtnHost);
        btnClient.onClick.AddListener(BtnClient);

        AddLoginEvent(OnLogin);
    }

    public void AddLoginEvent(Action action)
    {
        onLoginEvent += action;
    }

    public void RemoveLoginEvent(Action action)
    {
        onLoginEvent -= action;
    }



    private void BtnServer()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "0.0.0.0";
        NetworkManager.Singleton.StartServer();
        onLoginEvent?.Invoke();
    }

    private void BtnHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "0.0.0.0"; // tüm cihazlardan bağlantı kabul et
        NetworkManager.Singleton.StartHost();
        onLoginEvent?.Invoke();
    }

    public void BtnClient()
    {
        string ip = InputIp.text;

        if (string.IsNullOrWhiteSpace(ip))
            ip = "127.0.0.1";

        // IP doğrulama
        if (!System.Net.IPAddress.TryParse(ip, out _))
        {
            txtFeedback.text = $"Geçersiz IP adresi: {ip}";
            return;
        }

        // IP'yi transport'a aktar
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;

        // Client başlat
        NetworkManager.Singleton.StartClient();
        txtFeedback.text = $"Bağlanılıyor: {ip}";

        // Coroutine başlat
        StartCoroutine(CheckConnectionCoroutine(5f, ip));
    }

    private IEnumerator CheckConnectionCoroutine(float timeout, string ip)
    {
        float timer = 0f;

        while (!NetworkManager.Singleton.IsConnectedClient && timer < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        if (NetworkManager.Singleton.IsConnectedClient)
        {
            txtFeedback.text = "Bağlantı başarılı!";
            onLoginEvent?.Invoke();
        }
        else
        {
            txtFeedback.text = $"Bağlantı başarısız. IP: {ip}";
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void OnLogin()
    {
        panelLogin.gameObject.SetActive(false);
    }

    // Cihazın yerel IP adresini al
    private string GetLocalIPAddress()
    {
        string localIP = "Bilinmiyor";
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("IP alınamadı: " + ex.Message);
        }
        return localIP;
    }
}