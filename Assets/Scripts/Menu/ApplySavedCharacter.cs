using UnityEngine;

public class ApplySavedCharacter : MonoBehaviour 
{
    public GameObject[] hats, beards, brows;
    public Renderer body, pants;

    void Start() 
    {
        // Загружаем вещи
        SetItem("SelectedHat", hats);
        SetItem("SelectedBeard", beards);
        SetItem("SelectedBrow", brows);

        // Загружаем цвета
        ApplySavedColor("SkinColor", body);
        ApplySavedColor("PantsColor", pants);
    }

    void SetItem(string key, GameObject[] arr) {
        int id = PlayerPrefs.GetInt(key, -1);
        for (int i = 0; i < arr.Length; i++) arr[i].SetActive(i == id);
    }

    void ApplySavedColor(string key, Renderer r) {
        if (PlayerPrefs.HasKey(key) && ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(key), out Color c)) {
            r.material.color = c;
        }
    }
}