using UnityEngine;

public class PlayerCollider : MonoBehaviour
{

    [SerializeField] private PlayerNetwork playerNetwork;

    public void TakeDamage(int amount)
    {
        playerNetwork.TakeDamageServerRpc(amount);
    }

}
