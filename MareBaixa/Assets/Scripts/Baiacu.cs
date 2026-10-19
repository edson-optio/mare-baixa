using UnityEngine;

namespace MareBaixa
{
    // Baiacu Nervoso: patrulha devagar e é espinhento (o contato sempre machuca). Ao apanhar,
    // incha, treme e explode em espinhos; depois desincha e fica vulnerável por um tempo. O jeito
    // é bater uma vez e recuar antes que ele exploda (GDD p. 8).
    //
    // Prefab: SpriteRenderer (sorting 15) + Rigidbody2D Kinematic + CircleCollider2D trigger
    // (raio ~0.4) + Baiacu. Preencher swimFrames (baiacu-1/2) e inflated (baiacu-inflado).
    public class Baiacu : Enemy
    {
        enum Phase { Swim, Fuse, Deflate }

        [Header("Arte")]
        public Sprite[] swimFrames;
        public Sprite inflated;
        public bool artFacesRight = true;

        [Header("Patrulha")]
        public float speed = 1.2f;
        public float range = 2.5f;

        [Header("Explosão")]
        public float fuseTime = 1f;
        public float deflateTime = 1.5f;
        public float inflatedScale = 1.25f;
        public int spikeCount = 8;
        public float spikeSpeed = 6f;

        Phase phase = Phase.Swim;
        float phaseStart;
        float direction = 1f;
        float x;
        float bobClock;
        int groundMask;

        // Valores do GDD como padrão do componente; o que estiver salvo no prefab sobrescreve.
        public Baiacu()
        {
            maxHp = 3;
            pearlsMin = 2;
            pearlsMax = 3;
        }

        protected override void Awake()
        {
            base.Awake();
            groundMask = LayerMask.GetMask("Chao");
            direction = Random.value < .5f ? -1f : 1f;
            bobClock = Random.value * 10f;
        }

        protected override Vector2 Steer(float deltaTime)
        {
            float elapsed = Time.time - phaseStart;
            float pace = phase == Phase.Swim ? 1f : phase == Phase.Deflate ? .4f : 0f;
            if (pace > 0f)
            {
                // Vira nas pontas da patrulha ou quando vai bater numa parede.
                Vector2 position = Body.position;
                bool wall = Physics2D.Raycast(position, new Vector2(direction, 0f), .6f, groundMask).collider != null;
                if (wall || (direction > 0f && x >= range) || (direction < 0f && x <= -range))
                    direction = -direction;
                x = Mathf.Clamp(x + direction * speed * pace * deltaTime, -range, range);
                bobClock += deltaTime;
            }

            Vector2 goal = Home + new Vector2(x, Mathf.Sin(bobClock * 2.5f) * .1f);
            if (phase == Phase.Fuse)
            {
                // Treme mais forte conforme o pavio acaba.
                float strength = Mathf.Lerp(.02f, .07f, elapsed / fuseTime);
                goal += new Vector2(Mathf.Sin(Time.time * 70f), Mathf.Cos(Time.time * 53f)) * strength;
            }
            return goal;
        }

        protected override void Update()
        {
            // Atordoado, o pavio e o murchar ficam congelados.
            if (Stunned) phaseStart += Time.deltaTime;
            float elapsed = Time.time - phaseStart;
            if (phase == Phase.Fuse && elapsed >= fuseTime) Explode();
            else if (phase == Phase.Deflate && elapsed >= deflateTime) SetPhase(Phase.Swim);

            float scale = 1f;
            switch (phase)
            {
                case Phase.Swim:
                    Renderer.sprite = Frame(swimFrames, 4f);
                    break;
                case Phase.Fuse:
                    Renderer.sprite = inflated;
                    scale = Mathf.Lerp(1f, inflatedScale, Mathf.Clamp01(elapsed / .15f));
                    break;
                case Phase.Deflate:
                    // Murcha devagar; no começo ainda mostra o corpo cheio.
                    Renderer.sprite = elapsed < .3f ? inflated : Frame(swimFrames, 2f);
                    scale = Mathf.Lerp(inflatedScale, 1f, elapsed / .5f);
                    break;
            }
            transform.localScale = new Vector3(scale, scale, 1f);
            Renderer.flipX = artFacesRight ? direction < 0f : direction > 0f;
            base.Update();
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        // Inchado, os espinhos protegem: o golpe não entra até ele explodir (atordoado, entra).
        public override void Hit(int damage, Vector2 from)
        {
            if (Dead) return;
            if (phase == Phase.Fuse && !Stunned)
            {
                Effects.Spark(from);
                return;
            }
            base.Hit(damage, from);
        }

        protected override void OnHurt(Vector2 from)
        {
            if (phase == Phase.Swim) SetPhase(Phase.Fuse);
        }

        void Explode()
        {
            var kit = Kit.Current;
            if (kit != null && kit.spike != null)
            {
                for (int i = 0; i < spikeCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / spikeCount;
                    var heading = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var spike = Instantiate(kit.spike, (Vector2)transform.position + heading * .3f, Quaternion.identity, transform.parent);
                    spike.Launch(heading * spikeSpeed);
                }
            }
            Sound?.Play(Sfx.Burst);
            Effects.Spark(transform.position);
            Director.Instance?.CameraRig.Shake(.12f, .18f);
            SetPhase(Phase.Deflate);
        }
    }
}
