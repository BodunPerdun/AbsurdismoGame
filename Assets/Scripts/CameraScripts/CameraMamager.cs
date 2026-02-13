using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    // Робимо список приватним, але доступним для методів
    [SerializeField] private List<CinemachineCamera> _allCameras = new List<CinemachineCamera>();

    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // === МЕТОДИ РЕЄСТРАЦІЇ ===

    // Гравець викликає це, коли з'являється
    public void RegisterCamera(CinemachineCamera cam)
    {
        if (!_allCameras.Contains(cam))
        {
            _allCameras.Add(cam);
            Debug.Log($"CameraManager: Додано камеру {cam.name}");
        }
    }

    // Гравець викликає це, коли виходить або помирає
    public void UnregisterCamera(CinemachineCamera cam)
    {
        if (_allCameras.Contains(cam))
        {
            _allCameras.Remove(cam);
        }
    }

    // === ТРЯСКА ===
    public void CameraShake(float force = 1f)
    {
        // Для Cinemachine 3.x і Impulse System список камер НЕ ПОТРІБЕН!
        // Імпульс сам знайде всі активні Listeners на сцені.
        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulse(force);
        }
    }
}