using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class CustomizationManager : MonoBehaviour
{
    [System.Serializable]
    public struct CustomizationItem
    {
        public string itemName;
        public GameObject model;
        public Sprite icon;
    }

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

    [Header("Аудио")]
    public AudioSource clickSource; 

    private List<Button> hatButtons = new List<Button>();
    private List<Button> beardButtons = new List<Button>();
    private List<Button> browButtons = new List<Button>();

    public void GenerateUI(CustomizationItem[] hats, CustomizationItem[] beards, CustomizationItem[] brows,
                           ColorPreset[] skinColors, ColorPreset[] mainHairColors, ColorPreset[] pantsColors,
                           System.Action<string, int> onItemSelected, 
                           System.Action<Color, string> onColorSelected)
    {
        CreateCategory(hats, hatContainer, "Hat", onItemSelected, hatButtons);
        CreateCategory(beards, beardContainer, "Beard", onItemSelected, beardButtons);
        CreateCategory(brows, browContainer, "Brow", onItemSelected, browButtons);

        CreateColorPalette(skinColors, skinColorContainer, c => onColorSelected(c, "Skin"));
        CreateColorPalette(pantsColors, pantsColorContainer, c => onColorSelected(c, "Pants"));
        CreateColorPalette(mainHairColors, hatColorContainer, c => onColorSelected(c, "HatColor"));
        CreateColorPalette(mainHairColors, beardColorContainer, c => onColorSelected(c, "BeardColor"));
        CreateColorPalette(mainHairColors, browColorContainer, c => onColorSelected(c, "BrowColor"));
    }

    private void CreateCategory(CustomizationItem[] items, Transform container, string type, System.Action<string, int> callback, List<Button> list)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        list.Clear();

        // Кнопка "None"
        list.Add(CreateBtn(container, "None", -1, type, callback, null));

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].model != null)
                list.Add(CreateBtn(container, items[i].model.name, i, type, callback, items[i].icon));
        }
    }

    private Button CreateBtn(Transform container, string label, int index, string type, System.Action<string, int> callback, Sprite icon)
    {
        GameObject go = Instantiate(buttonPrefab, container);
        
        Image iconImage = go.transform.Find("Icon")?.GetComponent<Image>();
        Text textLabel = go.GetComponentInChildren<Text>();

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
            if (textLabel) textLabel.enabled = false;
        }
        else if (textLabel != null)
        {
            textLabel.text = label;
            if (iconImage) iconImage.enabled = false;
        }

        Button b = go.GetComponent<Button>();
        
        // Скрываем рамку выбора при создании
        Transform frame = go.transform.Find("SelectionFrame");
        if (frame) frame.gameObject.SetActive(false);

        b.onClick.AddListener(() => { 
            if (clickSource != null) clickSource.PlayOneShot(clickSource.clip);
            callback(type, index); 
            UpdateSelectionUI(type, b); 
        });

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
            go.GetComponent<Button>().onClick.AddListener(() => {
                if (clickSource != null) clickSource.PlayOneShot(clickSource.clip);
                callback(p.colorValue);
            });
        }
    }

    public void ApplyColor(Renderer r, Color c) 
    {
        if (r == null) return;
        r.material.DOColor(c, "_BaseColor", 0.3f);
        r.material.DOColor(c, "_Color", 0.3f);
    }

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
        int listIndex = index + 1; // +1 из-за кнопки "None"
        if (targetList != null && listIndex >= 0 && listIndex < targetList.Count) 
            UpdateSelectionUI(type, targetList[listIndex]);
    }
}