using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float speed = 20f;
    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        // Network'te spawn olduğunda çalışır
        if (!IsOwner) return; // Sadece spawn eden sahip hareket ettirsin

        rb = GetComponent<Rigidbody>();
        // Mermiye ilk hızı ver
        rb.linearVelocity = transform.forward * speed;

        // Mermiyi bir süre sonra yok et (eğer hiçbir yere çarpmazsa)
        Destroy(gameObject, 5f);
    }

    // Tetikleyici bir çarpışma olduğunda bu metot çalışır
    private void OnTriggerEnter(Collider other)
    {
        // Çarpışma kontrolünü sadece sunucu yapsın.
        // Bu, performansı artırır ve sonuçların herkes için aynı olmasını sağlar.
        if (!IsServer) return;

        // Çarptığımız objede PlayerCollider script'i var mı diye kontrol et
        PlayerCollider playerCollider = other.GetComponent<PlayerCollider>();

        if (playerCollider != null)
        {
            // Eğer varsa, o script'in TakeDamage metodunu çağırarak hasar ver.
            playerCollider.TakeDamage(damage);
        }

        // Mermi bir şeye çarptıktan sonra kendini yok etsin.
        // Sunucuda yok edildiği için tüm client'larda da yok olacaktır.
        Destroy(gameObject);
    }
}