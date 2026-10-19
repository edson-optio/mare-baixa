using UnityEngine;

namespace MareBaixa
{
    // Tentáculo Sombrio da Fossa: fica escondido no chão (ou na parede). A cada 3–4.5 s, ou quando
    // Casco passa perto, o chão avisa (sombra, bolhas, tremor) e ele sai varrendo por 1.2 s: o
    // contato machuca e empurra. Só apanha enquanto está fora (GDD p. 8). O Polvo Abissal também
    // usa: Spawn(..., once: true) + Emerge(aviso) faz ele sair uma vez e sumir.
    //
    // A raiz gira para "cima" apontar para fora da superfície; o sprite cresce a partir da base.
    //
    // Prefab: SpriteRenderer (sorting 15, tentaculo-1, pivô nos pés) + Rigidbody2D Kinematic +
    // BoxCollider2D trigger (0.6 × 2.4, offset 0, 1.2) + TentaculoSombrio. Preencher frames
    // (tentaculo-1/2) e warning (tentaculo-aviso).
    public class TentaculoSombrio : Enemy
    {
        enum Phase { Hidden, Warning, Out }

        [Header("Arte")]
        public Sprite[] frames;
        public Sprite warning;

        [Header("Ciclo (s)")]
        public float intervalMin = 3f;
        public float intervalMax = 4.5f;
        public float warningTime = .8f;
        public float outTime = 1.2f;
        public float senseRange = 2f;
        public float senseCooldown = 1.5f;   // escondido há pelo menos isto antes de reagir a Casco

        [Header("Varrida")]
        public float sweepAngle = 35f;
        public float pushSpeed = 6f;

        Phase phase = Phase.Hidden;
        float phaseStart;
        float phaseLength;
        float baseAngle;
        float sweepSign = 1f;
        float nextSpark;
        bool once;
        Collider2D hitbox;

        public bool Out => phase == Phase.Out;
        public bool Warning => phase == Phase.Warning;
        public bool Once => once;

        protected override bool ContactHurts => phase == Phase.Out;
        // A base fica presa à superfície: apanhar não descola o tentáculo.
        protected override bool Recoils => false;

        // Valores do GDD como padrão do componente; o que estiver salvo no prefab sobrescreve.
        public TentaculoSombrio()
        {
            maxHp = 2;
            pearlsMin = 1;
            pearlsMax = 2;
        }

        // Cria um tentáculo com a base em "feet" e saindo na direção "up". Com once = true ele não
        // tem ciclo próprio: espera Emerge, sai uma vez e se destrói (não deixa pérolas).
        public static TentaculoSombrio Spawn(Vector2 feet, Vector2 up, Transform parent, bool once)
        {
            var kit = Kit.Current;
            if (kit == null || kit.tentacle == null) return null;
            if (up.sqrMagnitude < .0001f) up = Vector2.up;
            float angle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg - 90f;
            var tentacle = Instantiate(kit.tentacle, feet, Quaternion.Euler(0f, 0f, angle), parent);
            tentacle.baseAngle = angle;
            tentacle.once = once;
            if (once)
            {
                tentacle.pearlsMin = tentacle.pearlsMax = 0;
                tentacle.Hide();   // só sai com Emerge
            }
            return tentacle;
        }

        protected override void Awake()
        {
            base.Awake();
            hitbox = GetComponent<Collider2D>();
            baseAngle = transform.eulerAngles.z;
            // Tentáculos da mesma sala começam fora de sincronia.
            SetPhase(Phase.Hidden, Random.Range(.5f, intervalMax));
        }

        // Começa o aviso agora, com a duração dada (o Polvo escolhe a dele).
        public void Emerge(float warningSeconds)
        {
            if (Dead || phase != Phase.Hidden) return;
            SetPhase(Phase.Warning, Mathf.Max(0f, warningSeconds));
            Sound?.Play(Sfx.Rumble);
        }

        // Teste automático: sai já, sem aviso.
        public void ForceOut()
        {
            if (Dead) return;
            SetPhase(Phase.Out, outTime);
        }

        protected override Vector2 Steer(float deltaTime) => Home;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (Dead || Stunned) return;
            float angle = baseAngle;
            if (phase == Phase.Out)
            {
                // Vai e volta uma vez enquanto está fora.
                float t = (Time.time - phaseStart) / outTime;
                angle += Mathf.Sin(t * Mathf.PI * 2f) * sweepAngle;
                sweepSign = Mathf.Cos(t * Mathf.PI * 2f) >= 0f ? 1f : -1f;
            }
            Body.MoveRotation(angle);
        }

        protected override void Update()
        {
            float elapsed = Time.time - phaseStart;
            if (Stunned) phaseStart += Time.deltaTime;
            else if (elapsed >= phaseLength)
            {
                if (!Advance()) return;   // o de uso único acabou de se destruir
            }
            else if (phase == Phase.Hidden && !once && elapsed >= senseCooldown && CascoNear()) Emerge(warningTime);

            elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Hidden:
                    Renderer.enabled = false;
                    break;
                case Phase.Warning:
                    Renderer.enabled = true;
                    Renderer.sprite = warning;
                    transform.localScale = Vector3.one;
                    WarningEffects();
                    break;
                case Phase.Out:
                    Renderer.enabled = true;
                    Renderer.sprite = Frame(frames, 8f);
                    // Cresce rápido ao sair e encolhe no fim, sempre a partir da base.
                    float grow = Stunned ? 1f : Mathf.Clamp01(elapsed / .15f) * Mathf.Clamp01((outTime - elapsed) / .2f);
                    transform.localScale = new Vector3(1f, Mathf.Max(.05f, grow), 1f);
                    break;
            }
            base.Update();
        }

        // Passa para a próxima fase; falso se o tentáculo de uso único acabou.
        bool Advance()
        {
            switch (phase)
            {
                case Phase.Hidden:
                    Emerge(warningTime);
                    return true;
                case Phase.Warning:
                    SetPhase(Phase.Out, outTime);
                    Director.Instance?.CameraRig.Shake(.1f, .15f);
                    return true;
                default:
                    if (!once)
                    {
                        Hide();
                        return true;
                    }
                    Renderer.enabled = false;
                    if (hitbox != null) hitbox.enabled = false;
                    Destroy(gameObject);
                    return false;
            }
        }

        void Hide() => SetPhase(Phase.Hidden, once ? float.MaxValue : Random.Range(intervalMin, intervalMax));

        void SetPhase(Phase next, float length)
        {
            phase = next;
            phaseStart = Time.time;
            phaseLength = length;
            // Ligado também no aviso, para o clarão da Lâmpada achar o tentáculo e cancelar a saída
            // (no aviso o contato não machuca e o golpe não entra).
            if (hitbox != null) hitbox.enabled = next != Phase.Hidden;
        }

        // Bolhas subindo da fenda, para dar tempo de sair de cima.
        void WarningEffects()
        {
            if (Time.time < nextSpark) return;
            nextSpark = Time.time + .2f;
            Vector2 side = transform.right * Random.Range(-.4f, .4f);
            Effects.Spark((Vector2)transform.position + side + (Vector2)transform.up * .2f);
        }

        bool CascoNear()
        {
            var casco = Target;
            return casco != null && !casco.Dead && Vector2.Distance(casco.Center, transform.position) <= senseRange;
        }

        // Machuca e empurra para o lado da varrida.
        protected override void OnContact(Casco casco)
        {
            casco.Hurt(transform.position);
            casco.Push((Vector2)transform.right * sweepSign * pushSpeed);
        }

        // Escondido (ou só a sombra no chão), o golpe não acha nada.
        public override void Hit(int damage, Vector2 from)
        {
            if (Dead || phase != Phase.Out) return;
            base.Hit(damage, from);
        }

        // O clarão durante o aviso faz ele desistir; fora, ele fica parado e exposto.
        protected override void OnStunned()
        {
            if (phase != Phase.Warning) return;
            if (once) Destroy(gameObject);
            else Hide();
        }

        // Preso ao chão: o barulho da válvula não o tira do lugar.
        protected override void OnAlert(Vector2 origin) { }
    }
}
