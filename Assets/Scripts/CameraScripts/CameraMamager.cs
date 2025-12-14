using UnityEngine;
using Unity.Cinemachine; // Обов'язково для Cinemachine 3.x
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    // 1. Створюємо Singleton, щоб мати доступ з будь-якого місця: CameraManager.Instance
    public static CameraManager Instance { get; private set; }

    [Header("Cameras List")]
    [Tooltip("Перетягніть сюди всі віртуальні камери, які є на сцені")]
    [SerializeField] private List<CinemachineCamera> _allCameras;

    [Header("Shake Settings")]
    [Tooltip("Джерело імпульсу. Можна додати компонент CinemachineImpulseSource прямо на цей об'єкт")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        // Налаштування Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // === МЕТОД 1: Перемикання камер ===

    /// <summary>
    /// Вмикає передану камеру, вимикаючи інші (через пріоритет)
    /// </summary>
    /// <param name="targetCamera">Камера, яку треба активувати</param>
    public void SwitchToCamera(CinemachineCamera targetCamera)
    {
        foreach (var cam in _allCameras)
        {
            // Якщо це та камера, яку ми хочемо - ставимо високий пріоритет (10)
            // Всім іншим ставимо низький (0)
            cam.Priority = (cam == targetCamera) ? 10 : 0;
        }
    }

    // === МЕТОД 2: Тряска (Shake) ===

    /// <summary>
    /// Викликає тряску з певною силою
    /// </summary>
    /// <param name="force">Сила тряски (за замовчуванням 1)</param>
    public void CameraShake(float force = 1f)
    {
        if (_impulseSource != null)
        {
            // Генеруємо імпульс з заданою силою
            _impulseSource.GenerateImpulse(force);
        }
        else
        {
            Debug.LogWarning("Не призначено Impulse Source у CameraManager!");
        }
    }
}