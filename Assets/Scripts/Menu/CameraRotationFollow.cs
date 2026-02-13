using UnityEngine;

public class CameraRotationFollow : MonoBehaviour
{
    [Header("Настройки наклона")]
    public float tiltAmount = 2.0f;  // На какой угол отклоняться (градусы)
    public float smoothness = 5.0f;  // Плавность движения

    private Quaternion startRotation;

    void Start()
    {
        // Запоминаем начальный поворот
        startRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // Получаем позицию мышки от -1 до 1
        float mouseX = (Input.mousePosition.x / Screen.width) - 0.5f;
        float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;

        // Рассчитываем целевой поворот (X и Y меняются местами для корректности осей)
        // Мышка вверх (Y+) -> Наклон камеры вверх (Rotation X-)
        // Мышка вправо (X+) -> Поворот камеры вправо (Rotation Y+)
        Quaternion targetRotation = startRotation * Quaternion.Euler(-mouseY * tiltAmount, mouseX * tiltAmount, 0);

        // Плавно переходим к новому повороту
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothness);
    }
}