using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Base dos inimigos: vida, piscar vermelho e recuo ao apanhar, pérolas ao morrer e dano por
    // contato. O corpo é Kinematic e cada inimigo só diz onde quer estar (Steer); a física não
    // empurra ninguém, então o movimento fica exatamente o planejado.
    //
    // Atordoado (clarão da Lâmpada, investida da Lata) o inimigo para no lugar, pisca branco e azul
    // e não machuca no contato, mas continua apanhando normalmente. A válvula da Cidade faz barulho:
    // Enemy.Alert chama OnAlert em quem estiver perto, e o padrão é ir na direção do som por 4 s.
    //
    // Prefab: SpriteRenderer (sorting 15) + Rigidbody2D Kinematic + Collider2D trigger, tudo na raiz.
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public abstract class Enemy : MonoBehaviour, IHittable, IStunnable
    {
        [Header("Vida e recompensa")]
        public int maxHp = 2;
        public int pearlsMin = 1;
        public int pearlsMax = 2;

        static readonly Color FlashColor = new Color(1f, .45f, .4f, 1f);
        static readonly Color StunColor = new Color(.55f, .75f, 1f, 1f);
        static readonly List<Enemy> Alive = new List<Enemy>();

        const float AlertTime = 4f;
        const float AlertReach = 2.5f;     // quanto o ponto de referência anda na direção do som, no máximo
        const float AlertDrift = 1.5f;     // e a que velocidade (u/s)

        public int Hp { get; protected set; }
        public bool Dead { get; private set; }
        public bool Stunned => Time.time < stunUntil;
        public bool Alerted => Time.time < alertUntil;

        protected Rigidbody2D Body { get; private set; }
        protected SpriteRenderer Renderer { get; private set; }
        protected Vector2 Home => homeBase + homeOffset;
        protected Casco Target => Director.Instance != null ? Director.Instance.Casco : null;
        protected GameAudio Sound => Director.Instance != null ? Director.Instance.Sound : null;

        // Só machuca no contato quando isto for verdade (a água-viva só durante o choque).
        protected virtual bool ContactHurts => true;

        // Leva o empurrãozinho para trás ao apanhar (o tentáculo, preso à superfície, não).
        protected virtual bool Recoils => true;

        Vector2 homeBase;
        Vector2 homeOffset;
        Vector2 alertOffset;
        Vector2 recoil;
        Vector2 lastGoal;
        bool hasGoal;
        float flashUntil;
        float stunUntil;
        float alertUntil;
        float animationOffset;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            // Parado no lugar, um corpo dormindo deixaria de avisar o contato com Casco.
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            Renderer = GetComponent<SpriteRenderer>();
            homeBase = transform.position;
            Hp = maxHp;
            animationOffset = Random.value * 10f;
        }

        protected virtual void OnEnable() => Alive.Add(this);
        protected virtual void OnDisable() => Alive.Remove(this);

        // Troca o ponto de referência (para quem nasce num ponto e se ajeita, como a moreia no cano).
        protected void SetHome(Vector2 home)
        {
            homeBase = home;
            homeOffset = Vector2.zero;
        }

        // Posição desejada neste passo de física, sem contar o recuo.
        protected abstract Vector2 Steer(float deltaTime);

        protected virtual void FixedUpdate()
        {
            if (Dead) return;
            Vector2 offsetGoal = Alerted ? alertOffset : Vector2.zero;
            homeOffset = Vector2.MoveTowards(homeOffset, offsetGoal, AlertDrift * Time.fixedDeltaTime);

            // Atordoado não pensa: fica onde estava, só o recuo dos golpes ainda mexe nele.
            if (!hasGoal) lastGoal = Body.position;
            lastGoal = Stunned ? WhileStunned(lastGoal, Time.fixedDeltaTime) : Steer(Time.fixedDeltaTime);
            hasGoal = true;
            recoil = Vector2.MoveTowards(recoil, Vector2.zero, 2f * Time.fixedDeltaTime);
            Body.MovePosition(lastGoal + recoil);
        }

        protected virtual void Update()
        {
            Color tint = Color.white;
            if (Time.time < flashUntil) tint = FlashColor;
            else if (Stunned) tint = Mathf.Repeat(Time.time * 8f, 1f) < .5f ? Color.white : StunColor;
            tint.a = Renderer.color.a;   // a arte semitransparente decide a própria opacidade
            Renderer.color = tint;
        }

        // Posição enquanto atordoado. Padrão: parado; quem tem gravidade própria continua caindo.
        protected virtual Vector2 WhileStunned(Vector2 last, float deltaTime) => last;

        // Quadro atual de uma animação em laço, com fase própria para inimigos iguais não andarem em sincronia.
        protected Sprite Frame(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return Renderer.sprite;
            int index = Mathf.FloorToInt((Time.time + animationOffset) * fps) % frames.Length;
            return frames[index];
        }

        protected static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        // ---------------------------------------------------------------- Dano

        public virtual void Hit(int damage, Vector2 from)
        {
            if (Dead) return;
            Hp -= damage;
            flashUntil = Time.time + .15f;
            Vector2 away = (Vector2)transform.position - from;
            if (Recoils) recoil = (away.sqrMagnitude > .0001f ? away.normalized : Vector2.up) * .35f;
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

        // ---------------------------------------------------------------- Atordoar

        public virtual void Stun(float seconds)
        {
            if (Dead || seconds <= 0f) return;
            bool wasStunned = Stunned;
            stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
            if (!wasStunned) OnStunned();
        }

        // Chamado quando o atordoamento começa (não a cada renovação).
        protected virtual void OnStunned() { }

        // ---------------------------------------------------------------- Alerta

        // Barulho no mundo (a válvula girando): todo inimigo vivo no raio fica sabendo.
        public static void Alert(Vector2 origin, float radius)
        {
            float radiusSq = radius * radius;
            foreach (var enemy in Alive.ToArray())
            {
                if (enemy == null || enemy.Dead) continue;
                if (((Vector2)enemy.transform.position - origin).sqrMagnitude <= radiusSq) enemy.OnAlert(origin);
            }
        }

        // Padrão: o ponto de referência anda até AlertReach blocos na direção do som por 4 s.
        protected virtual void OnAlert(Vector2 origin)
        {
            alertOffset = Vector2.ClampMagnitude(origin - homeBase, AlertReach);
            alertUntil = Time.time + AlertTime;
        }

        // ---------------------------------------------------------------- Contato

        void OnTriggerStay2D(Collider2D other)
        {
            if (Dead || Stunned || !ContactHurts) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco != null && !casco.Dead) OnContact(casco);
        }

        // O que o contato faz com Casco (o tentáculo também empurra).
        protected virtual void OnContact(Casco casco) => casco.Hurt(transform.position);
    }
}
