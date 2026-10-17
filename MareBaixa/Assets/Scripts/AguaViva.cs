using UnityEngine;

namespace MareBaixa
{
    // Água-viva Errante: flutua em zigue-zague e alterna calma, aviso e choque. Na calma é
    // inofensiva; no choque o contato machuca e quem ataca leva o choque. A graça é atacar entre
    // um choque e outro (GDD p. 8).
    //
    // Prefab: SpriteRenderer (sorting 15) + Rigidbody2D Kinematic + CircleCollider2D trigger
    // (raio ~0.4) + AguaViva. Preencher calmFrames (agua-viva-1/2) e chargedFrames (carregada-1/2).
    public class AguaViva : Enemy
    {
        enum Phase { Calm, Warning, Shock }

        [Header("Arte")]
        public Sprite[] calmFrames;
        public Sprite[] chargedFrames;

        [Header("Movimento")]
        public float rangeX = 2f;
        public float rangeY = .6f;
        public float driftPeriod = 6f;     // segundos para ir e voltar na horizontal
        public float bobPeriod = 1.4f;     // o sobe e desce é mais rápido: daí o zigue-zague

        [Header("Ciclo (s)")]
        public float calmTime = 1.8f;
        public float warningTime = .4f;
        public float shockTime = 1.2f;

        Phase phase;
        float phaseUntil;
        float clock;

        protected override bool ContactHurts => phase == Phase.Shock;
        public bool Shocking => phase == Phase.Shock;

        protected override void Awake()
        {
            base.Awake();
            // Começa em pontos diferentes do caminho e do ciclo para duas águas-vivas não piscarem juntas.
            clock = Random.value * driftPeriod;
            phase = Phase.Calm;
            phaseUntil = Time.time + Random.Range(.3f, calmTime);
            transform.position = Steer(0f);
        }

        protected override Vector2 Steer(float deltaTime)
        {
            clock += deltaTime;
            float x = rangeX * Triangle(clock / driftPeriod);
            float y = rangeY * Mathf.Sin(clock / bobPeriod * Mathf.PI * 2f);
            return Home + new Vector2(x, y);
        }

        // Onda triangular de -1 a 1: velocidade constante na horizontal, virando nas pontas.
        static float Triangle(float t) => Mathf.PingPong(t * 8f + 2f, 4f) * .5f - 1f;

        protected override void Update()
        {
            if (Time.time >= phaseUntil) Advance();

            switch (phase)
            {
                case Phase.Calm:
                    Renderer.sprite = Frame(calmFrames, 3f);
                    break;
                case Phase.Warning:
                    // Pisca entre calma e carregada para avisar que o choque vem aí.
                    bool charged = Mathf.Repeat(Time.time * 12f, 1f) < .5f;
                    Renderer.sprite = charged ? First(chargedFrames) : First(calmFrames);
                    break;
                case Phase.Shock:
                    Renderer.sprite = Frame(chargedFrames, 14f);
                    break;
            }
            base.Update();
        }

        void Advance()
        {
            switch (phase)
            {
                case Phase.Calm:
                    phase = Phase.Warning;
                    phaseUntil = Time.time + warningTime;
                    break;
                case Phase.Warning:
                    phase = Phase.Shock;
                    phaseUntil = Time.time + shockTime;
                    Sound?.Play(Sfx.Shock);
                    break;
                default:
                    phase = Phase.Calm;
                    phaseUntil = Time.time + calmTime;
                    break;
            }
        }

        static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        // Atacar durante o choque não fere a água-viva: o choque volta pela pinça.
        public override void Hit(int damage, Vector2 from)
        {
            if (Dead) return;
            if (phase == Phase.Shock)
            {
                var casco = Target;
                if (casco != null && !casco.Dead) casco.Hurt(transform.position);
                Sound?.Play(Sfx.Shock);
                Effects.Spark(from);
                return;
            }
            base.Hit(damage, from);
        }
    }
}
