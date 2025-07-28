using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float speed = 20f;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        if (!IsServer) return;
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        switch (other.tag)
        {
            case "Player":
                PlayerCollider playerCollider = other.GetComponent<PlayerCollider>();
                if (playerCollider != null)
                {
                    playerCollider.TakeDamage(damage);
                }
                Destroy(gameObject);
                break;
        }
    }
}