using UnityEngine;

public class BotCollider : MonoBehaviour
{

    [SerializeField] private BotPlayer playerNetwork;

    public void TakeDamage(int amount)
    {
        playerNetwork.TakeDamageServerRpc(amount);
    }

}
