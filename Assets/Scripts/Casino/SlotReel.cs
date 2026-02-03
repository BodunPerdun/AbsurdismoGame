using UnityEngine;
using System.Collections;

public class SlotReel : MonoBehaviour
{
    private const float STEP_ANGLE = 72f; 
    
    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource; 
    [SerializeField] private AudioClip tickSound;     

    [Header("Настройки")]
    [SerializeField] private float maxPitch = 1.4f;   
    [SerializeField] private float minPitch = 0.85f;   
    [SerializeField] private float maxVolume = 0.8f; 

    private Quaternion initialLocalRot;
    private Transform myTransform; // Кэшируем трансформ
    private float lastTickAngle = 0;

    void Awake()
    {
        // Кэшируем всё заранее
        myTransform = transform;
        initialLocalRot = myTransform.localRotation;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public IEnumerator Spin(float duration, int finalSymbol, System.Action<int> onComplete)
    {
        float elapsed = 0;
        float totalRotation = (360f * 5f) + (finalSymbol * STEP_ANGLE);
        lastTickAngle = 0;

        // Кэшируем локальные переменные для цикла, чтобы не лезть в переменные класса
        var source = audioSource;
        var hasSource = source != null;

        if (hasSource) 
        {
            source.volume = 0; 
            source.pitch = maxPitch; 
            source.Play();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Используем t * t * (3f - 2f * t) для более легкого сглаживания (SmoothStep)
            // или оставляем Pow, если нужно очень мягко
            float curve = 1f - Mathf.Pow(1f - t, 4f); 
            float currentAngle = curve * totalRotation;

            if (hasSource)
            {
                source.volume = maxVolume * Mathf.Clamp01(t * 8f) * Mathf.Clamp01((1f - t) * 4f);
                source.pitch = Mathf.Lerp(maxPitch, minPitch, t);
            }

            if (currentAngle - lastTickAngle >= STEP_ANGLE)
            {
                if (t < 0.95f) 
                {
                    if (hasSource && tickSound) source.PlayOneShot(tickSound, 0.3f);
                    lastTickAngle = currentAngle;
                }
            }

            // AngleAxis быстрее, чем Euler
            myTransform.localRotation = initialLocalRot * Quaternion.AngleAxis(currentAngle, Vector3.right);
            
            yield return null;
        }

        // Финальная фиксация
        myTransform.localRotation = initialLocalRot * Quaternion.AngleAxis(finalSymbol * STEP_ANGLE, Vector3.right);
        
        if (hasSource) 
        {
            source.Stop();
            if (tickSound) source.PlayOneShot(tickSound, 0.7f);
        }

        onComplete?.Invoke(finalSymbol);
    }
}