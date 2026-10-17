using UnityEngine;

namespace MareBaixa
{
    // A descida da vila para o Recife Raso: Casco escolhe a hora de mergulhar apertando E.
    //
    // Prefab: raiz com Descent (pivô nos pés, sem collider: a interação é por distância, raio 1.5).
    // SpriteRenderer opcional na raiz (sorting 10, saida); se houver, ele pulsa.
    public class Descent : Interactable
    {
        SpriteRenderer spriteRenderer;

        public Descent()
        {
            radius = 1.5f;
        }

        public override string Prompt => "E: mergulhar no " + GameRules.ReefName;

        void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (spriteRenderer == null) return;
            float glow = .85f + Mathf.Sin(Time.time * 3f) * .15f;
            spriteRenderer.color = new Color(glow, glow, glow, 1f) + new Color(0f, .1f, .1f, 0f);
        }

        public override void Interact(Casco casco) => Director.Instance?.OnExitReached();
    }
}
