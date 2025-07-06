using System;
using UnityEngine;
using UnityEngine.UI;

public class ControllerManagerUI : MonoBehaviour
{
    public static ControllerManagerUI Instance { get; private set; }
    public FixedJoystick movementJoystick;
    public FixedJoystick turretJoystick;
    [SerializeField] private Button btnShoot;
    private event Action shootEvent;

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
        movementJoystick.gameObject.SetActive(false);
        turretJoystick.gameObject.SetActive(false);
        btnShoot.gameObject.SetActive(false);

        btnShoot.onClick.AddListener(BtnShoot);
    }

    public void EnableControls()
    {
        movementJoystick.gameObject.SetActive(true);
        turretJoystick.gameObject.SetActive(true);
        btnShoot.gameObject.SetActive(true);
    }

    private void BtnShoot()
    {
        shootEvent?.Invoke();
    }

    public void AddShootEvent(Action action)
    {
        shootEvent += action;
    }

    public void RemoveShootEvent(Action action)
    {
        shootEvent -= action;
    }

}
