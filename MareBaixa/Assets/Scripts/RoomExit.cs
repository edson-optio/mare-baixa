using UnityEngine;

namespace MareBaixa
{
    // Saída da sala: Casco vivo encosta e o diretor leva para a próxima sala (ou fecha a run).
    // Nas arenas ela fica fechada até o chefe cair (Director.ExitOpen); ao abrir, brilha e toca.
    //
    // Prefab: SpriteRenderer (sorting 10, saida, pivô nos pés) + BoxCollider2D trigger
    // (1 × 2 blocos, offset 0, 1) + RoomExit. Sem Rigidbody2D: o corpo de Casco gera o contato.
    // Preencher openSprite (saida) e closedSprite (saida-fechada); sem closedSprite ela só escurece.
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoomExit : MonoBehaviour
    {
        public bool pulse = true;
        public Sprite openSprite;
        public Sprite closedSprite;

        static readonly Color ClosedTint = new Color(.45f, .45f, .5f, 1f);

        SpriteRenderer spriteRenderer;
        bool used;
        bool wasOpen = true;

        public bool Open => Director.Instance == null || Director.Instance.ExitOpen;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (openSprite == null) openSprite = spriteRenderer.sprite;
        }

        void Start()
        {
            wasOpen = Open;
            Refresh(wasOpen);
        }

        void Update()
        {
            bool open = Open;
            if (open && !wasOpen)
            {
                Effects.Spark((Vector2)transform.position + new Vector2(0f, 1f));
                Director.Instance?.Sound?.Play(Sfx.Door);
            }
            wasOpen = open;
            Refresh(open);
        }

        // Aberta, brilho pulsando de leve para a saída ser achada de longe.
        void Refresh(bool open)
        {
            if (!open)
            {
                spriteRenderer.sprite = closedSprite != null ? closedSprite : openSprite;
                spriteRenderer.color = closedSprite != null ? Color.white : ClosedTint;
                return;
            }
            spriteRenderer.sprite = openSprite;
            if (!pulse)
            {
                spriteRenderer.color = Color.white;
                return;
            }
            float glow = .85f + Mathf.Sin(Time.time * 3f) * .15f;
            spriteRenderer.color = new Color(glow, glow, glow, 1f) + new Color(0f, .1f, .1f, 0f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (used || !Open) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead) return;
            used = true;
            Director.Instance?.OnExitReached();
        }
    }
}
