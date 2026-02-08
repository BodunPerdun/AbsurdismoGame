using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class CustomizationManager : MonoBehaviour
{
    [Header("UI Префабы")]
    public GameObject buttonPrefab;      
    public GameObject colorButtonPrefab; 

    [Header("Контейнеры Моделей")]
    public Transform hatContainer;
    public Transform beardContainer;
    public Transform browContainer;

    [Header("Контейнеры Цветов")]
    public Transform skinColorContainer;
    public Transform hatColorContainer;   
    public Transform beardColorContainer; 
    public Transform browColorContainer;  
    public Transform pantsColorContainer;

    [Header("Рендереры Тела")]
    public Renderer bodyRenderer;
    public Renderer pantsRenderer;

    private List<Button> hatButtons = new List<Button>();
    private List<Button> beardButtons = new List<Button>();
    private List<Button> browButtons = new List<Button>();

    public void GenerateUI(GameObject[] hats, GameObject[] beards, GameObject[] brows,
                           ColorPreset[] skinColors, ColorPreset[] mainHairColors, ColorPreset[] pantsColors,
                           System.Action<string, int> onItemSelected, 
                           System.Action<Color, string> onColorSelected)
    {
        // Предметы
        CreateCategory(hats, hatContainer, "Hat", onItemSelected, hatButtons);
        CreateCategory(beards, beardContainer, "Beard", onItemSelected, beardButtons);
        CreateCategory(brows, browContainer, "Brow", onItemSelected, browButtons);

        // Цвета (используем mainHairColors для всех трёх зон головы)
        CreateColorPalette(skinColors, skinColorContainer, c => onColorSelected(c, "Skin"));
        CreateColorPalette(pantsColors, pantsColorContainer, c => onColorSelected(c, "Pants"));
        
        // ВСЕ ЭТИ ТРИ ИСПОЛЬЗУЮТ ОДИН МАССИВ mainHairColors
        CreateColorPalette(mainHairColors, hatColorContainer, c => onColorSelected(c, "HatColor"));
        CreateColorPalette(mainHairColors, beardColorContainer, c => onColorSelected(c, "BeardColor"));
        CreateColorPalette(mainHairColors, browColorContainer, c => onColorSelected(c, "BrowColor"));
    }

    private void CreateCategory(GameObject[] items, Transform container, string type, System.Action<string, int> callback, List<Button> list)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        list.Clear();
        list.Add(CreateBtn(container, "None", -1, type, callback));
        for (int i = 0; i < items.Length; i++) if (items[i]) list.Add(CreateBtn(container, items[i].name, i, type, callback));
    }

    private Button CreateBtn(Transform container, string label, int index, string type, System.Action<string, int> callback)
    {
        GameObject go = Instantiate(buttonPrefab, container);
        if (go.GetComponentInChildren<Text>()) go.GetComponentInChildren<Text>().text = label;
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => { callback(type, index); UpdateSelectionUI(type, b); });
        return b;
    }

    private void CreateColorPalette(ColorPreset[] presets, Transform container, System.Action<Color> callback)
    {
        if (container == null || presets == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        foreach (var p in presets)
        {
            GameObject go = Instantiate(colorButtonPrefab, container);
            Image img = go.GetComponent<Image>();
            if (img) img.color = p.colorValue;
            foreach (var t in go.GetComponentsInChildren<Text>()) t.enabled = false;
            go.GetComponent<Button>().onClick.AddListener(() => callback(p.colorValue));
        }
    }

    public void ApplyColor(Renderer r, Color c) 
    {
        if (r == null) return;
        // Пробиваем цвет через самые частые имена свойств шейдеров
        r.material.DOColor(c, "_BaseColor", 0.3f);
        r.material.DOColor(c, "_Color", 0.3f);
    }

    // Вспомогательный метод для поиска всех рендереров (для бровей и т.д.)
    public void ApplyToAllRenderers(GameObject obj, Color c)
    {
        if (obj == null) return;
        Renderer[] rnds = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rnds) ApplyColor(r, c);
    }

    public void UpdateSelectionUI(string type, Button clickedButton)
    {
        List<Button> targetList = type switch { "Hat" => hatButtons, "Beard" => beardButtons, "Brow" => browButtons, _ => null };
        if (targetList == null) return;
        foreach (Button btn in targetList) {
            Transform frame = btn.transform.Find("SelectionFrame");
            if (frame) frame.gameObject.SetActive(btn == clickedButton);
        }
    }

    public void HighlightButtonByIndex(string type, int index)
    {
        List<Button> targetList = type switch { "Hat" => hatButtons, "Beard" => beardButtons, "Brow" => browButtons, _ => null };
        int listIndex = index + 1; 
        if (targetList != null && listIndex >= 0 && listIndex < targetList.Count) UpdateSelectionUI(type, targetList[listIndex]);
    }
}