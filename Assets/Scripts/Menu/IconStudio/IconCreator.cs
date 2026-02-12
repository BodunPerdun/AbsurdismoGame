using UnityEngine;
using System.IO;

public class IconGenerator : MonoBehaviour
{
    public Camera targetCamera;
    public GameObject[] iconsObjects; // Сюда перетащи объекты прямо из сцены
    public int iconSize = 512;
    public string folderName = "GeneratedIcons";

    [ContextMenu("Generate Icons From Scene")]
    public void Generate()
    {
        // Создаем папку, если её нет
        string path = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        // Сначала выключаем все объекты на всякий случай
        foreach (GameObject obj in iconsObjects) 
        {
            if (obj != null) obj.SetActive(false);
        }

        foreach (GameObject obj in iconsObjects)
        {
            if (obj == null) continue;

            // 1. Включаем объект
            obj.SetActive(true);

            // 2. Настраиваем рендер
            RenderTexture rt = new RenderTexture(iconSize, iconSize, 24);
            targetCamera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);

            // 3. Делаем "фото"
            targetCamera.Render();

            // 4. Читаем пиксели
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, iconSize, iconSize), 0, 0);
            screenShot.Apply();

            // 5. Сохраняем PNG
            byte[] bytes = screenShot.EncodeToPNG();
            string fileName = obj.name + ".png";
            File.WriteAllBytes(Path.Combine(path, fileName), bytes);

            // 6. Выключаем объект и чистим память
            obj.SetActive(false);
            targetCamera.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(rt);
            DestroyImmediate(screenShot);

            Debug.Log($"Иконка сохранена: {fileName}");
        }
        
        // Обновляем папку Assets, чтобы иконки сразу появились в проекте
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}