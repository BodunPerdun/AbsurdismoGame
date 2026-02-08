using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class CustomizationManager : MonoBehaviour
{
    [Header("UI Префабы")]
    public GameObject buttonPrefab;      
    public GameObject colorButtonPrefab;

    [Header("Контейнеры")]
    public Transform hatContainer;
    public Transform beardContainer;
    public Transform browContainer;
    public Transform skinColorContainer;
    public Transform hairColorContainer;

    [Header("Рендерер персонажа")]
    public Renderer bodyRenderer;

    // Списки для отслеживания кнопок (чтобы управлять обводкой)
    private List<Button> hatButtons = new List<Button>();
    private List<Button> beardButtons = new List<Button>();
    private List<Button> browButtons = new List<Button>();

    public void GenerateUI(GameObject[] hats, GameObject[] beards, GameObject[] brows, 
                           ColorPreset[] skinColors, ColorPreset[] hairColors, 
                           System.Action<string, int> onItemSelected, 
                           System.Action<Color, bool> onColorSelected)
    {
        // Очистка перед генерацией
        ClearContainer(hatContainer, hatButtons);
        ClearContainer(beardContainer, beardButtons);
        ClearContainer(browContainer, browButtons);

        // Генерация
        CreateCategory(hats, hatContainer, "Hat", onItemSelected, hatButtons);
        CreateCategory(beards, beardContainer, "Beard", onItemSelected, beardButtons);
        CreateCategory(brows, browContainer, "Brow", onItemSelected, browButtons);

        CreateColorPalette(skinColors, skinColorContainer, c => onColorSelected(c, true));
        CreateColorPalette(hairColors, hairColorContainer, c => onColorSelected(c, false));
    }

    private void CreateCategory(GameObject[] items, Transform container, string type, 
                                System.Action<string, int> callback, List<Button> list)
    {
        if (container == null) return;
        
        // Кнопка "None" (индекс -1)
        list.Add(CreateBtn(container, "None", -1, type, callback));

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            list.Add(CreateBtn(container, items[i].name, i, type, callback));
        }
    }

    private Button CreateBtn(Transform container, string label, int index, string type, System.Action<string, int> callback)
    {
        GameObject go = Instantiate(buttonPrefab, container);
        go.SetActive(true);
        
        Text t = go.GetComponentInChildren<Text>();
        if (t != null) t.text = label;

        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => {
            callback(type, index);
            UpdateSelectionUI(type, b);
        });
        return b;
    }

    private void CreateColorPalette(ColorPreset[] presets, Transform container, System.Action<Color> callback)
    {
        if (container == null) return;
        foreach (var p in presets)
        {
            GameObject go = Instantiate(colorButtonPrefab, container);
            go.GetComponent<Image>().color = p.colorValue;
            go.GetComponent<Button>().onClick.AddListener(() => callback(p.colorValue));
        }
    }

    // Метод включения обводки
    public void UpdateSelectionUI(string type, Button clickedButton)
    {
        List<Button> targetList = (type == "Hat") ? hatButtons : (type == "Beard") ? beardButtons : browButtons;

        foreach (Button btn in targetList)
        {
            Transform frame = btn.transform.Find("SelectionFrame"); // Ищем объект обводки внутри кнопки
            if (frame != null) frame.gameObject.SetActive(btn == clickedButton);
        }
    }

    // Для автоматической подсветки при загрузке сохраненных данных
    public void HighlightButtonByIndex(string type, int index)
    {
        List<Button> targetList = (type == "Hat") ? hatButtons : (type == "Beard") ? beardButtons : browButtons;
        int listIndex = index + 1; // +1 потому что первой идет кнопка "None"

        if (listIndex >= 0 && listIndex < targetList.Count)
            UpdateSelectionUI(type, targetList[listIndex]);
    }

    private void ClearContainer(Transform container, List<Button> list)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
        list.Clear();
    }

    public void ApplyColor(Renderer r, Color c) 
    {
        if (r != null) r.material.DOColor(c, "_BaseColor", 0f);
    }
}