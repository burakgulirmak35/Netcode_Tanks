using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNetwork : NetworkBehaviour
{
    #region Değişkenler

    [Header("Transforms")]
    [SerializeField] private Transform _turret;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _myTransform;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 20f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Health")]
    [SerializeField] private Transform _canvasHealth;
    [SerializeField] private int _totalHealth = 100;
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _healthSliderEffect;
    [SerializeField] private float _healthEffectDuration = 0.5f;

    [Header("Gun")]
    [SerializeField] private float _firerate = 2f;
    private float _nextFireTime = 0f;
    [SerializeField] private float _turretRotateSpeed = 200f;
    [SerializeField] private GameObject _prefabBullet;

    [Header("Controllers")]
    private FixedJoystick _movementJoystick;
    private FixedJoystick _turretJoystick;

    [Header("Camera")]
    private CinemachineCamera cinemachineCamera;
    private Transform _mainCameraTransform;

    #endregion

    #region Unity Metotları

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _movementJoystick = ControllerManagerUI.Instance.movementJoystick;
            _turretJoystick = ControllerManagerUI.Instance.turretJoystick;
            _mainCameraTransform = Camera.main.transform;

            StartCoroutine(SetCamera());
            ControllerManagerUI.Instance.AddShootEvent(Shoot);
            ControllerManagerUI.Instance.EnableControls();
        }

        if (IsServer)
        {
            _currentHealth.Value = _totalHealth;
        }

        _currentHealth.OnValueChanged += OnHealthChanged;
        UpdateHealthBar(_currentHealth.Value, _currentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= OnHealthChanged;

        if (IsOwner && ControllerManagerUI.Instance != null)
        {
            ControllerManagerUI.Instance.RemoveShootEvent(Shoot);
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
        HandleTurretAim();
    }

    private void LateUpdate()
    {
        _canvasHealth.rotation = Quaternion.identity;
    }

    #endregion

    #region Kontrol ve Mekaniker

    private IEnumerator SetCamera()
    {
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = transform;
            cinemachineCamera.LookAt = transform;
        }
        _mainCameraTransform.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
        _mainCameraTransform.gameObject.SetActive(true);
    }

    private void HandleMovement()
    {
        float horizontalInput = _movementJoystick.Horizontal;
        float verticalInput = _movementJoystick.Vertical;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            Vector3 targetDirection = new Vector3(horizontalInput, 0f, verticalInput);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            _myTransform.rotation = Quaternion.Slerp(_myTransform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

            float moveAmount = new Vector2(horizontalInput, verticalInput).magnitude;
            moveAmount = Mathf.Clamp01(moveAmount);

            _myTransform.position += _myTransform.forward * _moveSpeed * moveAmount * Time.deltaTime;
        }
    }

    private void HandleTurretAim()
    {
        if (_turretJoystick.Horizontal != 0 || _turretJoystick.Vertical != 0)
        {
            Vector3 turretDirection = Vector3.forward * _turretJoystick.Vertical + Vector3.right * _turretJoystick.Horizontal;
            Quaternion targetRotation = Quaternion.LookRotation(turretDirection, Vector3.up);
            _turret.rotation = Quaternion.RotateTowards(_turret.rotation, targetRotation, _turretRotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateHealthBar(int newHealth, int oldHealth)
    {
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

    #endregion

    #region Network (RPC) Metotları

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

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        if (_currentHealth.Value <= 0) return;

        int newHealth = _currentHealth.Value - amount;
        _currentHealth.Value = Mathf.Max(0, newHealth);

        if (_currentHealth.Value <= 0)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
            StartRespawnClientRpc(clientRpcParams);
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBar(newValue, previousValue);
    }

    #endregion

    #region Heal

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