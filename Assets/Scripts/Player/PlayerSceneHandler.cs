using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerSceneHandler : NetworkBehaviour
{
    [Header("Налаштування Сцен")]
    public string menuSceneName = "LobbyScene";
    public string gameSceneName = "SampleScene";

    [Header("Компоненти для керування")]
    public MonoBehaviour[] scriptsToControl;
    public GameObject playerCameraObject;
    public GameObject[] lobbyVisuals;
    public GameObject[] gameVisuals;

    [Header("Visibility Settings")]
    [Tooltip("Перетягни сюди всі Renderer'и тіла (SkinnedMeshRenderer або MeshRenderer), які треба сховати від себе")]
    public GameObject[] bodyRenderers;

    private AudioListener _audioListener;
    public Animator animator;

    // --- МЕРЕЖЕВА ЗМІННА ---
    // hook викликається автоматично у всіх, коли змінна змінюється на сервері
    [SyncVar(hook = nameof(OnAnimStateChanged))]
    private int currentAnimationState = 0;

    private void Awake()
    {
        if (playerCameraObject != null)
        {
            _audioListener = playerCameraObject.GetComponent<AudioListener>();
            if (_audioListener == null)
                _audioListener = playerCameraObject.GetComponentInChildren<AudioListener>();
        }
    }

    private void Start()
    {
        // Ховаємо камеру чужих гравців
        if (!isLocalPlayer)
        {
            if (playerCameraObject != null) playerCameraObject.SetActive(false);
            if (_audioListener != null) _audioListener.enabled = false;
            return;
        }

        CheckSceneState();
    }

    public override void OnStartLocalPlayer()
    {
        CheckSceneState();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Цей метод потрібен, щоб при підключенні до сервера анімація застосувалась (для клієнта)
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Викликаємо вручну, щоб синхронізувати стан при вході
        OnAnimStateChanged(0, currentAnimationState);
    }

    private void OnDestroy()
    {
        if (isLocalPlayer) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isLocalPlayer) CheckSceneState();
    }

    private void CheckSceneState()
    {
        if (!isLocalPlayer) return;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == menuSceneName)
        {
            SetupLobbyState();
        }
        else
        {
            SetupGameState();
        }
    }

    private void SetupLobbyState()
    {
        RequestAnimationChange(0); // 0 = Lobby Sit

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetScriptsEnabled(false);

        if (playerCameraObject != null) playerCameraObject.SetActive(false);
        if (_audioListener != null) _audioListener.enabled = false;

        ToggleVisuals(lobbyVisuals, true);
        ToggleVisuals(gameVisuals, false);
    }

    private void SetupGameState()
    {
        RequestAnimationChange(-1);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetScriptsEnabled(true);

        if (playerCameraObject != null)
        {
            playerCameraObject.SetActive(true);
            if (_audioListener != null) _audioListener.enabled = true;
            if (CameraManager.Instance != null)
                CameraManager.Instance.RegisterCamera(playerCameraObject.GetComponent<CinemachineCamera>());
        }

        // Для локального гравця ми хочемо сховати його тіло, але залишити тінь. Це робиться через налаштування рендерингу.
        // Проходимося по всіх мешах тіла і кажемо їм відкидати тільки тінь
        foreach (GameObject rendObject in bodyRenderers)
        {
            Renderer[] renderers = rendObject.GetComponentsInChildren<Renderer>(true);

            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    // Меш не буде малюватися в камері, але тінь від нього залишиться
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        ToggleVisuals(lobbyVisuals, false);
        ToggleVisuals(gameVisuals, true);
    }

    private void SetScriptsEnabled(bool state)
    {
        foreach (var script in scriptsToControl)
            if (script != null) script.enabled = state;
    }

    private void ToggleVisuals(GameObject[] visuals, bool state)
    {
        if (visuals == null) return;
        foreach (var obj in visuals)
            if (obj != null) obj.SetActive(state);
    }

    // --- ЛОГІКА АНІМАЦІЇ ---

    // 1. Локальний гравець просить змінити анімацію
    public void RequestAnimationChange(int animIndex)
    {
        if (isLocalPlayer)
        {
            // Якщо ми хост (сервер і клієнт в одному), міняємо відразу
            if (isServer)
            {
                currentAnimationState = animIndex;
            }
            else
            {
                // Якщо ми просто клієнт, просимо сервер
                CmdSetAnimation(animIndex);
            }
        }
    }

    // 2. Команда виконується на сервері
    [Command]
    private void CmdSetAnimation(int animIndex)
    {
        currentAnimationState = animIndex; // Зміна цієї змінної тригерить Hook у всіх
    }

    // 3. Цей метод спрацьовує У ВСІХ гравців автоматично, коли змінюється currentAnimationState
    private void OnAnimStateChanged(int oldState, int newState)
    {
        if (animator == null) return;

        // Ми не використовуємо тригери, ми одразу вмикаємо потрібний кліп.
        // 0.2f — це час плавного переходу (змішування) в секундах.

        switch (newState)
        {
            case -1: // Вимкнення всіх анімацій (при вході в гру)
                animator.SetBool("SittingState", false);
                //animator.ResetTrigger("GoToGame");
                //animator.ResetTrigger("Dance");
                //animator.ResetTrigger("Wave");

                break;
            case 0: // ЛОБІ
                Debug.Log("Включеємо анмацію сидіння");
                // "SittingState" — це назва оранжевого/сірого прямокутника в Animator Controller
                animator.SetBool("SittingState", true);
                break;

            case 1: // ГРА (IDLE/LOCOMOTION)
                // Назва вашого BlendTree або Idle стану
                animator.SetTrigger("Locomotion");
                break;

            case 2: // ТАНЕЦЬ
                animator.SetTrigger("DanceMove");
                break;

            case 3: // ХВИЛЯ
                    // Для одноразових емоцій можна залишити тригер, 
                    // або теж використати CrossFade, якщо емоція зациклена
                animator.SetTrigger("WaveAnimation");
                break;
        }
    }

    // Публічний метод, якщо захочете викликати анімацію з кнопки UI (наприклад, танець)
    public void PlayEmote(int emoteIndex)
    {
        RequestAnimationChange(emoteIndex);
    }
}