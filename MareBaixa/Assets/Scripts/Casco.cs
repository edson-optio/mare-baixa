using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Casco, o protagonista: anda, pula, belisca (J), rola dentro da concha (K), usa a habilidade
    // da concha (L: pulo duplo, giro, investida ou clarão), interage (E) e come algas (Q). A concha é a armadura: cada golpe tira 1 ponto
    // de resistência e, sem concha, um golpe mata (GDD p. 2 e 7).
    //
    // Hierarquia: Casco (Rigidbody2D + BoxCollider2D) > Visual (gira e espelha) > Corpo e Concha.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class Casco : MonoBehaviour
    {
        [Header("Partes")]
        public Transform visual;
        public SpriteRenderer bodyRenderer;
        public SpriteRenderer shellRenderer;

        [Header("Animações")]
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite jumpFrame;
        public Sprite fallFrame;
        public Sprite[] attackFrames;

        // Quadros em que o corpo sobe 1 px; a concha acompanha.
        static readonly int[] IdleBob = { 0, 1 };
        static readonly int[] WalkBob = { 0, 1, 0, 1 };

        public Rigidbody2D Body { get; private set; }
        public BoxCollider2D Hitbox { get; private set; }

        public ShellType Shell { get; private set; }
        public int ShellHp { get; private set; }
        public int ShellMaxHp => Shells.Get(Shell).Resistance;
        public int NakedGuards { get; set; }
        public int Algae { get; private set; }
        public float Air { get; private set; }
        public float MaxAir { get; set; } = GameRules.BaseAir;
        public bool Grounded { get; private set; }
        public bool InWater { get; private set; }
        public bool HeadUnderwater { get; private set; }
        public bool Dodging => Time.time < dodgeUntil;
        public bool Spinning => Time.time < spinUntil;
        public bool Charging => Time.time < chargeUntil;
        public float LightRadius => Shell == ShellType.Lampada ? GameRules.LampLightRadius : GameRules.LightRadius;
        public bool Invulnerable => Dodging || Charging || Time.time < invulnerableUntil;
        public bool Dead { get; private set; }
        public float Facing { get; private set; } = 1f;
        public bool CanTakeDamage { get; set; } = true;   // a vila desliga
        public Interactable Focus { get; private set; }

        public Vector2 Center => (Vector2)transform.position + new Vector2(0f, .35f);
        public Vector2 Head => (Vector2)transform.position + new Vector2(0f, .75f);

        GameAudio Sound => Director.Instance != null ? Director.Instance.Sound : null;
        bool Locked => Dead || (Director.Instance != null && Director.Instance.InputLocked);

        ContactFilter2D groundFilter;
        ContactFilter2D hitFilter;
        readonly List<Collider2D> overlaps = new List<Collider2D>();
        readonly HashSet<IHittable> hitThisSwing = new HashSet<IHittable>();

        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        float attackReadyAt, attackUntil, spinReadyAt, spinUntil;
        float dodgeReadyAt, dodgeUntil;
        float chargeReadyAt, chargeUntil, flashReadyAt;
        Vector2 push, lastPush;
        float invulnerableUntil, knockbackUntil, trembleUntil, dropUntil;
        bool extraJumpUsed;
        bool airJumpRequested;
        bool cutJump;
        bool lockedLastFrame;
        float move;
        float animationClock;
        Sprite[] currentAnimation;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Hitbox = GetComponent<BoxCollider2D>();
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(LayerMask.GetMask("Chao"));
            groundFilter.SetNormalAngle(45f, 135f);
            hitFilter = new ContactFilter2D { useTriggers = true };
            hitFilter.SetLayerMask(~LayerMask.GetMask("Chao"));
            Air = MaxAir;
        }

        // ---------------------------------------------------------------- Concha

        public void Equip(ShellType type, int hp)
        {
            Shell = type;
            ShellHp = type == ShellType.Nenhuma ? 0 : Mathf.Clamp(hp < 0 ? ShellMaxHp : hp, 1, ShellMaxHp);
            shellRenderer.sprite = Kit.Current != null ? Kit.Current.ShellSprite(type) : null;
            shellRenderer.enabled = type != ShellType.Nenhuma;
        }

        // Troca pela concha do chão; a antiga fica para trás com a resistência que tinha.
        public void SwapShell(ShellPickup pickup)
        {
            ShellType oldType = Shell;
            int oldHp = ShellHp;
            Equip(pickup.type, pickup.hp);
            Destroy(pickup.gameObject);
            Director.Instance?.OnShellEquipped(pickup.type);
            // A concha largada pertence à sala, para sumir junto quando a sala for trocada.
            var room = Director.Instance != null ? Director.Instance.Room : null;
            if (oldType != ShellType.Nenhuma)
                ShellPickup.Spawn(oldType, oldHp, transform.position, room != null ? room.transform : null);
            Sound?.Play(Sfx.Equip);
            Effects.Spark(Center);
        }

        void BreakShell()
        {
            Effects.Shards(Center, Shell);
            Equip(ShellType.Nenhuma, 0);
            trembleUntil = Time.time + 1.5f;
            Sound?.Play(Sfx.ShellBreak);
            Director.Instance?.CameraRig.Shake(.25f, .3f);
            Director.Instance?.Hud.Flash();
        }

        // O Siri Saqueador leva a concha inteira, sem quebrar (GDD p. 8).
        public bool StealShell(out ShellType type, out int hp)
        {
            type = Shell;
            hp = ShellHp;
            if (Dead || Shell == ShellType.Nenhuma || Invulnerable || !CanTakeDamage) return false;
            Equip(ShellType.Nenhuma, 0);
            trembleUntil = Time.time + 1.5f;
            invulnerableUntil = Time.time + GameRules.InvulnerableTime;
            Sound?.Play(Sfx.Steal);
            Director.Instance?.Hud.Flash();
            return true;
        }

        // Empurrão de fora (correnteza, varrida de tentáculo), somado ao movimento só neste passo de física.
        public void Push(Vector2 velocity) => push += velocity;

        // ---------------------------------------------------------------- Itens

        public bool AddAlga()
        {
            if (Algae >= GameRules.MaxAlgae) return false;
            Algae++;
            return true;
        }

        public void SetAlgae(int count) => Algae = Mathf.Clamp(count, 0, GameRules.MaxAlgae);

        public void RefillAir()
        {
            Air = MaxAir;
            Sound?.Play(Sfx.Bubble);
        }

        void EatAlga()
        {
            if (Algae <= 0 || Shell == ShellType.Nenhuma || ShellHp >= ShellMaxHp)
            {
                Sound?.Play(Sfx.Error);
                return;
            }
            Algae--;
            ShellHp++;
            Sound?.Play(Sfx.Eat);
            Effects.Spark(Center);
        }

        // ---------------------------------------------------------------- Dano

        public void Hurt(Vector2 source)
        {
            if (Dead || Invulnerable || !CanTakeDamage) return;
            float direction = transform.position.x >= source.x ? 1f : -1f;
            Body.linearVelocity = new Vector2(direction * 6f, 7f);
            knockbackUntil = Time.time + .2f;
            invulnerableUntil = Time.time + GameRules.InvulnerableTime;
            Effects.HitStop(.06f);

            if (Shell != ShellType.Nenhuma)
            {
                ShellHp--;
                if (ShellHp <= 0) BreakShell();
                else
                {
                    Sound?.Play(Sfx.Crack);
                    Director.Instance?.CameraRig.Shake(.12f, .15f);
                }
            }
            else if (NakedGuards > 0)
            {
                NakedGuards--;
                trembleUntil = Time.time + 1f;
                Sound?.Play(Sfx.Hurt);
            }
            else Die("Sem concha, um golpe basta.");
        }

        public void Die(string reason)
        {
            if (Dead) return;
            Dead = true;
            Body.linearVelocity = new Vector2(0f, 6f);
            Hitbox.enabled = false;
            Body.gravityScale = GameRules.GravityScale * .5f;
            Sound?.Play(Sfx.Death);
            Director.Instance?.CameraRig.Shake(.3f, .4f);
            Director.Instance?.OnCascoDied(reason);
        }

        // ---------------------------------------------------------------- Laço

        void Update()
        {
            UpdateWater();
            // No quadro em que um painel fecha, o mesmo E não pode reabrir a conversa.
            bool locked = Locked;
            if (!locked && !lockedLastFrame) ReadActions();
            else move = 0f;
            lockedLastFrame = locked;
            UpdateAttack();
            Animate();
        }

        void ReadActions()
        {
            move = Controls.Move;
            if (Controls.Pressed(GameAction.Jump))
            {
                if (Controls.Down && IsOnPlatform()) DropThroughPlatform();
                else RequestJump();
            }
            if (Controls.Pressed(GameAction.Attack)) StartAttack();
            if (Controls.Pressed(GameAction.Dodge)) StartDodge();
            if (Controls.Pressed(GameAction.Ability)) UseAbility();
            if (Controls.Pressed(GameAction.Eat)) EatAlga();

            Focus = Interactable.Closest(Center);
            if (Focus != null && Controls.Pressed(GameAction.Interact)) Focus.Interact(this);
        }

        // No chão (ou no coyote time) o aperto fica guardado no buffer; no ar ele vira o pulo duplo
        // da Tampinha, para o buffer perto do chão não gastar o pulo duplo à toa.
        void RequestJump()
        {
            bool canGroundJump = Time.time - lastGroundedTime <= GameRules.CoyoteTime;
            if (!canGroundJump && Shell == ShellType.Tampinha && !extraJumpUsed) airJumpRequested = true;
            else lastJumpPressedTime = Time.time;
        }

        void UpdateWater()
        {
            var mare = Mare.Current;
            InWater = mare != null && mare.IsUnder(Center.y);
            bool head = mare != null && mare.IsUnder(Head.y);
            if (head != HeadUnderwater) Sound?.SetMuffled(head);
            HeadUnderwater = head;
            if (Dead) return;

            if (HeadUnderwater && !Locked)
            {
                Air -= Time.deltaTime;
                if (Air <= 0f)
                {
                    Air = 0f;
                    Die("O ar acabou debaixo d'água.");
                }
            }
            else Air = Mathf.Min(MaxAir, Air + GameRules.AirRefillPerSecond * Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (Dead) return;
            Grounded = Body.IsTouching(groundFilter) && Body.linearVelocity.y <= .01f;
            if (Grounded)
            {
                lastGroundedTime = Time.time;
                extraJumpUsed = false;
                if (Body.linearVelocity.y <= .01f) cutJump = false;
            }

            float waterMove = InWater ? GameRules.WaterMoveFactor : 1f;
            Body.gravityScale = GameRules.GravityScale * (InWater ? GameRules.WaterGravityFactor : 1f);
            Vector2 velocity = Body.linearVelocity;

            // O empurrão do passo anterior sai antes de acelerar, para a correnteza dar uma deriva
            // constante em vez de acumular velocidade a cada passo.
            velocity -= lastPush;
            if (Charging)
                velocity.x = Facing * GameRules.ChargeSpeed * waterMove;
            else if (Dodging)
                velocity.x = Facing * GameRules.DodgeSpeed * waterMove;
            else if (Time.time >= knockbackUntil)
            {
                float speed = GameRules.MoveSpeed * Shells.Get(Shell).SpeedFactor * waterMove;
                velocity.x = Mathf.MoveTowards(velocity.x, move * speed, GameRules.Acceleration * Time.fixedDeltaTime);
            }

            float jumpSpeed = GameRules.JumpSpeed * (InWater ? GameRules.WaterJumpFactor : 1f);
            bool wantsJump = Time.time - lastJumpPressedTime <= GameRules.JumpBufferTime;
            if (wantsJump && Time.time - lastGroundedTime <= GameRules.CoyoteTime)
                Jump(ref velocity, jumpSpeed);
            else if (airJumpRequested && Shell == ShellType.Tampinha && !extraJumpUsed)
            {
                extraJumpUsed = true;   // pulo duplo da Tampinha
                Jump(ref velocity, jumpSpeed * .9f);
                Effects.Spark(transform.position);
            }
            airJumpRequested = false;

            // Soltar o Espaço na subida corta a altura (pulo curto x pulo alto). Só vale para pulo
            // do Espaço: o pulo pelo L e o empurrão de um golpe não são cortados.
            if (cutJump && !Controls.Held(GameAction.Jump) && velocity.y > jumpSpeed * .4f && !Dodging)
                velocity.y = jumpSpeed * .4f;
            velocity.y = Mathf.Max(velocity.y, -(InWater ? GameRules.WaterMaxFallSpeed : GameRules.MaxFallSpeed));
            velocity += push;
            lastPush = push;
            push = Vector2.zero;
            Body.linearVelocity = velocity;
        }

        void Jump(ref Vector2 velocity, float speed)
        {
            velocity.y = speed;
            cutJump = Controls.Held(GameAction.Jump);
            lastGroundedTime = lastJumpPressedTime = -10f;
            Sound?.Play(Sfx.Jump);
        }

        bool IsOnPlatform()
        {
            var room = Director.Instance != null ? Director.Instance.Room : null;
            return Grounded && room != null && room.Platforms != null && Hitbox.IsTouching(room.Platforms);
        }

        void DropThroughPlatform()
        {
            var platforms = Director.Instance.Room.Platforms;
            Physics2D.IgnoreCollision(Hitbox, platforms, true);
            dropUntil = Time.time + .3f;
        }

        // ---------------------------------------------------------------- Ações

        float AttackFactor => InWater ? GameRules.WaterAttackFactor : 1f;

        void StartAttack()
        {
            if (Time.time < attackReadyAt || Dodging) return;
            attackReadyAt = Time.time + GameRules.AttackCooldown * AttackFactor;
            attackUntil = Time.time + GameRules.AttackActiveTime;
            hitThisSwing.Clear();
            Sound?.Play(Sfx.Attack);
        }

        void StartDodge()
        {
            if (Time.time < dodgeReadyAt) return;
            if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
            dodgeUntil = Time.time + GameRules.DodgeTime;
            dodgeReadyAt = Time.time + GameRules.DodgeCooldown;
            Sound?.Play(Sfx.Dodge);
        }

        void UseAbility()
        {
            switch (Shell)
            {
                case ShellType.Tampinha:
                    RequestJump();   // L também pula (e faz o pulo duplo no ar)
                    break;
                case ShellType.Espiral:
                    if (Time.time < spinReadyAt) return;
                    spinReadyAt = Time.time + GameRules.SpinCooldown * AttackFactor;
                    spinUntil = Time.time + GameRules.SpinActiveTime;
                    hitThisSwing.Clear();
                    Sound?.Play(Sfx.Spin);
                    break;
                case ShellType.Lata:
                    if (Time.time < chargeReadyAt || Dodging) return;
                    if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
                    chargeReadyAt = Time.time + GameRules.ChargeCooldown * AttackFactor;
                    chargeUntil = Time.time + GameRules.ChargeTime;
                    hitThisSwing.Clear();
                    Sound?.Play(Sfx.Charge);
                    Director.Instance?.CameraRig.Shake(.08f, .2f);
                    break;
                case ShellType.Lampada:
                    if (Time.time < flashReadyAt) return;
                    flashReadyAt = Time.time + GameRules.FlashCooldown;
                    Flash();
                    break;
                default:
                    Sound?.Play(Sfx.Error);
                    break;
            }
        }

        // Clarão da Lâmpada: atordoa todo mundo por perto e clareia a escuridão (GDD p. 7).
        void Flash()
        {
            Sound?.Play(Sfx.Flash);
            Escuridao.Current?.Flash(1.5f);
            Director.Instance?.Hud.Flash();
            Physics2D.OverlapCircle(Center, GameRules.FlashRadius, hitFilter, overlaps);
            var stunned = new HashSet<IStunnable>();
            foreach (var other in overlaps)
            {
                var target = other.GetComponentInParent<IStunnable>();
                if (target != null && stunned.Add(target)) target.Stun(GameRules.FlashStun);
            }
            Effects.Spark(Center);
        }

        void UpdateAttack()
        {
            // A colisão com a plataforma só volta depois que Casco saiu dela (na água ele cai devagar).
            var platforms = Director.Instance != null && Director.Instance.Room != null ? Director.Instance.Room.Platforms : null;
            if (dropUntil > 0f && Time.time >= dropUntil && platforms != null && !Hitbox.Distance(platforms).isOverlapped)
            {
                Physics2D.IgnoreCollision(Hitbox, platforms, false);
                dropUntil = 0f;
            }
            if (Dead) return;
            if (Time.time < attackUntil)
            {
                var point = Center + new Vector2(Facing * .75f, 0f);
                Physics2D.OverlapBox(point, new Vector2(.95f, .8f), 0f, hitFilter, overlaps);
                HitOverlaps(GameRules.AttackDamage);
            }
            if (Spinning)
            {
                Physics2D.OverlapCircle(Center, GameRules.SpinRadius, hitFilter, overlaps);
                HitOverlaps(GameRules.SpinDamage);
            }
            if (Charging)
            {
                var point = Center + new Vector2(Facing * .55f, 0f);
                Physics2D.OverlapBox(point, new Vector2(1.1f, .9f), 0f, hitFilter, overlaps);
                HitOverlaps(GameRules.ChargeDamage, GameRules.ChargeStun);
            }
        }

        void HitOverlaps(int damage, float stun = 0f)
        {
            foreach (var other in overlaps)
            {
                if (other.attachedRigidbody == Body) continue;
                var target = other.GetComponentInParent<IHittable>();
                if (target == null || !hitThisSwing.Add(target)) continue;
                target.Hit(damage, Center);
                if (stun > 0f) other.GetComponentInParent<IStunnable>()?.Stun(stun);
                Effects.Spark(other.bounds.center);
                Effects.HitStop(.05f);
                Sound?.Play(Sfx.Hit);
            }
        }

        // ---------------------------------------------------------------- Visual

        void Animate()
        {
            // Rolando ou na investida da Lata, a direção fica travada até o fim do movimento.
            if (!Dodging && !Charging && !Locked)
            {
                if (move > .01f) Facing = 1f;
                else if (move < -.01f) Facing = -1f;
            }
            visual.localScale = new Vector3(Facing, 1f, 1f);

            Sprite[] frames;
            int[] bob = null;
            float fps;
            if (Time.time < attackUntil + .08f) { frames = attackFrames; fps = 20f; }
            else if (!Grounded) { frames = new[] { Body.linearVelocity.y > 0f ? jumpFrame : fallFrame }; fps = 1f; }
            else if (Mathf.Abs(Body.linearVelocity.x) > .3f) { frames = walkFrames; bob = WalkBob; fps = 12f; }
            else { frames = idleFrames; bob = IdleBob; fps = 2.5f; }

            if (frames.Length != currentAnimation?.Length || frames[0] != currentAnimation[0]) animationClock = 0f;
            currentAnimation = frames;
            animationClock += Time.deltaTime;
            int index = Mathf.FloorToInt(animationClock * fps) % frames.Length;
            bodyRenderer.sprite = frames[index];
            float lift = bob != null ? bob[index % bob.Length] / 32f : 0f;
            shellRenderer.transform.localPosition = new Vector3(0f, -.5f + lift, 0f);

            // Rolar: dentro da concha só a concha gira; sem concha, Casco se enrola.
            if (Dodging)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 1400f);
                bodyRenderer.enabled = Shell == ShellType.Nenhuma;
            }
            else if (Charging)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, -12f * Facing);
                bodyRenderer.enabled = true;
            }
            else if (Spinning)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, -(spinUntil - Time.time) / GameRules.SpinActiveTime * 360f);
                bodyRenderer.enabled = true;
            }
            else
            {
                visual.localRotation = Quaternion.identity;
                bodyRenderer.enabled = true;
            }
            if (Dead) visual.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Treme sem concha (pânico) e pisca enquanto está invulnerável depois de um golpe.
            float tremble = Time.time < trembleUntil ? Mathf.Sin(Time.time * 90f) * .03f : 0f;
            visual.localPosition = new Vector3(tremble, .5f, 0f);
            bool blink = Time.time < invulnerableUntil && Mathf.Repeat(Time.time * 12f, 1f) < .35f;
            bodyRenderer.color = shellRenderer.color = blink ? new Color(1f, 1f, 1f, .35f) : Color.white;
        }

        // ---------------------------------------------------------------- Teste automático

        public void TeleportTo(Vector2 position)
        {
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector2.zero;
        }

        public void SetShellHp(int hp) => ShellHp = Mathf.Clamp(hp, 0, ShellMaxHp);
        public void UseAbilityNow() => UseAbility();
        public void SetAir(float air) => Air = air;
        public void MakeInvulnerable(float seconds) => invulnerableUntil = Time.time + seconds;
        public void ClearInvulnerability() => invulnerableUntil = 0f;
    }
}
