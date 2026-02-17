using Mirror;
using Unity.Cinemachine;
using UnityEngine;

public class WeaponsSwitching : NetworkBehaviour
{
    [Header("Налаштування")]
    public BaseWeapon[] weapons;
    public Animator animator;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int activeWeaponIndex = -1;

    private BaseWeapon CurrentWeapon =>
        (activeWeaponIndex >= 0 && activeWeaponIndex < weapons.Length) ? weapons[activeWeaponIndex] : null;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateWeaponVisuals(activeWeaponIndex);
    }

    void Update()
    {
        if (!isOwned) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) CmdSelectWeapon(-1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CmdSelectWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.Length > 1) CmdSelectWeapon(1);
    }

    public void FireAtTarget(Vector3 targetPoint)
    {
        BaseWeapon weapon = CurrentWeapon;
        if (weapon == null) return;

        // Отримуємо позицію дула. 
        // ВАЖЛИВО: Переконайся, що в BaseWeapon є публічне поле firePoint або transform, звідки вилітає куля.
        // Якщо в BaseWeapon немає firePoint, використовуй weapon.transform.position, але це буде менш точно.
        Transform muzzleTransform = weapon.transform;

        // Спробуй знайти firePoint у компоненті зброї, якщо він там є (псевдокод):
        // if (weapon.firePoint != null) muzzleTransform = weapon.firePoint;

        // Розраховуємо ПРАВИЛЬНИЙ напрямок: від Дула до Точки
        Vector3 direction = targetPoint - muzzleTransform.position;

        // DEBUG: Малюємо траєкторію кулі (Червона)
        Debug.DrawRay(muzzleTransform.position, direction, Color.red, 2f);

        Fire(direction); // Викликаємо твій старий метод Fire, але вже з правильним вектором
    }

    // --- ВХІД (Клієнт) ---
    public void Fire(Vector3 direction)
    {
        BaseWeapon weapon = CurrentWeapon;
        if (weapon == null)
        {
            return;
        }

        // 1. Клієнтська перевірка + Миттєві ефекти (Тряска/Звук)
        if (weapon.TryShootClient())
        {
            CmdFire(direction.normalized);
        }
    }

    // --- СЕРВЕР (Логіка) ---
    [Command]
    void CmdFire(Vector3 direction)
    {

        BaseWeapon weapon = CurrentWeapon;
        if (weapon != null)
        {
            // 1. Просимо пістолет створити кулю (вона поки існує тільки на сервері приховано)
            // Передаємо "gameObject" (себе) як власника
            GameObject bulletObj = weapon.PerformShot(direction, this.gameObject);

            // 2. Якщо куля успішно створена — спавнимо її в мережу
            if (bulletObj != null)
            {

                NetworkServer.Spawn(bulletObj);

                // 3. Звук для інших
                RpcPlaySoundForOthers(activeWeaponIndex);
            }
        }
        else
        {
            Debug.LogError("[Error] CmdFire: На сервері зброя NULL");
        }
    }

    // --- КЛІЄНТИ (Звук для інших) ---
    [ClientRpc(includeOwner = false)] // includeOwner = false, бо стрілок вже почув звук у TryShootClient
    void RpcPlaySoundForOthers(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weapons.Length)
        {
            if (weapons[weaponIndex] != null)
                weapons[weaponIndex].PlayShootSound();
        }
    }

    [Command]
    void CmdSelectWeapon(int index)
    {
        if (activeWeaponIndex == index) return;
        if (index >= -1 && index < weapons.Length)
        {
            activeWeaponIndex = index;
        }
    }

    void OnWeaponChanged(int oldIndex, int newIndex)
    {
        UpdateWeaponVisuals(newIndex);
    }

    void UpdateWeaponVisuals(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(false);
                weapons[i].SetSelectStatus(false);
            }
        }

        if (index == -1)
        {
            if (animator != null) animator.SetInteger("WeaponType", -1);
            return;
        }

        if (index >= 0 && index < weapons.Length && weapons[index] != null)
        {
            weapons[index].gameObject.SetActive(true);
            weapons[index].SetSelectStatus(true);
            if (animator != null) animator.SetInteger("WeaponType", index);

        }
    }

    public BaseWeapon GetActiveWeapon() { return CurrentWeapon; }
}