using UnityEngine;

namespace MareBaixa
{
    // Concha largada no chão: Casco troca a dele por ela com E. Guarda a resistência que tinha,
    // então uma concha rachada continua rachada quando é deixada para trás.
    //
    // Prefab: raiz com ShellPickup (pivô nos pés, sem collider: a interação é por distância) >
    // filho "Arte" com SpriteRenderer (sorting 10). Preencher art com esse filho; o sprite é
    // trocado pelo código conforme o tipo (Kit.Current.ShellSprite).
    public class ShellPickup : Interactable
    {
        public ShellType type = ShellType.Comum;
        public int hp = -1;                 // -1 = resistência cheia
        public SpriteRenderer art;

        // O desenho da concha fica ~5 px acima da base do sprite; descer a arte faz ela encostar no chão.
        const float ArtDrop = 5f / 32f;
        const float BobStep = 1f / 32f;

        float phase;

        public int HpShown => hp < 0 ? Shells.Get(type).Resistance : hp;

        public override string Prompt
        {
            get
            {
                var info = Shells.Get(type);
                return Controls.InteractHint + ": vestir " + info.Name + " (" + HpShown + "/" + info.Resistance + ")";
            }
        }

        public static ShellPickup Spawn(ShellType type, int hp, Vector2 feet, Transform parent)
        {
            var kit = Kit.Current;
            if (kit == null || kit.shellPickup == null || type == ShellType.Nenhuma) return null;
            var pickup = Instantiate(kit.shellPickup, feet, Quaternion.identity, parent);
            pickup.type = type;
            pickup.hp = hp;
            pickup.Refresh();
            return pickup;
        }

        void Awake()
        {
            if (art == null) art = GetComponentInChildren<SpriteRenderer>();
            phase = Random.value * 2f;
        }

        void Start() => Refresh();

        void Refresh()
        {
            if (art != null && Kit.Current != null) art.sprite = Kit.Current.ShellSprite(type);
        }

        // Balança 1 px para cima e para baixo, chamando atenção sem sair do lugar.
        void Update()
        {
            if (art == null || art.transform == transform) return;
            float lift = Mathf.Repeat(Time.time * .9f + phase, 1f) < .5f ? 0f : BobStep;
            art.transform.localPosition = new Vector3(0f, -ArtDrop + lift, 0f);
        }

        public override void Interact(Casco casco) => casco.SwapShell(this);
    }
}
