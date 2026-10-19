using UnityEngine;

namespace MareBaixa
{
    // Moreia de Cano: mora num cano e só mostra um pedaço da cabeça (escondida é invulnerável).
    // Casco passando na frente do cano ⇒ a cabeça treme (aviso) ⇒ bote rápido de 3 blocos (o
    // contato machuca) ⇒ volta devagar, vulnerável ⇒ esconde de novo. O jeito é esperar o bote
    // passar e bater na volta (GDD p. 8). Atordoada, fica do lado de fora apanhando.
    //
    // O bote vai para o lado livre do cano (o lado sem "#" na sala). A raiz é a cabeça e anda;
    // o filho "Corpo" (sprite repetido) liga a cabeça ao cano.
    //
    // Prefab: SpriteRenderer (sorting 15, moreia-escondida, pivô nos pés) + Rigidbody2D Kinematic
    // + BoxCollider2D trigger (0.7 × 0.5, offset 0, 0.4) + Moreia > filho "Corpo" com SpriteRenderer
    // (sorting 14, moreia-corpo, pivô nos pés, Draw Mode Tiled). Preencher hidden, strike, retract e body.
    public class Moreia : Enemy
    {
        enum Phase { Hidden, Warning, Strike, Retract, Cooldown }

        [Header("Arte (a cabeça olha para a direita)")]
        public Sprite hidden;
        public Sprite strike;
        public Sprite retract;
        public SpriteRenderer body;

        [Header("Bote")]
        public float sightRange = 3.5f;
        public float sightHeight = 1f;
        public float reach = 3f;
        public float hiddenPeek = .25f;    // quanto da cabeça aparece fora do cano escondida
        public float warningTime = .35f;
        public float strikeTime = .25f;
        public float retractTime = .9f;
        public float cooldownTime = 1.2f;

        Phase phase = Phase.Hidden;
        float phaseStart;
        float direction = 1f;
        float extension;
        float retractFrom;

        public bool Hidden => phase == Phase.Hidden || phase == Phase.Cooldown;
        // Só apanha na volta (ou atordoada): o jeito é esquivar do bote e bater enquanto ela recolhe.
        public bool Vulnerable => phase == Phase.Retract || Stunned;
        public bool Striking => phase == Phase.Strike;
        public float Direction => direction;

        protected override bool ContactHurts => phase == Phase.Strike;

        // Valores do GDD como padrão do componente; o que estiver salvo no prefab sobrescreve.
        public Moreia()
        {
            maxHp = 3;
            pearlsMin = 2;
            pearlsMax = 3;
        }

        void Start() => direction = FreeSide();

        // O bote vai para o lado sem pedra. Sem sala (teste), vai para a direita.
        float FreeSide()
        {
            var room = Director.Instance != null ? Director.Instance.Room : null;
            if (room == null) return 1f;
            Vector2 local = (Vector2)transform.position - (Vector2)room.transform.position;
            int x = Mathf.FloorToInt(local.x);
            int y = Mathf.FloorToInt(local.y);
            bool rightBlocked = room.At(x + 1, y) == '#';
            bool leftBlocked = room.At(x - 1, y) == '#';
            if (rightBlocked && !leftBlocked) return -1f;
            return 1f;
        }

        // Teste automático: dá o bote agora, sem esperar Casco passar.
        public void ForceStrike()
        {
            if (phase == Phase.Hidden || phase == Phase.Cooldown) SetPhase(Phase.Warning);
        }

        protected override Vector2 Steer(float deltaTime)
        {
            float elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Hidden:
                case Phase.Cooldown:
                    extension = Mathf.MoveTowards(extension, hiddenPeek, reach * 2f * deltaTime);
                    break;
                case Phase.Warning:
                    extension = hiddenPeek;
                    break;
                case Phase.Strike:
                    // Sai rápido e freia no fim (ease-out).
                    float t = Mathf.Clamp01(elapsed / strikeTime);
                    extension = Mathf.Lerp(hiddenPeek, reach, 1f - (1f - t) * (1f - t));
                    break;
                case Phase.Retract:
                    extension = Mathf.Lerp(retractFrom, hiddenPeek, Mathf.Clamp01(elapsed / retractTime));
                    break;
            }

            Vector2 goal = Home + new Vector2(direction * (extension - hiddenPeek), 0f);
            if (phase == Phase.Warning)
                goal += new Vector2(Mathf.Sin(Time.time * 80f) * .05f, Mathf.Cos(Time.time * 67f) * .03f);
            return goal;
        }

        protected override void Update()
        {
            float elapsed = Time.time - phaseStart;
            // Atordoada, o tempo da fase não corre: ela fica do lado de fora até passar.
            if (Stunned) phaseStart += Time.deltaTime;
            else
            {
                switch (phase)
                {
                    case Phase.Hidden:
                        if (CascoInFront()) SetPhase(Phase.Warning);
                        break;
                    case Phase.Warning:
                        if (elapsed >= warningTime)
                        {
                            SetPhase(Phase.Strike);
                            Sound?.Play(Sfx.Attack);
                        }
                        break;
                    case Phase.Strike:
                        if (elapsed >= strikeTime) SetPhase(Phase.Retract);
                        break;
                    case Phase.Retract:
                        if (elapsed >= retractTime) SetPhase(Phase.Cooldown);
                        break;
                    case Phase.Cooldown:
                        if (elapsed >= cooldownTime) SetPhase(Phase.Hidden);
                        break;
                }
            }

            if (Stunned) Renderer.sprite = retract != null ? retract : strike;
            else if (phase == Phase.Strike) Renderer.sprite = strike;
            else if (phase == Phase.Retract) Renderer.sprite = retract;
            else Renderer.sprite = hidden;
            Renderer.flipX = direction < 0f;
            UpdateBody();
            base.Update();
        }

        // O corpo vai da boca do cano até a nuca da cabeça.
        void UpdateBody()
        {
            if (body == null) return;
            float length = Mathf.Max(0f, extension - hiddenPeek);
            body.enabled = length > .05f;
            body.flipX = direction < 0f;
            body.size = new Vector2(length, body.size.y);
            body.transform.localPosition = new Vector3(-direction * length * .5f, 0f, 0f);
        }

        bool CascoInFront()
        {
            var casco = Target;
            if (casco == null || casco.Dead) return false;
            Vector2 offset = casco.Center - Home;
            float ahead = offset.x * direction;
            return ahead > 0f && ahead <= sightRange && Mathf.Abs(offset.y) <= sightHeight;
        }

        void SetPhase(Phase next)
        {
            if (next == Phase.Retract) retractFrom = extension;
            phase = next;
            phaseStart = Time.time;
        }

        // Fora da volta (escondida, no aviso ou no bote), o golpe bate no cano ou nos dentes.
        public override void Hit(int damage, Vector2 from)
        {
            if (Dead) return;
            if (!Vulnerable)
            {
                Effects.Spark(from);
                return;
            }
            base.Hit(damage, from);
        }

        // Atordoada não consegue se esconder: fica vulnerável até passar e depois volta para o cano.
        protected override void OnStunned()
        {
            if (phase != Phase.Retract) SetPhase(Phase.Retract);
        }

        // Presa ao cano: o barulho da válvula não tira a moreia do lugar.
        protected override void OnAlert(Vector2 origin) { }
    }
}
