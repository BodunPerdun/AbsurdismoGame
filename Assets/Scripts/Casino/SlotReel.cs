using UnityEngine;
using System.Collections;

public class SlotReel : MonoBehaviour
{
    private const float STEP_ANGLE = 72f; 
    
    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource; 
    [SerializeField] private AudioClip spinLoopSound; 
    [SerializeField] private AudioClip stopClickSound;

    [Header("Настройки")]
    [SerializeField] private float maxVolume = 0.8f; 
    [SerializeField] private float basePitch = 1.0f; 

    private Quaternion initialLocalRot;
    private Transform myTransform;

    void Awake()
    {
        myTransform = transform;
        initialLocalRot = myTransform.localRotation;
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Настройки для предотвращения задержек
        audioSource.playOnAwake = false;
        audioSource.priority = 0; // Высокий приоритет
        audioSource.spatialBlend = 0; // 2D звук (чтобы точно слышать)
    }

    public IEnumerator Spin(float duration, int finalSymbol, System.Action<int> onComplete)
    {
        float elapsed = 0;
        float totalRotation = (360f * 5f) + (finalSymbol * STEP_ANGLE);
        bool clickPlayed = false;

        if (audioSource != null && spinLoopSound != null)
        {
            audioSource.clip = spinLoopSound;
            audioSource.loop = true;
            audioSource.pitch = basePitch;
            audioSource.volume = 0;
            audioSource.Play();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Математика вращения
            float curve = 1f - Mathf.Pow(1f - t, 4f); 
            float currentAngle = curve * totalRotation;
            myTransform.localRotation = initialLocalRot * Quaternion.AngleAxis(currentAngle, Vector3.right);

            if (audioSource != null)
            {
                // Если почти конец (96%), подготавливаем почву для щелчка
                if (t > 0.80f && !clickPlayed)
                {
                    PlayFinalClick();
                    clickPlayed = true;
                }

                if (!clickPlayed)
                {
                    // Управление громкостью основного гула
                    float fadeIn = Mathf.Clamp01(t * 10f);
                    float fadeOut = Mathf.Clamp01((1f - t) * 5f);
                    audioSource.volume = maxVolume * fadeIn * fadeOut;
                }
            }
            yield return null;
        }

        // --- ГАРАНТИРОВАННАЯ ОСТАНОВКА ---
        if (!clickPlayed) PlayFinalClick();

        myTransform.localRotation = initialLocalRot * Quaternion.AngleAxis(finalSymbol * STEP_ANGLE, Vector3.right);
        onComplete?.Invoke(finalSymbol);
    }

    private void PlayFinalClick()
    {
        if (audioSource != null && stopClickSound != null)
        {
            audioSource.Stop(); // Полностью стопаем цикл кручения
            audioSource.loop = false;
            audioSource.pitch = 1.0f;
            audioSource.volume = maxVolume;
            audioSource.PlayOneShot(stopClickSound); 
            // Debug.Log("Щелчок воспроизведен на: " + gameObject.name);
        }
    }
}