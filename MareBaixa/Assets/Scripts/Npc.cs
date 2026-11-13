using UnityEngine;

namespace MareBaixa
{
    // Ordem igual à de Kit.portraits.
    public enum NpcKind { Buzia, Ostracia, Lagosta, Bolota }

    // Morador da vila: respira, olha para Casco quando ele chega perto e conversa com E (o diretor
    // decide se é diálogo ou loja). Bolota, medroso, treme de vez em quando.
    //
    // Prefab (um por morador): SpriteRenderer na raiz (sorting 12, pivô nos pés; vovo-buzia,
    // ostracia, dona-lagosta, bolota) + Npc com kind. Sem collider: a interação é por distância.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Npc : Interactable
    {
        public NpcKind kind;
        public string displayName;
        public bool artFacesRight = true;
        public float lookDistance = 4f;

        SpriteRenderer spriteRenderer;
        Vector3 home;
        float phase;
        float nextTremble;
        float trembleUntil;

        public Npc()
        {
            radius = 1.6f;
        }

        public override string Prompt => Controls.InteractHint + ": falar com " + displayName;

        public static string DefaultName(NpcKind kind)
        {
            switch (kind)
            {
                case NpcKind.Buzia: return "Vovó Búzia";
                case NpcKind.Ostracia: return "Ostrácia";
                case NpcKind.Lagosta: return "Dona Lagosta";
                default: return "Bolota";
            }
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (string.IsNullOrEmpty(displayName)) displayName = DefaultName(kind);
            home = transform.position;
            phase = Random.value * 10f;
            nextTremble = Time.time + Random.Range(2f, 5f);
        }

        void Update()
        {
            // Respira: estica só para cima, porque o pivô está nos pés.
            float breath = (Mathf.Sin((Time.time + phase) * 2f) + 1f) * .5f;
            transform.localScale = new Vector3(1f, 1f + breath * .03f, 1f);

            var casco = Director.Instance != null ? Director.Instance.Casco : null;
            if (casco != null && Mathf.Abs(casco.transform.position.x - home.x) <= lookDistance)
            {
                bool cascoRight = casco.transform.position.x > home.x;
                spriteRenderer.flipX = artFacesRight ? !cascoRight : cascoRight;
            }

            Vector3 offset = Vector3.zero;
            if (kind == NpcKind.Bolota)
            {
                if (Time.time >= nextTremble)
                {
                    trembleUntil = Time.time + .6f;
                    nextTremble = Time.time + Random.Range(3f, 6f);
                }
                if (Time.time < trembleUntil) offset.x = Mathf.Sin(Time.time * 80f) * .03f;
            }
            transform.position = home + offset;
        }

        public override void Interact(Casco casco) => Director.Instance?.OnTalk(this);
    }
}
