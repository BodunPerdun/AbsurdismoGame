using UnityEngine;

public class ForceQuit : MonoBehaviour
{
    // Цей метод викликається автоматично, коли гра закривається (Alt+F4 або Application.Quit)
    private void OnApplicationQuit()
    {
        // Тільки для ПК версії (щоб не зламати редактор Unity)
        #if !UNITY_EDITOR

                // Примусово вбиваємо поточний процес
                System.Diagnostics.Process.GetCurrentProcess().Kill();

        #endif
    }
}
