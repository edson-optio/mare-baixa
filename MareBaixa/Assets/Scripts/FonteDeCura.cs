using UnityEngine;

namespace MareBaixa
{
    // Fonte de cura da bolha de ar: beber uma vez devolve a concha à resistência máxima e enche as
    // algas. Sem concha, a fonte dá uma Concha Comum cheia no chão ao lado. Depois disso ela seca.
    //
    // Prefab: raiz com SpriteRenderer (sorting 10, fonte-1, pivô nos pés) + FonteDeCura. Sem
    // collider: a interação é por distância. Preencher full (fonte-1) e dry (fonte-seca).
    [RequireComponent(typeof(SpriteRenderer))]
    public class FonteDeCura : Interactable
    {
        public Sprite full;
        public Sprite dry;

        SpriteRenderer spriteRenderer;

        public bool Used { get; private set; }

        public FonteDeCura()
        {
            radius = 1.4f;
        }

        public override string Prompt => Used ? "A fonte secou" : Controls.InteractHint + ": beber da fonte";

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (full != null) spriteRenderer.sprite = full;
        }

        void Update()
        {
            if (Used) return;
            float glow = .9f + Mathf.Sin(Time.time * 2.5f) * .1f;
            spriteRenderer.color = new Color(glow, glow, 1f, 1f);
        }

        public override void Interact(Casco casco)
        {
            var sound = Director.Instance != null ? Director.Instance.Sound : null;
            if (Used)
            {
                sound?.Play(Sfx.Error);
                return;
            }
            Used = true;
            if (casco.Shell == ShellType.Nenhuma)
            {
                // A concha nasce ao lado, do lado de Casco, para não ficar escondida atrás da fonte.
                float side = casco.transform.position.x >= transform.position.x ? 1f : -1f;
                var room = Director.Instance != null ? Director.Instance.Room : null;
                Vector2 feet = (Vector2)transform.position + new Vector2(side * .9f, 0f);
                ShellPickup.Spawn(ShellType.Comum, -1, feet, room != null ? room.transform : transform.parent);
                Effects.Spark(feet + new Vector2(0f, .4f));
            }
            else casco.SetShellHp(casco.ShellMaxHp);
            casco.SetAlgae(GameRules.MaxAlgae);
            casco.RefillAir();
            sound?.Play(Sfx.Eat);
            Effects.Spark(casco.Center);
            Effects.Spark((Vector2)transform.position + new Vector2(0f, .6f));
            spriteRenderer.color = Color.white;
            if (dry != null) spriteRenderer.sprite = dry;
        }
    }
}
