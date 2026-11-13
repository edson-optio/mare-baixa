using UnityEngine;

namespace MareBaixa
{
    public enum WeaponType { Pincas, Canudo, Garfo, Escova }

    public readonly struct WeaponInfo
    {
        public readonly string Name;
        public readonly string Style;
        public readonly int Damage, Price;
        public readonly float Cooldown, ActiveTime, Reach, Height, Stun;
        public readonly Color Color;

        public WeaponInfo(string name, string style, int damage, int price, float cooldown,
            float activeTime, float reach, float height, float stun, Color color)
        {
            Name = name; Style = style; Damage = damage; Price = price;
            Cooldown = cooldown; ActiveTime = activeTime; Reach = reach; Height = height; Stun = stun; Color = color;
        }
    }

    // Sucata reaproveitada: espada rápida, lança longa e porrete pesado. A pinça continua gratuita.
    public static class Weapons
    {
        static readonly WeaponInfo[] Table =
        {
            new WeaponInfo("Pinças", "curtas e rápidas", 1, 0, GameRules.AttackCooldown, GameRules.AttackActiveTime, 1.22f, .8f, 0f, GameRules.Orange),
            new WeaponInfo("Espada de Canudo", "corte rápido", 2, 5, .38f, .16f, 1.8f, .95f, 0f, GameRules.Turquoise),
            new WeaponInfo("Lança de Garfo", "alcance longo", 2, 9, .52f, .2f, 2.35f, .75f, 0f, GameRules.Pearl),
            new WeaponInfo("Martelo de Escova", "pesado · atordoa", 3, 12, .72f, .24f, 1.65f, 1.3f, .45f, GameRules.Violet),
        };

        public static bool Valid(WeaponType type) => (int)type >= 0 && (int)type < Table.Length;
        public static WeaponInfo Get(WeaponType type) => Table[Valid(type) ? (int)type : 0];
        public static Sprite Sprite(WeaponType type)
        {
            var sprites = Kit.Current != null ? Kit.Current.weaponSprites : null;
            return Valid(type) && sprites != null && (int)type < sprites.Length ? sprites[(int)type] : null;
        }
    }
}
