using UnityEngine;

namespace MareBaixa
{
    // Peixe-pescador: flutua devagar com a isca acesa na frente da boca; no escuro da Fossa só a
    // isca aparece. Casco perto ⇒ vira para ele, abre a boca (aviso) e dá uma mordida-avanço curta.
    // Só a boca machuca: de lado ou por trás é seguro. O clarão da Lâmpada atordoa (GDD p. 8).
    //
    // Prefab: SpriteRenderer (sorting 15, pescador-1, pivô no centro, arte olhando para a direita) +
    // Rigidbody2D Kinematic + CircleCollider2D trigger (raio 0.45) + PeixePescador > filho "Isca"
    // com SpriteRenderer (sorting 61, acima da Escuridao) + LightSource (raio 1.2). Preencher swimFrames
    // (pescador-1/2), mouthOpen (pescador-boca) e lure (o LightSource do filho).
    public class PeixePescador : Enemy
    {
        enum Phase { Drift, Warning, Bite, Recover }

        [Header("Arte")]
        public Sprite[] swimFrames;
        public Sprite mouthOpen;
        public LightSource lure;
        public bool artFacesRight = true;

        [Header("Movimento")]
        public float rangeX = 1.5f;
        public float rangeY = .4f;
        public float driftPeriod = 7f;
        public float returnSpeed = 1f;

        [Header("Mordida")]
        public float sightRange = 4f;
        public float warningTime = .5f;
        public float biteTime = .3f;
        public float biteDistance = 2.2f;
        public float recoverTime = 1.2f;
        public float mouthOffset = .45f;
        public float mouthRadius = .5f;

        [Header("Isca")]
        // Posição da lâmpada desenhada na antena do sprite (pescador-1..4, 96×64 a 64 PPU).
        public float lureOffset = .62f;
        public float lureHeight = .14f;
        public float lureRadius = 1.2f;

        Phase phase = Phase.Drift;
        float phaseStart;
        float facing = 1f;
        float clock;
        Vector2 position;
        Vector2 biteFrom;
        Vector2 biteTo;

        public bool Biting => phase == Phase.Bite;
        public bool Opening => phase == Phase.Warning;
        public float Facing => facing;
        public Vector2 Mouth => (Vector2)transform.position + new Vector2(facing * mouthOffset, 0f);

        // Só a boca machuca (ver Update); o corpo encostado é seguro.
        protected override bool ContactHurts => false;

        // Valores do GDD como padrão do componente; o que estiver salvo no prefab sobrescreve.
        public PeixePescador()
        {
            maxHp = 3;
            pearlsMin = 2;
            pearlsMax = 3;
        }

        protected override void Awake()
        {
            base.Awake();
            clock = Random.value * driftPeriod;
            position = transform.position;
            if (lure != null) lure.radius = lureRadius;
        }

        // Teste automático: começa a mordida agora, na direção de Casco.
        public void ForceBite()
        {
            if (phase == Phase.Drift) StartWarning();
        }

        protected override Vector2 Steer(float deltaTime)
        {
            float elapsed = Time.time - phaseStart;
            switch (phase)
            {
                case Phase.Drift:
                    clock += deltaTime;
                    Vector2 path = Home + new Vector2(rangeX * Mathf.Sin(clock / driftPeriod * Mathf.PI * 2f),
                                                      rangeY * Mathf.Sin(clock / driftPeriod * Mathf.PI * 4f));
                    // Depois da mordida volta ao caminho sem teleportar: anda até ele.
                    Vector2 next = Vector2.MoveTowards(position, path, 2f * deltaTime);
                    if (Mathf.Abs(next.x - position.x) > .002f) facing = Mathf.Sign(next.x - position.x);
                    position = next;
                    break;
                case Phase.Warning:
                    // Recua um tiquinho, como quem toma impulso.
                    position = Vector2.MoveTowards(position, biteFrom - new Vector2(facing * .25f, 0f), deltaTime);
                    break;
                case Phase.Bite:
                    float t = Mathf.Clamp01(elapsed / biteTime);
                    position = Vector2.Lerp(biteFrom, biteTo, 1f - (1f - t) * (1f - t));
                    break;
                case Phase.Recover:
                    position = Vector2.MoveTowards(position, Home, returnSpeed * deltaTime);
                    break;
            }
            return position;
        }

        protected override void Update()
        {
            float elapsed = Time.time - phaseStart;
            if (Stunned) phaseStart += Time.deltaTime;
            else
            {
                switch (phase)
                {
                    case Phase.Drift:
                        if (CascoNear()) StartWarning();
                        break;
                    case Phase.Warning:
                        if (elapsed >= warningTime) StartBite();
                        break;
                    case Phase.Bite:
                        if (elapsed >= biteTime) SetPhase(Phase.Recover);
                        break;
                    case Phase.Recover:
                        if (elapsed >= recoverTime) SetPhase(Phase.Drift);
                        break;
                }
                BiteCheck();
            }

            bool open = !Stunned && (phase == Phase.Warning || phase == Phase.Bite);
            Renderer.sprite = open ? mouthOpen : Frame(swimFrames, phase == Phase.Drift ? 3f : 6f);
            Renderer.flipX = artFacesRight ? facing < 0f : facing > 0f;
            UpdateLure();
            base.Update();
        }

        // A isca fica sempre na frente da boca e pulsa de leve; atordoado, ela quase apaga.
        void UpdateLure()
        {
            if (lure == null) return;
            lure.transform.localPosition = new Vector3(facing * lureOffset, lureHeight, 0f);
            float pulse = 1f + Mathf.Sin(Time.time * 2.2f) * .08f;
            lure.radius = lureRadius * pulse * (Stunned ? .4f : 1f);
        }

        // A boca machuca sempre que encosta em Casco (parado ou mordendo).
        void BiteCheck()
        {
            var casco = Target;
            if (casco == null || casco.Dead) return;
            if (Vector2.Distance(casco.Center, Mouth) <= mouthRadius) casco.Hurt(Mouth);
        }

        bool CascoNear()
        {
            var casco = Target;
            return casco != null && !casco.Dead && Vector2.Distance(casco.Center, transform.position) <= sightRange;
        }

        void StartWarning()
        {
            var casco = Target;
            if (casco != null && Mathf.Abs(casco.Center.x - position.x) > .05f)
                facing = Mathf.Sign(casco.Center.x - position.x);
            biteFrom = position;
            SetPhase(Phase.Warning);
        }

        void StartBite()
        {
            biteFrom = position;
            var casco = Target;
            Vector2 heading = new Vector2(facing, 0f);
            if (casco != null)
            {
                Vector2 toCasco = casco.Center - position;
                // Mira em Casco, mas sem virar de costas nem subir/descer muito.
                if (toCasco.x * facing > 0f) heading = new Vector2(toCasco.x, Mathf.Clamp(toCasco.y, -Mathf.Abs(toCasco.x), Mathf.Abs(toCasco.x))).normalized;
            }
            biteTo = position + heading * biteDistance;
            Sound?.Play(Sfx.Attack);
            SetPhase(Phase.Bite);
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        // Atordoado no meio da mordida, para onde está e depois volta para casa.
        protected override void OnStunned()
        {
            if (phase != Phase.Drift) SetPhase(Phase.Recover);
        }
    }
}
