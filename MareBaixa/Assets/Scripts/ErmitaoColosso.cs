using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Ermitão Colosso, mini-chefe da Cidade Afundada (GDD p. 8): caranguejo-ermitão gigante com um
    // pneu de concha. Anda até Casco, se encolhe no pneu (aviso) e rola pela arena até bater na
    // parede; o impacto derruba pedras do teto e o deixa tonto, o único momento em que apanha.
    // Na fase 2 rola mais rápido, ricocheteia uma vez na primeira parede e às vezes pula no lugar,
    // fazendo uma onda de choque no chão.
    //
    // Prefab: raiz (pivô nos pés) com Rigidbody2D Kinematic + BoxCollider2D trigger (2.2 × 1.7) +
    // ErmitaoColosso > filho "Arte" com SpriteRenderer (sorting 15).
    //
    // Equilíbrio (meta do GDD p. 6: vencer entre a 5ª e a 10ª run). Números finais:
    //   Vida 12, fase 2 com 6. Ciclo ~8 s: anda 1.6 u/s por ~2.2 s (ou até 3.5 de Casco), aviso 0.7 s,
    //   rola 9 u/s (fase 2: 12) e fica tonto 2.5 s (+1 s por Stun, no máx. 2×), levanta 0.5 s.
    //   Na janela tonta Casco chega em ~1 s e acerta 3–4 golpes (1 de dano, recarga 0.32 s; giro e
    //   investida dão 2): ~2 ciclos por fase, luta de 35–50 s.
    //   Desvio: a hitbox tem 1.7 de altura e Casco pula 2.5, então dá para pular o pneu (~0.4 s no ar
    //   acima dele contra ~0.3 s para o pneu passar a 9 u/s) ou rolar através (esquiva 0.3 s invulnerável).
    //   Pedras: 3 (fase 2: 5, uma mirando Casco), 8 u/s, aviso 0.25 s. Fase 2: ricochete com quique de
    //   0.8, pulo no lugar 45% (nunca dois seguidos) com ondas de 7 u/s e alcance 11 (pular por cima).
    public class ErmitaoColosso : Boss
    {
        public enum ColossoState { Idle, Walk, Warn, Roll, Dizzy, Rise, Crouch, Jump }

        [Header("Arte")]
        public SpriteRenderer art;
        public Sprite[] walkFrames;
        public Sprite[] rollFrames;
        public Sprite dizzySprite;
        public Sprite warnSprite;
        public bool artFacesRight;

        [Header("Movimento")]
        public float walkSpeed = 1.6f;
        public float walkTime = 2.2f;
        public float warnTime = .7f;
        public float rollSpeed = 9f;
        public float rollSpeedPhase2 = 12f;
        public float halfWidth = 1.4f;

        [Header("Tonto")]
        public float dizzyTime = 2.5f;
        public float stunBonus = 1f;
        public int maxStunBonuses = 2;
        public float riseTime = .5f;

        [Header("Impacto")]
        public int rocksPhase1 = 3;
        public int rocksPhase2 = 5;
        public float rockSpeed = 8f;

        [Header("Pulo (fase 2)")]
        public float jumpChance = .45f;
        public float jumpHeight = 2.6f;
        public float jumpTime = .8f;
        public float waveSpeed = 7f;
        public float waveRange = 11f;

        public ColossoState State { get; private set; } = ColossoState.Idle;
        public float StateTime => Time.time - stateStart;

        float stateStart;
        float stateLength;
        float direction = -1f;      // para onde olha / rola
        float floorY;
        float hop;                  // altura do pulo/quique acima do chão
        bool ricocheted;
        int stunBonuses;
        bool jumpedLast;
        float starsAt;
        int groundMask;
        Vector3 artHome;

        // Ondas de choque andando pelo chão depois do pulo.
        class Wave { public float x; public float dir; public float travelled; public Transform visual; }
        readonly List<Wave> waves = new List<Wave>();

        public ErmitaoColosso()
        {
            DisplayName = "Ermitão Colosso";
            MaxHp = 12;
        }

        protected override void Awake()
        {
            base.Awake();
            groundMask = LayerMask.GetMask("Chao");
            if (art == null) art = GetComponentInChildren<SpriteRenderer>();
            artHome = art != null ? art.transform.localPosition : Vector3.zero;
            floorY = transform.position.y;
        }

        protected override bool Vulnerable => State == ColossoState.Dizzy;
        protected override bool ContactHurts => State != ColossoState.Dizzy && State != ColossoState.Rise && State != ColossoState.Idle;
        protected override Vector2 Center => (Vector2)transform.position + new Vector2(0f, 1.1f);

        protected override int PhaseFor(int hp) => hp <= MaxHp / 2 ? 2 : 1;

        protected override void OnReveal()
        {
            // Treme no lugar durante a apresentação e depois começa a andar.
            FaceCasco();
            SetState(ColossoState.Idle, introTime + .3f);
        }

        void SetState(ColossoState next, float length)
        {
            State = next;
            stateStart = Time.time;
            stateLength = length;
        }

        void FaceCasco()
        {
            var casco = Target;
            if (casco != null && Mathf.Abs(casco.transform.position.x - transform.position.x) > .1f)
                direction = Mathf.Sign(casco.transform.position.x - transform.position.x);
        }

        // ---------------------------------------------------------------- Comportamento

        protected override void Think()
        {
            bool done = StateTime >= stateLength;
            switch (State)
            {
                case ColossoState.Idle:
                case ColossoState.Rise:
                    if (done) StartWalk();
                    break;
                case ColossoState.Walk:
                    if (done || CascoClose(3.5f)) SetState(ColossoState.Warn, warnTime);
                    break;
                case ColossoState.Warn:
                    FaceCasco();
                    if (done)
                    {
                        ricocheted = false;
                        SetState(ColossoState.Roll, 999f);
                        Sound?.Play(Sfx.Spin);
                    }
                    break;
                case ColossoState.Dizzy:
                    if (Time.time >= starsAt)
                    {
                        Effects.Spark(Center + new Vector2(Random.Range(-.8f, .8f), 1.3f));
                        starsAt = Time.time + .3f;
                    }
                    if (done) SetState(ColossoState.Rise, riseTime);
                    break;
                case ColossoState.Crouch:
                    if (done) SetState(ColossoState.Jump, jumpTime);
                    break;
                case ColossoState.Jump:
                    if (done) Land();
                    break;
            }
        }

        void StartWalk()
        {
            // Nunca dois pulos seguidos: só o rolamento abre a janela de dano.
            if (Phase >= 2 && !jumpedLast && Random.value < jumpChance)
            {
                jumpedLast = true;
                SetState(ColossoState.Crouch, .4f);
                return;
            }
            jumpedLast = false;
            FaceCasco();
            SetState(ColossoState.Walk, walkTime * Random.Range(.8f, 1.2f));
        }

        bool CascoClose(float distance)
        {
            var casco = Target;
            return casco != null && Mathf.Abs(casco.transform.position.x - transform.position.x) <= distance;
        }

        void FixedUpdate()
        {
            if (Body == null || Dying || Defeated) return;
            Vector2 position = Body.position;
            float dt = Time.fixedDeltaTime;
            hop = 0f;

            if (Fighting)
            {
                switch (State)
                {
                    case ColossoState.Walk:
                        FaceCasco();
                        if (!Blocked(position, direction, .3f)) position.x += direction * walkSpeed * dt;
                        break;
                    case ColossoState.Roll:
                        float speed = Phase >= 2 ? rollSpeedPhase2 : rollSpeed;
                        if (Blocked(position, direction, speed * dt + .05f)) HitWall();
                        else position.x += direction * speed * dt;
                        // Depois do ricochete ele vem quicando por um instante.
                        if (ricocheted) hop = Mathf.Max(0f, Mathf.Sin(Mathf.Clamp01(StateTime / .45f) * Mathf.PI)) * .8f;
                        break;
                    case ColossoState.Jump:
                        hop = Mathf.Sin(Mathf.Clamp01(StateTime / jumpTime) * Mathf.PI) * jumpHeight;
                        break;
                }
            }
            position.y = floorY + hop;
            Body.MovePosition(position);
            MoveWaves(dt);
        }

        // Tem parede logo à frente? Testa na altura do pneu, a partir da borda do corpo.
        bool Blocked(Vector2 position, float dir, float distance)
        {
            for (int i = 0; i < 2; i++)
            {
                var origin = position + new Vector2(dir * (halfWidth - .05f), .4f + i * 1.2f);
                if (Physics2D.Raycast(origin, new Vector2(dir, 0f), distance, groundMask).collider != null) return true;
            }
            return false;
        }

        void HitWall()
        {
            if (Phase >= 2 && !ricocheted)
            {
                // Fase 2: ricocheteia na primeira parede e volta rolando.
                ricocheted = true;
                direction = -direction;
                stateStart = Time.time;
                Director.Instance?.CameraRig.Shake(.2f, .2f);
                Sound?.Play(Sfx.Hit);
                return;
            }
            Director.Instance?.CameraRig.Shake(.4f, .45f);
            Sound?.Play(Sfx.Rumble);
            Effects.Spark(Center + new Vector2(direction * halfWidth, 0f));
            StartCoroutine(DropRocks(Phase >= 2 ? rocksPhase2 : rocksPhase1));
            ForceDizzy();
        }

        // Fica tonto agora (impacto na parede; o teste automático também usa).
        public void ForceDizzy()
        {
            stunBonuses = 0;
            starsAt = 0f;
            SetState(ColossoState.Dizzy, dizzyTime);
        }

        // Clarão da Lâmpada ou investida da Lata: só prolonga a tontura.
        public override void Stun(float seconds)
        {
            if (State != ColossoState.Dizzy || stunBonuses >= maxStunBonuses) return;
            stunBonuses++;
            stateLength += stunBonus;
        }

        protected override void OnPhaseChanged(int phase)
        {
            Sound?.Play(Sfx.Shock);
        }

        // ---------------------------------------------------------------- Pedras do teto

        IEnumerator DropRocks(int count)
        {
            var kit = Kit.Current;
            var room = Director.Instance != null ? Director.Instance.Room : null;
            if (kit == null || kit.spike == null) yield break;
            float width = room != null ? room.Width : 20f;
            float top = room != null ? room.Height - 1f : floorY + 10f;
            for (int i = 0; i < count; i++)
            {
                // Na fase 2 a primeira pedra mira Casco.
                var casco = Target;
                bool aimed = i == 0 && Phase >= 2 && casco != null;
                // Nasce logo abaixo do teto da sala, por cima das plataformas (que seguram as pedras e
                // servem de abrigo). Se o ponto cair dentro de pedra, desce até achar espaço livre.
                Vector2 spawn = Vector2.zero;
                bool found = false;
                for (int attempt = 0; attempt < 8 && !found; attempt++)
                {
                    float x = aimed && attempt == 0
                        ? Mathf.Clamp(casco.transform.position.x, 2.5f, width - 2.5f)
                        : Random.Range(2.5f, width - 2.5f);
                    for (float y = top - .4f; y > floorY + 2.5f && !found; y -= 1f)
                    {
                        spawn = new Vector2(x, y);
                        found = Physics2D.OverlapCircle(spawn, .3f, groundMask) == null;
                    }
                }
                if (!found) continue;
                Effects.Spark(spawn);
                yield return new WaitForSeconds(.25f);
                if (Defeated) yield break;
                var rock = Instantiate(kit.spike, spawn, Quaternion.identity, RoomParent);
                rock.lifetime = 4f;
                rock.Launch(Vector2.down * rockSpeed);
            }
        }

        // ---------------------------------------------------------------- Pulo e onda de choque

        void Land()
        {
            Director.Instance?.CameraRig.Shake(.35f, .35f);
            Sound?.Play(Sfx.Burst);
            for (int side = -1; side <= 1; side += 2)
            {
                var wave = new Wave { x = transform.position.x + side * halfWidth, dir = side };
                wave.visual = WaveVisual();
                waves.Add(wave);
            }
            SetState(ColossoState.Rise, riseTime);
        }

        Transform WaveVisual()
        {
            var kit = Kit.Current;
            var go = new GameObject("Onda de choque", typeof(SpriteRenderer));
            go.transform.SetParent(RoomParent, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = kit != null ? kit.white : null;
            renderer.color = new Color(.85f, .8f, .65f, .85f);
            renderer.sortingOrder = 44;
            go.transform.localScale = new Vector3(.5f, .35f, 1f);
            return go.transform;
        }

        void MoveWaves(float dt)
        {
            var casco = Target;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var wave = waves[i];
                float step = waveSpeed * dt;
                bool wall = Physics2D.Raycast(new Vector2(wave.x, floorY + .2f), new Vector2(wave.dir, 0f), step + .1f, groundMask).collider != null;
                wave.x += wave.dir * step;
                wave.travelled += step;
                if (wave.visual != null)
                {
                    wave.visual.position = new Vector3(wave.x, floorY + .17f, 0f);
                    float fade = 1f - wave.travelled / waveRange;
                    wave.visual.localScale = new Vector3(.5f, .35f * Mathf.Max(.3f, fade), 1f);
                }
                // Só pega quem está com os pés no chão: pular por cima é a saída.
                if (casco != null && !casco.Dead && casco.Grounded && Mathf.Abs(casco.transform.position.x - wave.x) < .5f
                    && casco.transform.position.y < floorY + .3f)
                    casco.Hurt(new Vector2(wave.x - wave.dir, floorY));
                if (wall || wave.travelled >= waveRange || Dying || Defeated)
                {
                    if (wave.visual != null) Destroy(wave.visual.gameObject);
                    waves.RemoveAt(i);
                }
            }
        }

        // ---------------------------------------------------------------- Visual

        protected override void Update()
        {
            base.Update();
            if (art == null || Dying || Defeated) return;
            Vector3 offset = Vector3.zero;
            switch (State)
            {
                case ColossoState.Idle:
                    art.sprite = Revealed ? (warnSprite != null ? warnSprite : Pick(walkFrames, 0f)) : Pick(walkFrames, 0f);
                    if (Revealed) offset = new Vector3(Mathf.Sin(Time.time * 60f), 0f, 0f) * .04f;
                    break;
                case ColossoState.Walk:
                    art.sprite = Pick(walkFrames, 4f);
                    break;
                case ColossoState.Warn:
                case ColossoState.Crouch:
                    art.sprite = warnSprite != null ? warnSprite : Pick(rollFrames, 0f);
                    offset = new Vector3(Mathf.Sin(Time.time * 70f), Mathf.Cos(Time.time * 53f), 0f) * .06f;
                    break;
                case ColossoState.Roll:
                    art.sprite = Pick(rollFrames, 14f);
                    break;
                case ColossoState.Jump:
                    art.sprite = Pick(rollFrames, 0f);
                    break;
                case ColossoState.Dizzy:
                    art.sprite = dizzySprite != null ? dizzySprite : Pick(walkFrames, 0f);
                    offset = new Vector3(Mathf.Sin(Time.time * 6f) * .08f, 0f, 0f);
                    break;
                case ColossoState.Rise:
                    art.sprite = Pick(walkFrames, 0f);
                    break;
            }
            art.transform.localPosition = artHome + offset;
            art.flipX = artFacesRight ? direction < 0f : direction > 0f;
        }

        Sprite Pick(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return art.sprite;
            int index = fps <= 0f ? 0 : Mathf.FloorToInt(Time.time * fps) % frames.Length;
            return frames[index] != null ? frames[index] : art.sprite;
        }

        // ---------------------------------------------------------------- Derrota

        protected override void OnDefeated()
        {
            foreach (var wave in waves) if (wave.visual != null) Destroy(wave.visual.gameObject);
            waves.Clear();
            var rare = Random.value < .5f ? ShellType.Lata : ShellType.Lampada;
            ShellPickup.Spawn(rare, -1, new Vector2(transform.position.x, floorY), RoomParent);
            Progress.Data.colossoDefeated = true;
            Progress.Save();
        }
    }
}
