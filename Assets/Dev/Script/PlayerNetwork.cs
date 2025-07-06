using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNetwork : NetworkBehaviour
{
    // Değişkenlerinizde bir sorun yok, olduğu gibi kalabilir...
    #region Değişkenler

    [Header("Transforms")]
    [SerializeField] private Transform _turret;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _myTransform;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 20f;
    [SerializeField] private float _rotationSpeed = 150f;

    [Header("Health")]
    [SerializeField] private Transform _canvasHealth;
    [SerializeField] private int _totalHealth = 100;
    private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _healthSliderEffect;


    [Header("Gun")]
    [SerializeField] private float _firerate = 2f; // Saniyede atış sayısı
    private float _nextFireTime = 0f;
    [SerializeField] private float _turretRotateSpeed = 200f;
    [SerializeField] private GameObject _prefabBullet;

    [Header("Controllers")]
    private FixedJoystick _movementJoystick;
    private FixedJoystick _turretJoystick;

    [Header("Camera")]

    private CinemachineCamera cinemachineCamera;

    #endregion

    #region Unity Metotları

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _movementJoystick = ControllerManagerUI.Instance.movementJoystick;
            _turretJoystick = ControllerManagerUI.Instance.turretJoystick;
            // Bu satır doğru, UI'daki bir butona Shoot() metodunu bağlıyorsunuz.
            SetCamera();

            ControllerManagerUI.Instance.AddShootEvent(Shoot);
            ControllerManagerUI.Instance.EnableControls();
        }

        if (IsServer)
        {
            _currentHealth.Value = _totalHealth;
        }

        _currentHealth.OnValueChanged += OnHealthChanged;
        UpdateHealthBar(_currentHealth.Value);
    }

    private void SetCamera()
    {
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        cinemachineCamera.Follow = transform;
        cinemachineCamera.LookAt = transform;
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= OnHealthChanged;

        // ÖNEMLİ: Event aboneliğini de kaldırmalısınız.
        if (IsOwner && ControllerManagerUI.Instance != null)
        {
            ControllerManagerUI.Instance.RemoveShootEvent(Shoot);
        }
    }

    void Update()
    {
        _canvasHealth.rotation = Quaternion.identity;
        if (!IsOwner) return;
        HandleMovement();
        HandleTurretAim();
    }

    #endregion

    #region Kontrol ve Mekaniker

    private void HandleMovement()
    {
        float horizontalInput = _movementJoystick.Horizontal;
        float verticalInput = _movementJoystick.Vertical;
        // Eğer joystick'te bir hareket varsa...
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

    // --- DEĞİŞİKLİK BURADA ---
    // Metodun adı ve içeriği güncellendi. Artık sadece nişan almaktan sorumlu.
    private void HandleTurretAim()
    {
        if (_turretJoystick.Horizontal != 0 || _turretJoystick.Vertical != 0)
        {
            Vector3 turretDirection = Vector3.forward * _turretJoystick.Vertical + Vector3.right * _turretJoystick.Horizontal;
            Quaternion targetRotation = Quaternion.LookRotation(turretDirection, Vector3.up);
            _turret.rotation = Quaternion.RotateTowards(_turret.rotation, targetRotation, _turretRotateSpeed * Time.deltaTime);
        }

        // --- OTOMATİK ATEŞ ETME KODU BURADAN KALDIRILDI ---
    }

    private void UpdateHealthBar(int newHealth)
    {
        // Can değerini 0 ile 1 arasında bir orana çevir (slider'ın value değeri için)
        float normalizedHealth = (float)newHealth / _totalHealth;
        _healthSlider.value = normalizedHealth;
        _healthSliderEffect.DOValue(normalizedHealth, 0.5f);
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
        int newHealth = _currentHealth.Value - amount;
        _currentHealth.Value = Mathf.Max(0, newHealth);

        if (_currentHealth.Value <= 0)
        {
            Debug.Log("Bir tank yok edildi!");
        }
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthBar(newValue);
    }

    #endregion
}