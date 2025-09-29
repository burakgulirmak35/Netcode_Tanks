using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BotManager : NetworkBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("Bot Ayarları")]
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    [Header("Hedef Noktaları")]
    [SerializeField] private List<Transform> points;
    private KdTree<Transform> kdTree;

    private List<Transform> availablePoints = new List<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start yerine OnNetworkSpawn kullanmak daha güvenilir olabilir
    public override void OnNetworkSpawn()
    {
        ResetAvailablePoints();
        SpawnBots();
    }

    private void SpawnBots()
    {
        // Bu kodun SADECE SERVER'da çalışmasını sağlıyoruz
        if (!IsServer) return;

        if (botPrefab == null)
        {
            Debug.LogError("Bot Prefab atanmamış!");
            return;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject botInstance = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            botInstance.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    private void ResetAvailablePoints()
    {
        availablePoints = new List<Transform>(points);
        kdTree = new KdTree<Transform>(just2D: true);
        kdTree.AddAll(availablePoints);
    }

    public Transform GetNextPoint(BotPlayer bot, Vector3 fromPos)
    {
        if (availablePoints.Count == 0)
        {
            ResetAvailablePoints();
        }

        kdTree = new KdTree<Transform>(just2D: true);
        kdTree.AddAll(availablePoints);

        Transform nearest = kdTree.FindClosest(fromPos);

        if (nearest != null)
        {
            availablePoints.Remove(nearest);
        }

        return nearest;
    }
}