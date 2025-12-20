using UnityEngine;
using Mirror;

public class WeaponsSwitching : NetworkBehaviour
{
    [Header("Налаштування")]
    public BaseWeapon[] weapons;
    public Animator animator;

    // Початкове значення -1 означає "без зброї"
    private int activeWeaponIndex = -1;

    // Безпечна перевірка: якщо індекс -1, повертаємо null
    private BaseWeapon CurrentWeapon =>
        (activeWeaponIndex >= 0 && activeWeaponIndex < weapons.Length) ? weapons[activeWeaponIndex] : null;

    void Start()
    {
        if (!isOwned) return;

        // При старті: -1 (без зброї) або 0 (пістолет) — як ви захочете
        SelectWeapon(-1);
    }

    void Update()
    {
        if (!isOwned) return;

        // Клавіша 1 -> Сховати зброю (індекс -1)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectWeapon(-1);

        // Клавіша 2 -> Перша зброя в масиві (індекс 0)
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeapon(0);

        // Клавіша 3 -> Друга зброя в масиві (індекс 1)
        if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.Length > 1) SelectWeapon(1);
    }

    void SelectWeapon(int index)
    {
        // КРОК 1: Спочатку вимикаємо АБСОЛЮТНО ВСЮ зброю
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(false);

            weapons[i].SetSelectStatus(false);
            
        }

        // КРОК 2: Обробка режиму "Без зброї"
        if (index == -1)
        {
            activeWeaponIndex = -1;

            // Якщо є аніматор, кажемо йому перейти в стан без зброї
            if (animator != null)
            {
                // Припускаємо, що -1 в аніматорі налаштовано як "Empty/Unarmed"
                animator.SetInteger("WeaponType", -1);
            }
            return; // Виходимо з функції, бо вмикати нічого не треба
        }

        // КРОК 3: Перевірка на помилки (щоб не вийти за межі масиву)
        if (index < 0 || index >= weapons.Length) return;

        // КРОК 4: Вмикаємо потрібну зброю
        activeWeaponIndex = index;
        weapons[activeWeaponIndex].gameObject.SetActive(true);
        weapons[activeWeaponIndex].SetSelectStatus(true);

        // КРОК 5: Оновлюємо анімацію
        if (animator != null)
        {
            animator.SetInteger("WeaponType", (int) activeWeaponIndex);
        }
    }

    public BaseWeapon GetActiveWeapon(){return CurrentWeapon;}
}