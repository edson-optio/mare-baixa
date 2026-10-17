using UnityEngine;

namespace MareBaixa
{
    // Pérola, a moeda do jogo. Parada na sala ou nascendo pulando de um inimigo ou baú; perto de
    // Casco ela é puxada como por um ímã e, ao tocar, entra na conta da run.
    //
    // Prefab: SpriteRenderer (sorting 10, perola-1) + Rigidbody2D Kinematic + CircleCollider2D
    // trigger (raio 0.3) + Pearl. Preencher frames (perola-1/2).
    [RequireComponent(typeof(Rigidbody2D))]
    public class Pearl : MonoBehaviour
    {
        public Sprite[] frames;
        public float framesPerSecond = 4f;
        public float magnetRadius = 2f;
        public float magnetSpeed = 12f;
        public float popTime = .4f;

        Rigidbody2D body;
        Vector2 velocity;
        Vector2 popVelocity;
        float popUntil;
        float pull;
        bool collected;
        int groundMask;

        // Espalha várias pérolas pulando a partir de um ponto (inimigo morto, baú aberto).
        public static void Drop(Vector2 position, int count, Transform parent)
        {
            var kit = Kit.Current;
            if (kit == null || kit.pearl == null) return;
            for (int i = 0; i < count; i++)
            {
                var pearl = Instantiate(kit.pearl, position, Quaternion.identity, parent).GetComponent<Pearl>();
                if (pearl != null) pearl.Pop(new Vector2(Random.Range(-3f, 3f), Random.Range(3f, 5.5f)));
            }
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            // Parada no lugar, dormindo ela deixaria de perceber Casco encostado.
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            groundMask = LayerMask.GetMask("Chao");
        }

        void Start()
        {
            if (frames != null && frames.Length > 0)
            {
                var animator = GetComponent<SpriteAnimator>();
                if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
                animator.Play(frames, framesPerSecond);
            }
        }

        // Nasce pulando: sai com esta velocidade e freia até parar em popTime segundos.
        public void Pop(Vector2 newVelocity)
        {
            popVelocity = velocity = newVelocity;
            popUntil = Time.time + popTime;
        }

        bool Popping => Time.time < popUntil;

        void FixedUpdate()
        {
            Vector2 position = body.position;
            var casco = Director.Instance != null ? Director.Instance.Casco : null;

            if (Popping)
            {
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, popVelocity.magnitude / popTime * Time.fixedDeltaTime);
                Vector2 next = position + velocity * Time.fixedDeltaTime;
                // Não entra na pedra: bateu, para ali mesmo.
                if (Physics2D.OverlapCircle(next, .15f, groundMask) != null) velocity = Vector2.zero;
                else body.MovePosition(next);
            }
            else if (casco != null && !casco.Dead && Vector2.Distance(position, casco.Center) <= magnetRadius)
            {
                // O puxão acelera, para a pérola "grudar" em quem passa perto.
                pull = Mathf.MoveTowards(pull, magnetSpeed, 40f * Time.fixedDeltaTime);
                body.MovePosition(Vector2.MoveTowards(position, casco.Center, pull * Time.fixedDeltaTime));
            }
            else pull = 0f;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            // Durante o pulo de nascimento ela ainda não pode ser pega, para dar para vê-la saindo.
            if (collected || Popping) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco == null || casco.Dead) return;
            collected = true;
            Director.Instance?.AddPearls(1);
            Effects.Spark(transform.position);
            Destroy(gameObject);
        }
    }
}
