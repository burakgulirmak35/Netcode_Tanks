using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    public static NetworkManagerUI Instance { get; private set; }

    [Header("LoginPanel")]
    [SerializeField] private GameObject panelLogin;

    [Header("LoginButtons")]
    [SerializeField] private Button btnServer;
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnClient;


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

    void Start()
    {
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
        NetworkManager.Singleton.StartServer();
        onLoginEvent?.Invoke();
    }

    private void BtnHost()
    {
        NetworkManager.Singleton.StartHost();
        onLoginEvent?.Invoke();
    }

    private void BtnClient()
    {
        NetworkManager.Singleton.StartClient();
        onLoginEvent?.Invoke();
    }

    private void OnLogin()
    {
        panelLogin.gameObject.SetActive(false);
    }
}