using Mirror;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    [Header("Налаштування")]
    public float switchCooldown = 0.5f;
    public CinemachineCamera baseCamera;

    // Список камер ЖИВИХ гравців
    private List<CinemachineCamera> _activePlayersCameras = new List<CinemachineCamera>();

    private int _spectatorIndex = 0;
    private bool _isSpectating = false;
    private float _lastSwitchTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // На старті базова камера вимкнена (або увімкнена, якщо це лобі)
        if (baseCamera) baseCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isSpectating) return;

        // Видаляємо пусті елементи (якщо хтось відключився)
        _activePlayersCameras.RemoveAll(cam => cam == null);

        // Якщо список порожній (всі померли або вийшли) -> вмикаємо огляд бази
        if (_activePlayersCameras.Count == 0)
        {
            ActivateBaseCamera();
            return;
        }

        // Керування перемиканням
        if (Time.time - _lastSwitchTime > switchCooldown)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                SwitchSpectatorTarget(1);
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SwitchSpectatorTarget(-1);
            }
        }
    }


    public void RegisterPlayersCamera(CinemachineCamera camera)
    {
        if (camera != null && !_activePlayersCameras.Contains(camera))
        {
            // === ВИПРАВЛЕННЯ ===
            // Примусово "гасимо" камеру при реєстрації.
            // Це гарантує, що коли хтось оживає, його камера не перехопить вигляд у локального гравця.
            camera.Priority = 0;
            camera.gameObject.SetActive(false);
            // ===================

            _activePlayersCameras.Add(camera);

            // (Опціонально) Якщо ми зараз в режимі спостерігача і дивимось на "Базу", 
            // можна автоматично переключитися на цього гравця, що ожив.
            if (_isSpectating && _activePlayersCameras.Count == 1)
            {
                SwitchSpectatorTarget(0);
            }
        }
    }

    public void UnregisterPlayerCamera(CinemachineCamera camera)
    {
        if (_activePlayersCameras.Contains(camera))
        {
            _activePlayersCameras.Remove(camera);
        }

        // Якщо ми зараз спостерігали саме за цим гравцем, треба перемкнутися
        if (_isSpectating)
        {
            SwitchSpectatorTarget(0); // Оновити вид
        }
    }

    // === ЛОГІКА СПОСТЕРІГАЧА ===
    public void EnableSpectatorMode()
    {
        _isSpectating = true;
        SwitchSpectatorTarget(1); // Спробувати переключитись на когось
        Debug.Log("Spectator Mode: ON");
    }

    public void DisableSpectatorMode()
    {
        _isSpectating = false;

        // Вимикаємо всі чужі камери і базову
        if (baseCamera) baseCamera.gameObject.SetActive(false);
        DisableAllSpectatorCameras();

        Debug.Log("Spectator Mode: OFF");
    }

    // SpectatorManager.cs

    private void SwitchSpectatorTarget(int direction)
    {
        // 1. Якщо нікого немає - вмикаємо базу
        if (_activePlayersCameras.Count == 0)
        {
            ActivateBaseCamera();
            return;
        }

        // 2. Якщо є гравці - вимикаємо базову
        if (baseCamera) baseCamera.gameObject.SetActive(false);

        // 3. Шукаємо ЖИВОГО гравця
        // Ми робимо це в циклі, щоб пропустити всіх мертвих, якщо такі залишились у списку
        int originalIndex = _spectatorIndex;
        int attempts = 0;

        // Цикл працює поки ми не перевіримо всіх гравців у списку
        while (attempts < _activePlayersCameras.Count)
        {
            _spectatorIndex += direction;

            // Зациклення списку (по колу)
            if (_spectatorIndex >= _activePlayersCameras.Count) _spectatorIndex = 0;
            if (_spectatorIndex < 0) _spectatorIndex = _activePlayersCameras.Count - 1;

            CinemachineCamera candidateCam = _activePlayersCameras[_spectatorIndex];

            // Перевіряємо, чи камера існує і чи живий її власник
            if (candidateCam != null)
            {
                // Намагаємось отримати скрипт здоров'я з батьківського об'єкта камери
                var playerHealth = candidateCam.GetComponentInParent<PlayerHealth>();

                // Якщо скрипт знайдено і гравець НЕ мертвий - ми знайшли ціль!
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    SetCameraTarget(candidateCam);
                    return; // Виходимо, бо знайшли живого
                }
            }

            attempts++;
        }

        // 4. Якщо ми пройшли весь цикл і не знайшли жодного живого гравця (всі мертві)
        ActivateBaseCamera();
    }

    private void SetCameraTarget(CinemachineCamera target)
    {
        foreach (var camera in _activePlayersCameras)
        {
            if (camera == null) continue;

            if (camera == target)
            {
                camera.Priority = 20;
                camera.gameObject.SetActive(true);
            }
            else
            {
                camera.Priority = 0;
                camera.gameObject.SetActive(false);
            }
        }
        _lastSwitchTime = Time.time;
    }

    private void ActivateBaseCamera()
    {
        // Вмикаємо базу
        if (baseCamera)
        {
            baseCamera.gameObject.SetActive(true);
            baseCamera.Priority = 20;
        }

        // Вимикаємо всі інші (якщо раптом вони залишились висіти)
        DisableAllSpectatorCameras();
    }

    private void DisableAllSpectatorCameras()
    {
        foreach (var camera in _activePlayersCameras)
        {
            if (camera != null)
            {
                camera.Priority = 0;
                camera.gameObject.SetActive(false);
            }
        }
    }
}