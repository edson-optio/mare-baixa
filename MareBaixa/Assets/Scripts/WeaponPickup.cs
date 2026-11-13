using UnityEngine;

namespace MareBaixa
{
    public class WeaponPickup : Interactable
    {
        public WeaponType Type { get; private set; }
        SpriteRenderer art;

        public override string Prompt => Controls.InteractHint + ": pegar " + Weapons.Get(Type).Name
            + " · " + Weapons.Get(Type).Damage + " dano · " + Weapons.Get(Type).Style;

        public static WeaponPickup Spawn(WeaponType type, Vector2 feet, Transform parent)
        {
            if (!Weapons.Valid(type) || type == WeaponType.Pincas) return null;
            var go = new GameObject(Weapons.Get(type).Name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var pickup = go.AddComponent<WeaponPickup>();
            pickup.Type = type;
            pickup.art = new GameObject("Sucata", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            pickup.art.transform.SetParent(go.transform, false);
            pickup.art.sprite = Weapons.Sprite(type);
            pickup.art.sortingOrder = 11;
            pickup.radius = 1.35f;
            return pickup;
        }

        void Update()
        {
            if (art == null) return;
            art.transform.localPosition = new Vector3(-.45f, .28f + Mathf.Sin(Time.time * 3f) * .055f, 0f);
            art.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        }

        public override void Interact(Casco casco)
        {
            if (casco == null || casco.Dead) return;
            if (casco.Weapon == Type) { Director.Instance?.Sound.Play(Sfx.Error); return; }
            WeaponType old = casco.Weapon;
            casco.EquipWeapon(Type);
            // Sai da lista de interações já neste quadro, antes do Destroy diferido.
            gameObject.SetActive(false);
            Destroy(gameObject);
            Spawn(old, (Vector2)casco.transform.position + Vector2.left * casco.Facing * .7f, transform.parent);
            Director.Instance?.Sound.Play(Sfx.Equip);
            Director.Instance?.Hud.Toast(Weapons.Get(Type).Name + " equipada · " + Weapons.Get(Type).Style);
            Effects.Spark(casco.Center);
        }
    }
}
