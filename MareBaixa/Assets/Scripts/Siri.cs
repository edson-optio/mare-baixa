using UnityEngine;

namespace MareBaixa
{
    // Siri Saqueador: anda de lado pelo chão e persegue Casco. Perto, abre as garras (aviso) e dá
    // o agarrão: se Casco tem concha, o siri rouba e foge mais rápido com ela nas costas; sem concha,
    // o agarrão é um golpe normal. Apanhar, ser atordoado ou morrer carregando faz ele largar a
    // concha (GDD p. 8). O contato com o corpo não machuca: o perigo é o agarrão.
    //
    // A gravidade é própria, por raycast na layer Chao (corpo Kinematic), e ele não cai de beiradas.
    //
    // Prefab: SpriteRenderer (sorting 15, siri-1, pivô nos pés) + Rigidbody2D Kinematic +
    // BoxCollider2D trigger (0.8 × 0.5, offset 0, 0.25) + Siri > filho "Concha" com SpriteRenderer
    // (sorting 16, desligado). Preencher walkFrames (siri-1/2), claws (siri-garra), fleeing
    // (siri-fugindo) e carried (o filho).
    public class Siri : Enemy
    {
        enum Phase { Walk, Windup, Recover, Carry }

        [Header("Arte")]
        public Sprite[] walkFrames;
        public Sprite claws;
        public Sprite fleeing;
        public SpriteRenderer carried;
        public bool artFacesRight = true;

        [Header("Movimento")]
        public float patrolSpeed = 1.2f;
        public float chaseSpeed = 2.4f;
        public float fleeSpeed = 4f;
        public float patrolRange = 2f;
        public float sightRange = 6f;
        public float gravity = 30f;
        public float maxFallSpeed = 12f;

        [Header("Agarrão")]
        public float grabRange = 1.2f;
        public float windupTime = .4f;
        public float recoverTime = .8f;

        const float HalfWidth = .4f;
        const float Probe = .6f;

        Phase phase = Phase.Walk;
        float phaseStart;
        float direction = 1f;
        Vector2 feet;
        float fallSpeed;
        int groundMask;
        ShellType stolenType;
        int stolenHp;

        public bool Grabbing => phase == Phase.Windup;
        public bool Carrying => phase == Phase.Carry;
        public ShellType Stolen => Carrying ? stolenType : ShellType.Nenhuma;

        protected override bool ContactHurts => false;

        // Valores do GDD como padrão do componente; o que estiver salvo no prefab sobrescreve.
        public Siri()
        {
            maxHp = 3;
            pearlsMin = 2;
            pearlsMax = 4;
        }

        protected override void Awake()
        {
            base.Awake();
            groundMask = LayerMask.GetMask("Chao");
            feet = transform.position;
            direction = Random.value < .5f ? -1f : 1f;
            if (carried != null) carried.enabled = false;
        }

        // Teste automático: começa o agarrão agora (Casco precisa estar perto para ele pegar).
        public void ForceGrab()
        {
            if (phase == Phase.Walk) SetPhase(Phase.Windup);
        }

        // ---------------------------------------------------------------- Movimento

        protected override Vector2 Steer(float deltaTime)
        {
            // Os pés são o estado de verdade; o recuo dos golpes fica só por cima, na base.
            float speed = 0f;
            var casco = Target;
            bool cascoNear = casco != null && !casco.Dead && Mathf.Abs(casco.transform.position.y - feet.y) < 2f
                             && Vector2.Distance(casco.transform.position, feet) <= sightRange;
            switch (phase)
            {
                case Phase.Walk:
                    if (cascoNear)
                    {
                        float dx = casco.transform.position.x - feet.x;
                        if (Mathf.Abs(dx) > .2f) direction = Mathf.Sign(dx);
                        speed = Mathf.Abs(dx) > grabRange * .6f ? chaseSpeed : 0f;
                    }
                    else
                    {
                        // Patrulha em volta de casa.
                        float fromHome = feet.x - Home.x;
                        if ((direction > 0f && fromHome >= patrolRange) || (direction < 0f && fromHome <= -patrolRange))
                            direction = -direction;
                        speed = patrolSpeed;
                    }
                    break;
                case Phase.Carry:
                    // Foge para o lado oposto de Casco.
                    if (casco != null && Mathf.Abs(casco.transform.position.x - feet.x) > .1f)
                        direction = casco.transform.position.x > feet.x ? -1f : 1f;
                    speed = fleeSpeed;
                    break;
            }

            if (speed > 0f)
            {
                if (Blocked(direction))
                {
                    // Na patrulha vira; correndo atrás ou fugindo, fica parado na beirada.
                    if (phase == Phase.Walk && !cascoNear) direction = -direction;
                }
                else feet.x += direction * speed * deltaTime;
            }

            Fall(deltaTime);
            return feet;
        }

        // Atordoado não anda, mas continua caindo (da beirada, por exemplo).
        protected override Vector2 WhileStunned(Vector2 last, float deltaTime)
        {
            Fall(deltaTime);
            return feet;
        }

        // Parede logo à frente ou buraco no próximo passo.
        bool Blocked(float side)
        {
            Vector2 chest = feet + new Vector2(0f, .3f);
            if (Physics2D.Raycast(chest, new Vector2(side, 0f), HalfWidth + .1f, groundMask).collider != null) return true;
            Vector2 ahead = feet + new Vector2(side * (HalfWidth + .1f), .1f);
            return Physics2D.Raycast(ahead, Vector2.down, Probe, groundMask).collider == null && Grounded();
        }

        bool Grounded() => Physics2D.Raycast(feet + new Vector2(0f, .1f), Vector2.down, .15f, groundMask).collider != null;

        void Fall(float deltaTime)
        {
            var hit = Physics2D.Raycast(feet + new Vector2(0f, .1f), Vector2.down, .1f + fallSpeed * deltaTime + .05f, groundMask);
            if (hit.collider != null && fallSpeed >= 0f)
            {
                feet.y = hit.point.y;
                fallSpeed = 0f;
                return;
            }
            fallSpeed = Mathf.Min(fallSpeed + gravity * deltaTime, maxFallSpeed);
            feet.y -= fallSpeed * deltaTime;
        }

        // ---------------------------------------------------------------- Agarrão

        protected override void Update()
        {
            float elapsed = Time.time - phaseStart;
            if (Stunned) phaseStart += Time.deltaTime;
            else
            {
                switch (phase)
                {
                    case Phase.Walk:
                        if (CascoInReach(grabRange)) SetPhase(Phase.Windup);
                        break;
                    case Phase.Windup:
                        if (elapsed >= windupTime) Grab();
                        break;
                    case Phase.Recover:
                        if (elapsed >= recoverTime) SetPhase(Phase.Walk);
                        break;
                }
            }

            if (phase == Phase.Carry) Renderer.sprite = fleeing != null ? fleeing : Frame(walkFrames, 10f);
            else if (phase == Phase.Windup) Renderer.sprite = claws;
            else Renderer.sprite = Frame(walkFrames, phase == Phase.Walk ? 6f : 2f);
            Renderer.flipX = artFacesRight ? direction < 0f : direction > 0f;
            if (carried != null)
            {
                carried.enabled = phase == Phase.Carry;
                carried.flipX = Renderer.flipX;
            }
            base.Update();
        }

        bool CascoInReach(float range)
        {
            var casco = Target;
            if (casco == null || casco.Dead) return false;
            Vector2 offset = (Vector2)casco.transform.position - feet;
            return Mathf.Abs(offset.x) <= range && Mathf.Abs(offset.y) <= .9f;
        }

        void Grab()
        {
            var casco = Target;
            if (CascoInReach(grabRange + .2f))
            {
                if (casco.StealShell(out ShellType type, out int hp))
                {
                    stolenType = type;
                    stolenHp = hp;
                    if (carried != null && Kit.Current != null) carried.sprite = Kit.Current.ShellSprite(type);
                    Sound?.Play(Sfx.Equip);
                    Effects.Spark(casco.Center);
                    Director.Instance?.CameraRig.Shake(.12f, .15f);
                    SetPhase(Phase.Carry);
                    return;
                }
                // Sem concha para levar, a garra belisca.
                if (casco.Shell == ShellType.Nenhuma) casco.Hurt(transform.position);
            }
            SetPhase(Phase.Recover);
        }

        // Larga a concha roubada no chão, com a resistência que ela tinha.
        void DropShell()
        {
            if (phase != Phase.Carry) return;
            var room = Director.Instance != null ? Director.Instance.Room : null;
            ShellPickup.Spawn(stolenType, stolenHp, feet, room != null ? room.transform : transform.parent);
            Effects.Spark(feet + new Vector2(0f, .4f));
            stolenType = ShellType.Nenhuma;
            SetPhase(Phase.Recover);
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        protected override void OnHurt(Vector2 from) => DropShell();
        protected override void OnStunned() => DropShell();

        protected override void Die()
        {
            DropShell();
            base.Die();
        }

        // Carregando a concha ele só quer fugir: o barulho não chama.
        protected override void OnAlert(Vector2 origin)
        {
            if (phase != Phase.Carry) base.OnAlert(origin);
        }
    }
}
