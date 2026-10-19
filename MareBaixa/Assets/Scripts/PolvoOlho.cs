using UnityEngine;

namespace MareBaixa
{
    // Um dos dois olhos do Polvo Abissal, o alvo da luta: fechado não apanha; aberto, cada golpe
    // tira vida dele e do Polvo. Sem vida, fecha de vez. Brilha fraco no escuro (LightSource).
    //
    // Prefab: filho do Polvo com SpriteRenderer (sorting 16) + CircleCollider2D trigger (raio ~0.5)
    // + LightSource + PolvoOlho.
    //
    // Números finais: vida 6; luz 1.6 aberto, 0.7 fechado, 0 cego.
    public class PolvoOlho : MonoBehaviour, IHittable
    {
        public PolvoAbissal boss;
        public Sprite openSprite;
        public Sprite closedSprite;
        public int maxHp = 6;
        public float openLight = 1.6f;
        public float closedLight = .7f;

        public int Hp { get; private set; }
        public bool Open { get; private set; }
        public bool Blind => Hp <= 0;

        SpriteRenderer spriteRenderer;
        LightSource glow;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            glow = GetComponent<LightSource>();
            if (boss == null) boss = GetComponentInParent<PolvoAbissal>();
            Hp = maxHp;
            Refresh();
        }

        // Abre ou fecha (um olho cego fica fechado).
        public void SetOpen(bool open)
        {
            open &= !Blind;
            if (Open == open) return;
            Open = open;
            Refresh();
        }

        public void SetHp(int hp)
        {
            Hp = Mathf.Clamp(hp, 0, maxHp);
            if (Blind) Open = false;
            Refresh();
        }

        // Tira vida do olho e devolve quanto entrou de fato.
        public int TakeDamage(int damage)
        {
            int taken = Mathf.Min(damage, Hp);
            SetHp(Hp - taken);
            return taken;
        }

        public void Hit(int damage, Vector2 from)
        {
            if (boss != null) boss.HitEye(this, damage, from);
        }

        void Refresh()
        {
            if (spriteRenderer != null)
            {
                var sprite = Open ? openSprite : closedSprite;
                if (sprite != null) spriteRenderer.sprite = sprite;
            }
            if (glow != null) glow.radius = Blind ? 0f : Open ? openLight : closedLight;
        }
    }
}
