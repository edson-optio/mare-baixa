using UnityEngine;

namespace MareBaixa
{
    // Banco feito de madeira trazida pela maré. Não bloqueia o caminho; E/RB deita Casco e salva.
    public class BancoDescanso : Interactable
    {
        public override string Prompt => Controls.InteractHint + ": descansar e salvar no banco";
        public Vector2 Feet => transform.position;

        public static BancoDescanso Spawn(Vector2 feet, Transform parent)
        {
            var go = new GameObject("Banco de descanso", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var art = go.GetComponent<SpriteRenderer>();
            art.sprite = Kit.Current != null ? Kit.Current.benchSprite : null;
            art.sortingOrder = 10;
            var bench = go.AddComponent<BancoDescanso>();
            bench.radius = 1.7f;
            go.AddComponent<LightSource>().radius = 2f;
            return bench;
        }

        public override void Interact(Casco casco)
        {
            if (Director.Instance is RunManager run) run.RestAt(this);
        }
    }
}
