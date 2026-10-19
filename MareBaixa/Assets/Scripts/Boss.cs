using System.Collections;
using UnityEngine;

namespace MareBaixa
{
    // Base dos chefes (GDD p. 8): vida em fases, piscar e hit-stop ao apanhar, "tink" quando o golpe
    // cai fora da janela de vulnerabilidade, apresentação quando Casco chega perto e derrota com pérolas.
    // Cada chefe diz quando está vulnerável, quando o contato machuca e o que faz a cada quadro (Think).
    //
    // Prefab: raiz com Rigidbody2D Kinematic + o componente do chefe; a arte fica em filhos.
    //
    // Números finais: acorda com Casco a 9 blocos na horizontal (Polvo: 12), apresentação 1.2 s,
    // golpe que entra = piscar 0.15 s + hit-stop 0.07 s; derrota em 1.2 s com 15 pérolas.
    public abstract class Boss : MonoBehaviour, IHittable, IStunnable
    {
        // O chefe da sala atual (nulo sem chefe ou depois de derrotado). O HUD mostra a barra dele.
        public static Boss Current { get; protected set; }

        public string DisplayName;
        public int MaxHp = 10;
        public int Hp { get; protected set; }
        public int Phase { get; protected set; } = 1;
        public bool Defeated { get; protected set; }

        [Header("Apresentação e derrota")]
        public float wakeDistance = 9f;     // distância horizontal de Casco que acorda o chefe
        public float introTime = 1.2f;
        public int pearlReward = 15;

        static readonly Color FlashColor = new Color(1f, .45f, .4f, 1f);

        // Já se apresentou (a barra de chefe pode aparecer).
        public bool Revealed { get; private set; }
        // Terminou a apresentação e está lutando.
        public bool Fighting => Revealed && Time.time >= fightAt && !Dying && !Defeated;
        // Levou o último golpe e está na animação de derrota.
        public bool Dying { get; private set; }

        protected Rigidbody2D Body { get; private set; }
        protected Casco Target => Director.Instance != null ? Director.Instance.Casco : null;
        protected GameAudio Sound => Director.Instance != null ? Director.Instance.Sound : null;
        protected Transform RoomParent => transform.parent;

        protected abstract bool Vulnerable { get; }
        protected virtual bool ContactHurts => false;

        // Centro visual do chefe (pérolas e brilhos saem daqui).
        protected virtual Vector2 Center => (Vector2)transform.position + Vector2.up;

        SpriteRenderer[] renderers;
        float flashUntil;
        float fightAt;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            if (Body != null)
            {
                Body.bodyType = RigidbodyType2D.Kinematic;
                // Parado no lugar, um corpo dormindo deixaria de avisar o contato com Casco.
                Body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            }
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Hp = MaxHp;
        }

        protected virtual void OnEnable() => Current = this;
        protected virtual void OnDisable() { if (Current == this) Current = null; }

        protected virtual void Update()
        {
            if (Defeated) return;
            if (!Revealed && !Dying)
            {
                var casco = Target;
                if (casco != null && !casco.Dead && Mathf.Abs(casco.transform.position.x - transform.position.x) <= wakeDistance)
                    Reveal();
            }
            if (Fighting) Think();
            if (!Dying) Tint(Time.time < flashUntil ? FlashColor : Color.white);
        }

        // Comportamento do chefe a cada quadro, depois da apresentação.
        protected abstract void Think();

        // Chamado no começo da apresentação (o chefe pode abrir os olhos, rugir...).
        protected virtual void OnReveal() { }

        // Fase para uma vida dada; a troca chama OnPhaseChanged.
        protected abstract int PhaseFor(int hp);
        protected virtual void OnPhaseChanged(int phase) { }

        // Extra da derrota (concha rara, maré recuando). Roda antes de avisar o Director.
        protected virtual void OnDefeated() { }

        // Acorda o chefe na hora (o teste automático usa para não depender da posição de Casco).
        public void Reveal()
        {
            if (Revealed) return;
            Revealed = true;
            fightAt = Time.time + introTime;
            Director.Instance?.CameraRig.Shake(.15f, introTime);
            Sound?.Play(Sfx.Roar);
            OnReveal();
        }

        // Pula a apresentação (teste automático).
        public void SkipIntro()
        {
            Reveal();
            fightAt = Time.time;
        }

        // ---------------------------------------------------------------- Dano

        public virtual void Hit(int damage, Vector2 from)
        {
            if (Dying || Defeated) return;
            if (!Fighting || !Vulnerable)
            {
                Tink(from);
                return;
            }
            Damage(damage, from);
        }

        // Golpe que não entra: faísca e som de erro, sem dano.
        protected void Tink(Vector2 from)
        {
            Effects.Spark(Vector2.Lerp(from, Center, .5f));
            Sound?.Play(Sfx.Error);
        }

        protected void Damage(int damage, Vector2 from)
        {
            if (Dying || Defeated || damage <= 0) return;
            Hp = Mathf.Max(0, Hp - damage);
            flashUntil = Time.time + .15f;
            Effects.HitStop(.07f);
            Effects.Spark(Vector2.Lerp(from, Center, .5f));
            Director.Instance?.CameraRig.Shake(.1f, .12f);
            if (Hp <= 0)
            {
                StartCoroutine(Defeat());
                return;
            }
            int next = PhaseFor(Hp);
            if (next != Phase) SetPhase(next);
        }

        protected void SetPhase(int phase)
        {
            Phase = phase;
            Director.Instance?.CameraRig.Shake(.2f, .4f);
            OnPhaseChanged(phase);
        }

        // Só vale onde o chefe disser (o Colosso tonto fica mais tempo tonto).
        public virtual void Stun(float seconds) { }

        // ---------------------------------------------------------------- Derrota

        // Derrota imediata, sem a animação (teste automático): mesmo final da derrota normal.
        public void DefeatNow()
        {
            if (Defeated) return;
            StopAllCoroutines();
            BeginDefeat();
            Hp = 0;
            FinishDefeat();
        }

        void BeginDefeat()
        {
            Dying = true;
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            Sound?.Play(Sfx.Burst);
            Director.Instance?.CameraRig.Shake(.35f, 1.2f);
        }

        IEnumerator Defeat()
        {
            BeginDefeat();

            // Pisca, encolhe e solta brilhos durante 1.2 s.
            Vector3 startScale = transform.localScale;
            const float duration = 1.2f;
            float sparkAt = 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Tint(Mathf.FloorToInt(t * 14f) % 2 == 0 ? Color.white : FlashColor);
                transform.localScale = startScale * Mathf.Lerp(1f, .55f, t / duration);
                if (t >= sparkAt)
                {
                    Effects.Spark(Center + Random.insideUnitCircle * 1.2f);
                    sparkAt = t + .12f;
                }
                yield return null;
            }
            FinishDefeat();
        }

        void FinishDefeat()
        {
            if (Defeated) return;
            Effects.Shards(Center, ShellType.Comum);
            Pearl.Drop(Center, pearlReward, RoomParent);
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = false;
            foreach (var light in GetComponentsInChildren<LightSource>()) light.enabled = false;
            OnDefeated();
            Defeated = true;
            if (Current == this) Current = null;
            Director.Instance?.OnBossDefeated(this);
        }

        void Tint(Color color)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                color.a = renderer.color.a;
                renderer.color = color;
            }
        }

        // ---------------------------------------------------------------- Ajuda para os chefes

        // Chão logo abaixo de um ponto (ou o próprio ponto se não houver chão perto).
        protected static Vector2 FloorBelow(Vector2 point, float range = 12f)
        {
            var hit = Physics2D.Raycast(point, Vector2.down, range, LayerMask.GetMask("Chao"));
            return hit.collider != null ? hit.point : point;
        }

        // ---------------------------------------------------------------- Contato

        void OnTriggerStay2D(Collider2D other)
        {
            if (!Fighting || !ContactHurts) return;
            var casco = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<Casco>()
                : other.GetComponent<Casco>();
            if (casco != null && !casco.Dead) casco.Hurt(transform.position);
        }
    }
}
