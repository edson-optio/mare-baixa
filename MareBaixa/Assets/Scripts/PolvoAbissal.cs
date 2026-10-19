using System.Collections;
using UnityEngine;

namespace MareBaixa
{
    // Polvo Abissal, o chefe final na Fossa Abissal (GDD p. 8). O corpo fica no fundo da arena, sem
    // colisão; os alvos são os dois olhos (PolvoOlho), que só apanham abertos. A vida do Polvo é a
    // soma da vida dos olhos (6 + 6), e a luta tem três fases:
    //   1 (12→8): tentáculos saem do chão em sequência perto de Casco; entre as ondas, os olhos abrem.
    //   2 (8→4):  jorra tinta, a tela escurece (só os olhos e a luz de Casco aparecem), tentáculos de parede.
    //   3 (4→0):  a água sobe rápido; os olhos ficam abertos mais tempo. Ao vencer, a água recua.
    //
    // Prefab: raiz (no ponto B, pés) com Rigidbody2D Kinematic + PolvoAbissal > filho "Corpo"
    // (SpriteRenderer, sorting -5, sem collider) e os filhos "OlhoEsquerdo"/"OlhoDireito" (PolvoOlho).
    //
    // Equilíbrio (meta do GDD p. 6: vencer entre a 5ª e a 10ª run). Números finais:
    //   Vida 12 (olhos 6 + 6), 4 por fase. Golpe 1 (recarga 0.32 s), giro/investida 2: numa janela
    //   aberta o jogador médio tira 3–4, então ~2 janelas por fase e luta de 60–80 s.
    //   F1: ondas de 3–4 tentáculos (aviso 0.8 s, 0.55 s entre eles), olhos abertos 2.5 s (ciclo ~7 s).
    //   F2: escuridão 0.92 em 1.5 s; 2 leques de 4 tintas a 3.2 u/s (fácil de ver e desviar) + 1
    //       tentáculo (parede ou chão) por ciclo; olhos abertos 2.5 s.
    //   F3: maré a 0.55 u/s, escuridão 0.85, olhos abertos 3.5 s com 3 tintas a cada 1.5 s.
    //       Os olhos sobem junto com a água (ficam ~2 blocos acima dela, até 4 abaixo do teto), então o
    //       jogador sobe as plataformas atrás deles em vez de lutar afogado (Casco tem 8 s de ar).
    //       Cada olho também desliza para 1.2 da borda da coluna de plataformas "=" do lado dele (no
    //       Covil do Polvo: x 11.2 e 20.8), para dar para acertar de cima delas.
    //   Clarão da Lâmpada (Stun) com os olhos abertos: +1 s de janela, uma vez por abertura.
    //   Olhos piscam 2× (0.32 s) antes de abrir: é o aviso da janela.
    public class PolvoAbissal : Boss
    {
        [Header("Partes")]
        public SpriteRenderer body;
        public PolvoOlho leftEye;
        public PolvoOlho rightEye;
        public JatoDeTinta inkPrefab;

        [Header("Fase 1: tentáculos do chão")]
        public float tentacleWarning = .8f;
        public float tentacleGap = .55f;
        public float openTimePhase1 = 2.5f;

        [Header("Fase 2: tinta e escuridão")]
        public float darkness = .92f;
        public float darkenTime = 1.5f;
        public float inkSpeed = 3.2f;
        public int inkPerVolley = 4;
        public float openTimePhase2 = 2.5f;

        [Header("Fase 3: maré")]
        public float tideSpeed = .55f;
        public float recedeSpeed = -2f;
        public float openTimePhase3 = 3.5f;
        public float eyesAboveWater = 1f;   // a raiz fica isto acima da água (os olhos, +1)
        public float riseSpeed = 1.2f;
        public float stunOpenBonus = 1f;
        public float eyeGap = 1.2f;         // fase 3: distância do olho até a borda da plataforma

        Coroutine routine;
        int routinePhase;
        Vector3 bodyHome;
        float homeY;
        float openBonus;
        bool bonusUsed;
        float leftEyeX = -3f;
        float rightEyeX = 3f;

        public PolvoAbissal()
        {
            DisplayName = "Polvo Abissal";
            MaxHp = 12;
            wakeDistance = 12f;
        }

        protected override void Awake()
        {
            base.Awake();
            if (leftEye != null) leftEye.boss = this;
            if (rightEye != null) rightEye.boss = this;
            // A vida é a soma dos olhos.
            MaxHp = (leftEye != null ? leftEye.maxHp : 0) + (rightEye != null ? rightEye.maxHp : 0);
            if (MaxHp <= 0) MaxHp = 12;
            Hp = MaxHp;
            if (body != null) bodyHome = body.transform.localPosition;
            homeY = transform.position.y;
            if (leftEye != null) leftEyeX = leftEye.transform.localPosition.x;
            if (rightEye != null) rightEyeX = rightEye.transform.localPosition.x;
        }

        protected override bool Vulnerable => (leftEye != null && leftEye.Open) || (rightEye != null && rightEye.Open);
        protected override Vector2 Center => (Vector2)transform.position + new Vector2(0f, 1.2f);
        // A boca fica na altura dos olhos, para a tinta não bater logo no piso.
        Vector2 Mouth => (Vector2)transform.position + new Vector2(0f, 1f);

        protected override int PhaseFor(int hp) => hp > MaxHp * 2 / 3 ? 1 : hp > MaxHp / 3 ? 2 : 3;

        // O próprio corpo não tem collider; se algo chamar Hit na raiz, conta como golpe fora da janela.
        public override void Hit(int damage, Vector2 from) => Tink(from);

        public void HitEye(PolvoOlho eye, int damage, Vector2 from)
        {
            if (Dying || Defeated) return;
            if (!Fighting || eye == null || !eye.Open)
            {
                Tink(from);
                return;
            }
            Damage(eye.TakeDamage(damage), from);
        }

        protected override void OnReveal()
        {
            // "Dois olhos enormes se abrem": abre durante a apresentação e fecha antes de atacar.
            StartCoroutine(IntroBlink());
        }

        IEnumerator IntroBlink()
        {
            SetEyes(true);
            yield return new WaitForSeconds(introTime * .8f);
            SetEyes(false);
        }

        protected override void Think()
        {
            if (routine == null || routinePhase != Phase) StartPhase(Phase);
        }

        protected override void OnPhaseChanged(int phase)
        {
            Sound?.Play(Sfx.Shock);
            StartPhase(phase);
        }

        void StartPhase(int phase)
        {
            if (routine != null) StopCoroutine(routine);
            SetEyes(false);
            routinePhase = phase;
            routine = StartCoroutine(phase == 1 ? PhaseOne() : phase == 2 ? PhaseTwo() : PhaseThree());
        }

        // Pula para uma fase acertando a vida dos olhos (teste automático).
        public void ForcePhase(int phase)
        {
            phase = Mathf.Clamp(phase, 1, 3);
            if (!Revealed) SkipIntro();
            int target = phase == 1 ? MaxHp : phase == 2 ? MaxHp * 2 / 3 : MaxHp / 3;
            int left = (target + 1) / 2;
            leftEye?.SetHp(left);
            rightEye?.SetHp(target - left);
            Hp = target;
            Phase = phase;
            OnPhaseChanged(phase);
        }

        public bool EyesOpen => Vulnerable;

        void SetEyes(bool open)
        {
            leftEye?.SetOpen(open);
            rightEye?.SetOpen(open);
            // Os dois cegos com vida sobrando não acontece: a vida do Polvo é a soma dos olhos.
        }

        // ---------------------------------------------------------------- Fases

        IEnumerator PhaseOne()
        {
            while (true)
            {
                yield return new WaitForSeconds(.6f);
                int count = Random.Range(3, 5);
                for (int i = 0; i < count; i++)
                {
                    FloorTentacle(.6f);
                    yield return new WaitForSeconds(tentacleGap);
                }
                yield return new WaitForSeconds(tentacleWarning + .4f);
                yield return OpenEyes(openTimePhase1, 0f);
            }
        }

        IEnumerator PhaseTwo()
        {
            Darken(darkness);
            yield return new WaitForSeconds(darkenTime);
            while (true)
            {
                yield return InkVolley();
                yield return new WaitForSeconds(.7f);
                if (Random.value < .5f) WallTentacle();
                else FloorTentacle(.8f);
                yield return new WaitForSeconds(.7f);
                yield return InkVolley();
                yield return new WaitForSeconds(tentacleWarning + .3f);
                yield return OpenEyes(openTimePhase2, 0f);
            }
        }

        IEnumerator PhaseThree()
        {
            Darken(Mathf.Min(darkness, .85f));
            RaiseTide();
            SpreadEyesToPlatforms();
            yield return new WaitForSeconds(.8f);
            while (true)
            {
                // Abertos mais tempo, mas cuspindo tinta enquanto isso.
                yield return OpenEyes(openTimePhase3, 1.5f);
                yield return new WaitForSeconds(.4f);
                if (Random.value < .5f) WallTentacle();
                else FloorTentacle(.6f);
                yield return new WaitForSeconds(1.1f);
            }
        }

        IEnumerator OpenEyes(float seconds, float inkEvery)
        {
            // Pisca rápido antes de abrir, como aviso.
            for (int i = 0; i < 2; i++)
            {
                SetEyes(true);
                yield return new WaitForSeconds(.08f);
                SetEyes(false);
                yield return new WaitForSeconds(.08f);
            }
            SetEyes(true);
            openBonus = 0f;
            bonusUsed = false;
            float nextInk = inkEvery > 0f ? Time.time + inkEvery : float.MaxValue;
            for (float start = Time.time; Time.time < start + seconds + openBonus;)
            {
                if (Time.time >= nextInk)
                {
                    Volley(inkPerVolley - 1);
                    nextInk = Time.time + inkEvery;
                }
                yield return null;
            }
            SetEyes(false);
        }

        // Clarão da Lâmpada com os olhos abertos: segura a janela aberta mais um pouco.
        public override void Stun(float seconds)
        {
            if (!Fighting || !Vulnerable || bonusUsed) return;
            bonusUsed = true;
            openBonus = stunOpenBonus;
        }

        // Fase 3: cada olho vai para perto da borda da coluna de plataformas ("=") mais próxima do
        // lado dele, para Casco conseguir bater de cima delas enquanto sobe (Casco não nada).
        void SpreadEyesToPlatforms()
        {
            var room = Director.Instance != null ? Director.Instance.Room : null;
            if (room == null) return;
            int center = Mathf.FloorToInt(transform.position.x - room.transform.position.x);
            int fromY = Mathf.FloorToInt(homeY) + 1;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int x = center; x >= 0 && x < room.Width; x += side)
                {
                    bool platform = false;
                    for (int y = fromY; y < room.Height && !platform; y++) platform = room.At(x, y) == '=';
                    if (!platform) continue;
                    // Borda da plataforma do lado do Polvo, mais um pouco para dentro do vão.
                    float edge = room.transform.position.x + (side < 0 ? x + 1f : x);
                    float local = edge - side * eyeGap - transform.position.x;
                    if (side < 0) leftEyeX = Mathf.Min(local, -1f);
                    else rightEyeX = Mathf.Max(local, 1f);
                    break;
                }
            }
        }

        void MoveEye(PolvoOlho eye, float x)
        {
            if (eye == null) return;
            var local = eye.transform.localPosition;
            local.x = Mathf.MoveTowards(local.x, x, riseSpeed * 2f * Time.fixedDeltaTime);
            eye.transform.localPosition = local;
        }

        // Fase 3: os olhos acompanham a água subindo, para o jogador subir as plataformas atrás deles.
        void FixedUpdate()
        {
            if (Body == null || Dying || Defeated || Phase < 3) return;
            MoveEye(leftEye, leftEyeX);
            MoveEye(rightEye, rightEyeX);
            if (Mare.Current == null) return;
            var room = Director.Instance != null ? Director.Instance.Room : null;
            float ceiling = room != null ? room.Height - 4f : float.MaxValue;
            float goal = Mathf.Clamp(Mare.Current.Level + eyesAboveWater, homeY, Mathf.Max(homeY, ceiling));
            var position = Body.position;
            position.y = Mathf.MoveTowards(position.y, goal, riseSpeed * Time.fixedDeltaTime);
            Body.MovePosition(position);
        }

        // ---------------------------------------------------------------- Ataques

        // Tentáculo saindo do chão perto de Casco (com aviso).
        void FloorTentacle(float spread)
        {
            var casco = Target;
            if (casco == null || casco.Dead) return;
            float x = ClampX(casco.transform.position.x + Random.Range(-spread, spread));
            Vector2 feet = FloorBelow(new Vector2(x, casco.Center.y + .2f));
            TentaculoSombrio.Spawn(feet, Vector2.up, RoomParent, true)?.Emerge(tentacleWarning);
        }

        // Tentáculo saindo da parede mais próxima de Casco, na altura dele.
        void WallTentacle()
        {
            var casco = Target;
            if (casco == null || casco.Dead) return;
            int mask = LayerMask.GetMask("Chao");
            var left = Physics2D.Raycast(casco.Center, Vector2.left, 10f, mask);
            var right = Physics2D.Raycast(casco.Center, Vector2.right, 10f, mask);
            RaycastHit2D wall = left.collider == null ? right
                : right.collider == null ? left
                : left.distance <= right.distance ? left : right;
            if (wall.collider == null || wall.distance < .3f)
            {
                FloorTentacle(.8f);
                return;
            }
            TentaculoSombrio.Spawn(wall.point, wall.normal, RoomParent, true)?.Emerge(tentacleWarning);
        }

        IEnumerator InkVolley()
        {
            Volley(inkPerVolley);
            yield return new WaitForSeconds(.25f);
        }

        // Leque de bolotas de tinta mirando em Casco.
        void Volley(int count)
        {
            var casco = Target;
            if (inkPrefab == null || casco == null || casco.Dead || count <= 0) return;
            Vector2 aim = casco.Center - Mouth;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            const float spread = 18f;
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + (i - (count - 1) * .5f) * spread;
                var heading = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var ink = Instantiate(inkPrefab, Mouth + heading * .4f, Quaternion.identity, RoomParent);
                ink.Launch(heading * inkSpeed * Random.Range(.85f, 1.15f));
            }
            Sound?.Play(Sfx.Bubble);
        }

        float ClampX(float x)
        {
            var room = Director.Instance != null ? Director.Instance.Room : null;
            return room != null ? Mathf.Clamp(x, 1.5f, room.Width - 1.5f) : x;
        }

        // ---------------------------------------------------------------- Escuridão e maré

        static void Darken(float amount)
        {
            var dark = Escuridao.Current != null ? Escuridao.Current : Escuridao.Create(0f);
            dark.SetDarkness(amount, 1.5f);
        }

        void RaiseTide()
        {
            if (Mare.Current != null)
            {
                Mare.Current.Speed = tideSpeed;
                return;
            }
            // Arena sem maré: cria uma agora, já subindo.
            var room = Director.Instance != null ? Director.Instance.Room : null;
            var kit = Kit.Current;
            if (room != null && kit != null)
                Mare.Create(room.transform, kit, room.Width, room.Height, room.FloorY, tideSpeed);
        }

        IEnumerator Recede()
        {
            var mare = Mare.Current;
            if (mare == null) yield break;
            mare.Speed = recedeSpeed;
            while (mare != null && mare.Level > mare.Bottom) yield return null;
            if (mare == null) yield break;
            mare.Speed = 0f;
            mare.SetLevel(mare.Bottom);
        }

        // ---------------------------------------------------------------- Visual e derrota

        protected override void Update()
        {
            base.Update();
            if (Dying && routine != null)
            {
                StopCoroutine(routine);
                routine = null;
                SetEyes(false);
            }
            // O corpo respira devagar no fundo.
            if (body != null && !Dying && !Defeated)
                body.transform.localPosition = bodyHome + new Vector3(0f, Mathf.Sin(Time.time * 1.3f) * .12f, 0f);
        }

        protected override void OnDefeated()
        {
            StartCoroutine(Recede());
            Escuridao.Current?.SetDarkness(.4f, 2f);
            // Depois da vitória nada mais machuca: some com os tentáculos e a tinta que sobraram.
            foreach (var tentacle in FindObjectsByType<TentaculoSombrio>(FindObjectsSortMode.None)) Destroy(tentacle.gameObject);
            foreach (var ink in FindObjectsByType<JatoDeTinta>(FindObjectsSortMode.None)) Destroy(ink.gameObject);
        }
    }
}
