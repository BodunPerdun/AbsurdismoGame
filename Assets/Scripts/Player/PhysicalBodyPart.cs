using UnityEngine;
using Mirror; // 1. Обов'язково підключаємо простір імен Mirror

[RequireComponent(typeof(ConfigurableJoint))]
public class PhysicalBodyPart : NetworkBehaviour // 2. Змінюємо MonoBehaviour на NetworkBehaviour
{
    [SerializeField] private Transform _target;

    [Header("Нахил за камерою (для Spine / Head)")]
    [Tooltip("Перетягни сюди cameraRoot. Для рук/ніг залиш пустим!")]
    [SerializeField] private Transform _cameraRoot;

    [Tooltip("Вісь, по якій згинається кістка. Залежить від 3D моделі (Mixamo, Blender тощо). Зазвичай (1,0,0), (0,0,1) або (0,1,0)")]
    [SerializeField] private Vector3 _bendAxis = new Vector3(1, 0, 0);

    [Tooltip("Наскільки сильно згинається ця кістка. 1 = на весь кут камери.")]
    [SerializeField] private float _bendMultiplier = 1f;

    private ConfigurableJoint _joint;
    private Quaternion _initialRotation;

    // 3. Змінна, яка автоматично синхронізується від сервера до всіх клієнтів
    [SyncVar]
    private float _syncedPitch;

    void Start()
    {
        _joint = GetComponent<ConfigurableJoint>();
        _initialRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        // Беремо базову ротацію з анімації
        Quaternion finalTargetRotation = _target.localRotation;

        // Якщо ми вказали камеру (це хребет або голова), додаємо її нахил
        if (_cameraRoot != null)
        {
            float pitch = 0f;

            // 4. Перевіряємо, чи це наш локальний гравець (ми ним керуємо)
            if (isLocalPlayer)
            {
                // Читаємо реальний кут нашої камери
                pitch = _cameraRoot.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;

                // 5. ОПТИМІЗАЦІЯ: Відправляємо на сервер тільки якщо кут змінився хоча б на 1 градус.
                // FixedUpdate працює 50 разів на секунду, ми не хочемо "покласти" сервер спамом.
                if (Mathf.Abs(_syncedPitch - pitch) > 1f)
                {
                    CmdSyncPitch(pitch);
                }
            }
            else
            {
                // Для копій інших гравців на нашому екрані беремо синхронізоване значення з мережі
                pitch = _syncedPitch;
            }

            // Створюємо додаткове обертання на основі кута камери
            Quaternion cameraBend = Quaternion.AngleAxis(pitch * _bendMultiplier, _bendAxis);

            // Додаємо це обертання до анімації
            finalTargetRotation *= cameraBend;
        }

        // Застосовуємо до фізичного суглоба
        _joint.targetRotation = Quaternion.Inverse(finalTargetRotation) * _initialRotation;
    }

    // 6. Ця команда виконується на сервері. Клієнт просить сервер оновити змінну для всіх
    [Command]
    private void CmdSyncPitch(float newPitch)
    {
        _syncedPitch = newPitch;
    }
}