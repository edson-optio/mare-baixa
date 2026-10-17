using UnityEngine;

namespace MareBaixa
{
    // Base dos inimigos do Recife: vida, piscar vermelho e recuo ao apanhar, pérolas ao morrer e
    // dano por contato. O corpo é Kinematic e cada inimigo só diz onde quer estar (Steer); a física
    // não empurra ninguém, então o movimento fica exatamente o planejado.
    //
    // Prefab: SpriteRenderer (sorting 15) + Rigidbody2D Kinematic + Collider2D trigger, tudo na raiz.
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public abstract class Enemy : MonoBehaviour, IHittable
    {
        [Header("Vida e recompensa")]
        public int maxHp = 2;
        public int pearlsMin = 1;
        public int pearlsMax = 2;

        static readonly Color FlashColor = new Color(1f, .45f, .4f, 1f);

        public int Hp { get; protected set; }
        public bool Dead { get; private set; }

        protected Rigidbody2D Body { get; private set; }
        protected SpriteRenderer Renderer { get; private set; }
        protected Vector2 Home { get; private set; }
        protected Casco Target => Director.Instance != null ? Director.Instance.Casco : null;
        protected GameAudio Sound => Director.Instance != null ? Director.Instance.Sound : null;

        // Só machuca no contato quando isto for verdade (a água-viva só durante o choque).
        protected virtual bool ContactHurts => true;

        Vector2 recoil;
        float flashUntil;
        float animationOffset;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            // Parado no lugar, um corpo dormindo deixaria de avisar o contato com Casco.
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            Renderer = GetComponent<SpriteRenderer>();
            Home = transform.position;
            Hp = maxHp;
            animationOffset = Random.value * 10f;
        }

        // Posição desejada neste passo de física, sem contar o recuo.
        protected abstract Vector2 Steer(float deltaTime);

        protected virtual void FixedUpdate()
        {
            if (Dead) return;
            Vector2 goal = Steer(Time.fixedDeltaTime);
            recoil = Vector2.MoveTowards(recoil, Vector2.zero, 2f * Time.fixedDeltaTime);
            Body.MovePosition(goal + recoil);
        }

        protected virtual void Update()
        {
            Color tint = Time.time < flashUntil ? FlashColor : Color.white;
            tint.a = Renderer.color.a;   // a arte semitransparente decide a própria opacidade
            Renderer.color = tint;
        }

        // Quadro atual de uma animação em laço, com fase própria para inimigos iguais não andarem em sincronia.
        protected Sprite Frame(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return Renderer.sprite;
            int index = Mathf.FloorToInt((Time.time + animationOffset) * fps) % frames.Length;
            return frames[index];
        }

        // ---------------------------------------------------------------- Dano

        public virtual void Hit(int damage, Vector2 from)
        {
            if (Dead) return;
            Hp -= damage;
            flashUntil = Time.time + .15f;
            Vector2 away = (Vector2)transform.position - from;
            recoil = (away.sqrMagnitude > .0001f ? away.normalized : Vector2.up) * .35f;
            if (Hp <= 0) Die();
            else OnHurt(from);
        }

        // Chamado quando apanha e sobrevive.
        protected virtual void OnHurt(Vector2 from) { }

        protected virtual void Die()
        {
            Dead = true;
            Pearl.Drop(transform.position, Random.Range(pearlsMin, pearlsMax + 1), transform.parent);
            Effects.Spark(transform.position);
            Director.Instance?.CameraRig.Shake(.08f, .12f);
            Destroy(gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (Dead || !ContactHurts) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco != null && !casco.Dead) casco.Hurt(transform.position);
        }
    }
}
