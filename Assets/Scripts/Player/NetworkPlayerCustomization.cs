using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class NetworkPlayerCustomization : NetworkBehaviour
{
    [Header("References inside Player Prefab")]
    // Сюди перетягніть батьківські об'єкти, всередині яких лежать моделі шапок/борід
    public Transform hatContainer;
    public Transform beardContainer;
    public Transform browContainer;

    [Header("Renderers")]
    public Renderer bodyRenderer;
    public Renderer pantsRenderer;
    public GameObject bodyHairModel;

    // --- SyncVars (Синхронізовані змінні) ---
    // hook викликається автоматично, коли значення змінюється
    [SyncVar(hook = nameof(OnHatChanged))] public int hatIndex = -1;
    [SyncVar(hook = nameof(OnBeardChanged))] public int beardIndex = -1;
    [SyncVar(hook = nameof(OnBrowChanged))] public int browIndex = -1;

    [SyncVar(hook = nameof(OnSkinColorChanged))] public Color skinColor = Color.white;
    [SyncVar(hook = nameof(OnPantsColorChanged))] public Color pantsColor = Color.gray;
    [SyncVar(hook = nameof(OnHatColorChanged))] public Color hatColor = Color.white;
    [SyncVar(hook = nameof(OnBeardColorChanged))] public Color beardColor = Color.black;
    [SyncVar(hook = nameof(OnBrowColorChanged))] public Color browColor = Color.black;

    // --- 1. Завантаження даних (Тільки для власника) ---
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LoadPrefsAndSendToServer();
    }

    private void LoadPrefsAndSendToServer()
    {
        int hIndex = PlayerPrefs.GetInt("SelectedHat", -1);
        int bIndex = PlayerPrefs.GetInt("SelectedBeard", -1);
        int brIndex = PlayerPrefs.GetInt("SelectedBrow", -1);

        Color sColor = LoadColor("SkinColor", Color.white);
        Color pColor = LoadColor("PantsColor", Color.gray);

        Color hColor = LoadColor("HatColorColor", Color.white);
        Color bColor = LoadColor("BeardColorColor", Color.black);
        Color brColor = LoadColor("BrowColorColor", Color.black);

        // Відправляємо дані на сервер
        CmdSyncCustomization(hIndex, bIndex, brIndex, sColor, pColor, hColor, bColor, brColor);
    }

    private Color LoadColor(string key, Color defaultColor)
    {
        if (PlayerPrefs.HasKey(key) && ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(key), out Color loaded))
            return loaded;
        return defaultColor;
    }

    // --- 2. Команда на сервер ---
    [Command]
    private void CmdSyncCustomization(int h, int b, int br, Color s, Color p, Color hc, Color bc, Color brc)
    {
        // Сервер отримує дані і оновлює SyncVars.
        // Це автоматично викличе хуки (OnHatChanged і т.д.) у всіх клієнтів.
        hatIndex = h;
        beardIndex = b;
        browIndex = br;
        skinColor = s;
        pantsColor = p;
        hatColor = hc;
        beardColor = bc;
        browColor = brc;
    }

    // --- 3. Хуки (Оновлення візуалу у всіх) ---

    // Примітка: Mirror передає старе і нове значення в хук
    void OnHatChanged(int oldIndex, int newIndex) => ToggleModel(hatContainer, newIndex);
    void OnBeardChanged(int oldIndex, int newIndex) => ToggleModel(beardContainer, newIndex);
    void OnBrowChanged(int oldIndex, int newIndex) => ToggleModel(browContainer, newIndex);

    void OnSkinColorChanged(Color oldCol, Color newCol) => ApplyColor(bodyRenderer, newCol);
    void OnPantsColorChanged(Color oldCol, Color newCol) => ApplyColor(pantsRenderer, newCol);

    void OnHatColorChanged(Color oldCol, Color newCol) => ApplyColorToContainer(hatContainer, newCol);
    void OnBeardColorChanged(Color oldCol, Color newCol)
    {
        ApplyColorToContainer(beardContainer, newCol);
        if (bodyHairModel != null) ApplyColor(bodyHairModel.GetComponent<Renderer>(), newCol);
    }
    void OnBrowColorChanged(Color oldCol, Color newCol)
    {
        ApplyColor(browContainer.GetComponent<Renderer>(), newCol);
        ApplyColorToContainer(browContainer, newCol);
    }

    // --- Допоміжні методи ---
    private void ToggleModel(Transform container, int index)
    {
        if (container == null) return;

        // Вимикаємо всі, вмикаємо потрібний
        for (int i = 0; i < container.childCount; i++)
        {
            container.GetChild(i).gameObject.SetActive(i == index);
        }
    }

    private void ApplyColor(Renderer r, Color c)
    {
        if (r == null) return;
        // Використовуємо PropertyBlock або просто material (але обережно з інстансінгом матеріалів)
        r.material.color = c;
        if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
    }

    private void ApplyColorToContainer(Transform container, Color c)
    {
        if (container == null) return;
        Renderer[] renderers = container.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) ApplyColor(r, c);
    }

    // Щоб візуал оновився у тих, хто зайшов пізніше (SyncVar ініціалізується, але хук може не викликатись автоматично при старті у деяких версіях Mirror, тому краще викликати вручну в OnStartClient)
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Примусове оновлення при підключенні до вже існуючих гравців
        OnHatChanged(-1, hatIndex);
        OnBeardChanged(-1, beardIndex);
        OnBrowChanged(-1, browIndex);

        /*
        OnSkinColorChanged(Color.white, skinColor);
        OnPantsColorChanged(Color.gray, pantsColor);
        OnHatColorChanged(Color.white, hatColor);
        OnBeardColorChanged(Color.black, beardColor);
        OnBrowColorChanged(Color.black, browColor);
        */
        Color clearColor = new Color(0, 0, 0, 0);

        OnSkinColorChanged(clearColor, skinColor);
        OnPantsColorChanged(clearColor, pantsColor);
        OnHatColorChanged(clearColor, hatColor);
        OnBeardColorChanged(clearColor, beardColor);
        OnBrowColorChanged(clearColor, browColor);
    }

    // --- НОВЕ: Команди для оновлення в реальному часі ---
    [Command]
    public void CmdUpdateItem(string type, int index)
    {
        if (type == "Hat") hatIndex = index;
        else if (type == "Beard") beardIndex = index;
        else if (type == "Brow") browIndex = index;
    }

    [Command]
    public void CmdUpdateColor(string type, Color c)
    {
        if (type == "Skin") skinColor = c;
        else if (type == "Pants") pantsColor = c;
        else if (type == "HatColor") hatColor = c;
        else if (type == "BeardColor") beardColor = c;
        else if (type == "BrowColor") browColor = c;
    }
}