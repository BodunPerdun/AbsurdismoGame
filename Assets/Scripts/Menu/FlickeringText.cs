using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PulsatingLegacyText : MonoBehaviour
{
    private Text textElement;
    public float speed = 2.0f;      // Скорость пульсации
    public float minAlpha = 0.1f;   // Минимальная прозрачность (от 0 до 1)
    public float maxAlpha = 1.0f;   // Максимальная прозрачность

    void Start()
    {
        textElement = GetComponent<Text>();
    }

    void Update()
    {
        if (textElement != null)
        {
            // Используем математическую функцию синуса для создания плавных волн
            // Mathf.PingPong тоже подошел бы, но Sin дает более мягкие переходы
            float noise = Mathf.PerlinNoise(Time.time * 5f, 0) * 0.1f; // Легкое дрожание
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * speed) + 1.0f) / 2.0f) + noise;
            // Применяем новую прозрачность к цвету текста
            Color tempColor = textElement.color;
            tempColor.a = alpha;
            textElement.color = tempColor;
        }
    }
}