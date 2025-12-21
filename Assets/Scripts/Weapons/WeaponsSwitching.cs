using UnityEngine;
using Mirror;

public class WeaponsSwitching : NetworkBehaviour
{
    [Header("Налаштування")]
    public BaseWeapon[] weapons;
    public Animator animator;

    [Header("Combat")]
    public Camera playerCamera;

    // [SyncVar] - головна зміна. 
    // Коли ця змінна змінюється на сервері, Mirror автоматично оновлює її у всіх клієнтів
    // і викликає метод 'OnWeaponChanged'.
    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int activeWeaponIndex = -1;

    private BaseWeapon CurrentWeapon =>
        (activeWeaponIndex >= 0 && activeWeaponIndex < weapons.Length) ? weapons[activeWeaponIndex] : null;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Примусово оновлюємо візуал при старті, щоб сховати зброю
        UpdateWeaponVisuals(activeWeaponIndex);
    }

    void Update()
    {
        // Обробка натискань тільки для власника
        if (!isOwned) return;

        // Ми більше не викликаємо SelectWeapon напряму. Ми просимо сервер змінити зброю.
        if (Input.GetKeyDown(KeyCode.Alpha1)) CmdSelectWeapon(-1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CmdSelectWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.Length > 1) CmdSelectWeapon(1);
    }

    // --- ПУБЛІЧНИЙ МЕТОД ДЛЯ СТРІЛЬБИ (викликається з MouseRotation) ---
    public void Fire(Vector3 direction)
    {
        // Тільки якщо ми маємо зброю, надсилаємо запит на постріл
        if (activeWeaponIndex != -1)
        {
            CmdFire(direction);
        }
    }

    // --- КОМАНДИ (Виконуються на Сервері) ---

    [Command]
    void CmdSelectWeapon(int index)
    {
        // Перевірка валідності індексу
        if (index >= -1 && index < weapons.Length)
        {
            // Змінюємо змінну. 
            // Оскільки це SyncVar, Mirror автоматично викличе OnWeaponChanged на всіх клієнтах!
            activeWeaponIndex = index;
        }
    }

    [Command]
    void CmdFire(Vector3 direction)
    {
        BaseWeapon weapon = CurrentWeapon;
        // Тепер сервер знає правильний індекс, тому weapon не буде null
        if (weapon != null)
        {
            weapon.TryShoot(direction);
        }
    }

    // --- ВІЗУАЛІЗАЦІЯ (Hook) ---

    // Цей метод викликається автоматично Mirror, коли змінюється activeWeaponIndex
    void OnWeaponChanged(int oldIndex, int newIndex)
    {
        UpdateWeaponVisuals(newIndex);
    }

    void UpdateWeaponVisuals(int index)
    {
        // 1. Вимикаємо ВСЮ зброю
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(false);
                weapons[i].SetSelectStatus(false);
            }
        }

        // 2. Якщо режим "без зброї"
        if (index == -1)
        {
            if (animator != null) animator.SetInteger("WeaponType", -1);
            return;
        }

        // 3. Вмикаємо нову зброю
        if (index >= 0 && index < weapons.Length)
        {
            if (weapons[index] != null)
            {
                weapons[index].gameObject.SetActive(true);
                weapons[index].SetSelectStatus(true);
            }
            if (animator != null) animator.SetInteger("WeaponType", index);
        }
    }

    public BaseWeapon GetActiveWeapon() { return CurrentWeapon; }
}