using UnityEngine;

namespace MareBaixa
{
    // Saída da sala: Casco vivo encosta e o diretor leva para a próxima sala (ou fecha a run).
    //
    // Prefab: SpriteRenderer (sorting 10, saida, pivô nos pés) + BoxCollider2D trigger
    // (1 × 2 blocos, offset 0, 1) + RoomExit. Sem Rigidbody2D: o corpo de Casco gera o contato.
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoomExit : MonoBehaviour
    {
        public bool pulse = true;

        SpriteRenderer spriteRenderer;
        bool used;

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        // Brilho pulsando de leve para a saída ser achada de longe.
        void Update()
        {
            if (!pulse) return;
            float glow = .85f + Mathf.Sin(Time.time * 3f) * .15f;
            spriteRenderer.color = new Color(glow, glow, glow, 1f) + new Color(0f, .1f, .1f, 0f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (used) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead) return;
            used = true;
            Director.Instance?.OnExitReached();
        }
    }
}
