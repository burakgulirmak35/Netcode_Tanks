using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BotPlayer : NetworkBehaviour
{
    [Header("Transforms")]
    [SerializeField] private Transform _turret;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _myTransform;
    [SerializeField] private Transform _canvasHealth;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Gun")]
    [SerializeField] private float _firerate = 1.5f;
    private float _nextFireTime = 0f;
    [SerializeField] private float _turretRotateSpeed = 200f;
    [SerializeField] private GameObject _prefabBullet;

    [Header("Health")]
    [SerializeField] private int _totalHealth = 100;
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _healthSliderEffect;
    [SerializeField] private float _healthEffectDuration = 0.5f;

    [Header("Bot AI")]
    [SerializeField] private float _moveDuration = 30f;
    [SerializeField] private float _turretAimDuration = 2f;

    private Coroutine botRoutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _totalHealth;
            botRoutine = StartCoroutine(BotBehavior());
        }

        _currentHealth.OnValueChanged += OnHealthChanged;
        UpdateHealthBar(_currentHealth.Value, _currentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void LateUpdate()
    {
        if (_canvasHealth != null)
            _canvasHealth.rotation = Quaternion.identity;
    }

    #region Bot AI

    private IEnumerator BotBehavior()
    {
        while (true)
        {
            // 1. Rastgele yön seç ve yürü
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            float elapsedMove = 0f;

            while (elapsedMove < _moveDuration)
            {
                Quaternion targetRotation = Quaternion.LookRotation(randomDirection);
                _myTransform.rotation = Quaternion.Slerp(_myTransform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                _myTransform.position += _myTransform.forward * _moveSpeed * Time.deltaTime;

                elapsedMove += Time.deltaTime;
                yield return null;
            }

            // 2. Rastgele turret yönü seç ve ateş et
            Vector3 randomTurretDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            Quaternion originalTurretRotation = _turret.rotation;
            float elapsedAim = 0f;

            while (elapsedAim < _turretAimDuration)
            {
                Quaternion targetTurretRotation = Quaternion.LookRotation(randomTurretDirection);
                _turret.rotation = Quaternion.RotateTowards(_turret.rotation, targetTurretRotation, _turretRotateSpeed * Time.deltaTime);

                Shoot();

                elapsedAim += Time.deltaTime;
                yield return null;
            }

            // 3. Turret eski rotasyona dönsün
            _turret.rotation = originalTurretRotation;

            yield return null;
        }
    }

    #endregion

    #region Shoot

    private void Shoot()
    {
        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + 1f / _firerate;
            FireServerRpc(_firePoint.position, _firePoint.rotation);
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = Instantiate(_prefabBullet, position, rotation);
        bullet.GetComponent<NetworkObject>().Spawn(true);
    }

    #endregion

    #region Health

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        if (_currentHealth.Value <= 0) return;

        int newHealth = _currentHealth.Value - amount;
        _currentHealth.Value = Mathf.Max(0, newHealth);

        if (_currentHealth.Value <= 0)
        {
            StartRespawnClientRpc(new ClientRpcParams());
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBar(newValue, previousValue);
    }

    private void UpdateHealthBar(int newHealth, int oldHealth)
    {
        if (_healthSlider == null || _healthSliderEffect == null) return;

        float normalizedHealth = (float)newHealth / _totalHealth;

        if (newHealth > oldHealth)
        {
            _healthSliderEffect.value = normalizedHealth;
            _healthSlider.DOValue(normalizedHealth, _healthEffectDuration);
        }
        else
        {
            _healthSlider.value = normalizedHealth;
            _healthSliderEffect.DOValue(normalizedHealth, _healthEffectDuration);
        }
    }

    [ClientRpc]
    private void StartRespawnClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(RespawnCoroutine());
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeHealthServerRpc()
    {
        _currentHealth.Value = _totalHealth;
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);
        TakeHealthServerRpc();
    }

    #endregion
}
