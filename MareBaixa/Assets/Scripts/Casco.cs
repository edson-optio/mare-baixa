using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    // Casco: anda, pula, ataca com pinças/sucata (J), rola (K), usa a habilidade
    // da concha (L), interage (E) e come algas (Q). A concha é a armadura: cada golpe tira 1 ponto
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
        public WeaponType Weapon { get; private set; }
        public bool Resting { get; private set; }
        public bool Attacking => Time.time < attackUntil;
        public float AbilityCooldown => Mathf.Max(0f, (Shell == ShellType.Espiral ? spinReadyAt
            : Shell == ShellType.Lata ? chargeReadyAt : Shell == ShellType.Lampada ? flashReadyAt : 0f) - Time.time);
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
        public bool Invulnerable => Resting || Dodging || Spinning || Charging || Time.time < invulnerableUntil;
        public bool Dead { get; private set; }
        public float Facing { get; private set; } = 1f;
        public bool CanTakeDamage { get; set; } = true;   // a vila desliga
        public Interactable Focus { get; private set; }

        // Modo -demo (DemoPiloto): Casco apanha e reage (empurrão leve, rachadura), mas a concha não
        // quebra, o ar não acaba, ninguém leva a concha e ele não morre. Sem piscar, para o vídeo.
        public static bool DemoGod;

        public Vector2 Center => (Vector2)transform.position + new Vector2(0f, .35f);
        public Vector2 Head => (Vector2)transform.position + new Vector2(0f, .75f);

        GameAudio Sound => Director.Instance != null ? Director.Instance.Sound : null;
        bool Locked => Dead || Resting || (Director.Instance != null && Director.Instance.InputLocked);

        ContactFilter2D groundFilter;
        ContactFilter2D hitFilter;
        readonly List<Collider2D> overlaps = new List<Collider2D>();
        readonly HashSet<IHittable> hitThisSwing = new HashSet<IHittable>();
        readonly HashSet<IHittable> hitThisAbility = new HashSet<IHittable>();

        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        float attackReadyAt, attackUntil, spinReadyAt, spinUntil;
        float attackStartedAt, nextSpinTrail;
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
        SpriteRenderer weaponRenderer;

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
            CreateWeaponRenderers();
        }

        // ---------------------------------------------------------------- Concha

        public void Equip(ShellType type, int hp)
        {
            CancelActions();
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
            if (DemoGod || Dead || Shell == ShellType.Nenhuma || Invulnerable || !CanTakeDamage) return false;
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

        public void EquipWeapon(WeaponType type)
        {
            Weapon = Weapons.Valid(type) ? type : WeaponType.Pincas;
            attackUntil = 0f;
            // Trocar de arma não elimina a recuperação do último ataque.
            weaponRenderer.sprite = Weapons.Sprite(Weapon);
        }

        public void RestOnBench(Vector2 feet, bool resting)
        {
            CancelActions();
            Body.linearVelocity = Vector2.zero;
            Resting = resting;
            Body.simulated = !resting;
            Body.position = feet + (resting ? new Vector2(0f, .4f) : Vector2.zero);
            transform.position = Body.position;
        }

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
            if (DemoGod)
            {
                Body.linearVelocity = new Vector2(direction * 4f, 5f);
                knockbackUntil = Time.time + .15f;
                invulnerableUntil = Time.time + GameRules.InvulnerableTime;
                if (Shell != ShellType.Nenhuma && ShellHp > 1) ShellHp--;
                Sound?.Play(Shell != ShellType.Nenhuma ? Sfx.Crack : Sfx.Hurt);
                Director.Instance?.CameraRig.Shake(.1f, .12f);
                return;
            }
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
            if (Dead || DemoGod) return;
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
                if (DemoGod) Air = Mathf.Max(Air, MaxAir * .25f);
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
            if (Dead || Resting) return;
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
            if (Spinning)
                velocity.x = Facing * GameRules.SpinSpeed * waterMove;
            else if (Charging)
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
            if (Time.time < attackReadyAt || Dodging || Spinning || Charging || Locked) return;
            if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
            var weapon = Weapons.Get(Weapon);
            attackStartedAt = Time.time;
            swingFacing = Facing;
            attackReadyAt = Time.time + weapon.Cooldown * AttackFactor;
            attackUntil = Time.time + weapon.ActiveTime;
            hitThisSwing.Clear();
            Sound?.Play(Sfx.Attack);
            Effects.Slash(Center, Facing, weapon.Reach, weapon.Color, Weapon == WeaponType.Garfo);
        }

        void StartDodge()
        {
            if (Time.time < dodgeReadyAt || Spinning || Charging || Locked) return;
            if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
            attackUntil = 0f;
            dodgeUntil = Time.time + GameRules.DodgeTime;
            dodgeReadyAt = Time.time + GameRules.DodgeCooldown;
            Sound?.Play(Sfx.Dodge);
        }

        void UseAbility()
        {
            if (Dodging || Spinning || Charging || Locked) return;
            switch (Shell)
            {
                case ShellType.Tampinha:
                    RequestJump();   // L também pula (e faz o pulo duplo no ar)
                    break;
                case ShellType.Espiral:
                    if (Time.time < spinReadyAt) return;
                    spinReadyAt = Time.time + GameRules.SpinCooldown * AttackFactor;
                    spinUntil = Time.time + GameRules.SpinActiveTime;
                    attackUntil = 0f;
                    if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
                    // No chão a Espiral salta numa cambalhota. No ar ela avança sem ganhar
                    // outro impulso vertical: não permite voar repetindo a habilidade.
                    if (Grounded || Time.time - lastGroundedTime <= GameRules.CoyoteTime)
                        Body.linearVelocity = new Vector2(Body.linearVelocity.x,
                            GameRules.SpinJumpSpeed * (InWater ? GameRules.WaterJumpFactor : 1f));
                    cutJump = false;
                    lastGroundedTime = lastJumpPressedTime = -10f;
                    hitThisAbility.Clear();
                    nextSpinTrail = 0f;
                    Sound?.Play(Sfx.Spin);
                    Effects.Spark(Center);
                    break;
                case ShellType.Lata:
                    if (Time.time < chargeReadyAt || Dodging) return;
                    if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
                    chargeReadyAt = Time.time + GameRules.ChargeCooldown * AttackFactor;
                    chargeUntil = Time.time + GameRules.ChargeTime;
                    attackUntil = 0f;
                    hitThisAbility.Clear();
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
            if (Locked) return;
            if (Time.time < attackUntil)
            {
                var weapon = Weapons.Get(Weapon);
                float near = .25f;
                var point = Center + new Vector2(Facing * (weapon.Reach + near) * .5f, .05f);
                Physics2D.OverlapBox(point, new Vector2(weapon.Reach - near, weapon.Height), 0f, hitFilter, overlaps);
                HitOverlaps(weapon.Damage, hitThisSwing, weapon.Stun);
            }
            if (Spinning)
            {
                Physics2D.OverlapCircle(Center, GameRules.SpinRadius, hitFilter, overlaps);
                HitOverlaps(GameRules.SpinDamage, hitThisAbility);
                if (Time.time >= nextSpinTrail)
                {
                    nextSpinTrail = Time.time + .065f;
                    Effects.Afterimage(shellRenderer, GameRules.Turquoise);
                }
            }
            if (Charging)
            {
                var point = Center + new Vector2(Facing * .55f, 0f);
                Physics2D.OverlapBox(point, new Vector2(1.1f, .9f), 0f, hitFilter, overlaps);
                HitOverlaps(GameRules.ChargeDamage, hitThisAbility, GameRules.ChargeStun);
            }
        }

        void HitOverlaps(int damage, HashSet<IHittable> alreadyHit, float stun = 0f)
        {
            foreach (var other in overlaps)
            {
                if (other.attachedRigidbody == Body) continue;
                var target = other.GetComponentInParent<IHittable>();
                if (target == null || !alreadyHit.Add(target)) continue;
                // Não atravessa chão ou parede para atingir um inimigo do outro lado.
                if (Physics2D.Linecast(Center, other.bounds.center, LayerMask.GetMask("Chao"))) continue;
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
            if (!Dodging && !Charging && !Spinning && !Attacking && !Locked)
            {
                if (move > .01f) Facing = 1f;
                else if (move < -.01f) Facing = -1f;
            }
            visual.localScale = new Vector3(Facing, 1f, 1f);

            Sprite[] frames;
            int[] bob = null;
            float fps;
            int swingFrame = SwingBodyFrame();
            if (swingFrame >= 0) { frames = attackFrames; fps = 0f; }
            else if (Time.time < attackUntil + .08f) { frames = attackFrames; fps = 20f; }
            else if (!Grounded) { frames = new[] { Body.linearVelocity.y > 0f ? jumpFrame : fallFrame }; fps = 1f; }
            else if (Mathf.Abs(Body.linearVelocity.x) > .3f) { frames = walkFrames; bob = WalkBob; fps = 12f; }
            else { frames = idleFrames; bob = IdleBob; fps = 2.5f; }

            if (frames.Length != currentAnimation?.Length || frames[0] != currentAnimation[0]) animationClock = 0f;
            currentAnimation = frames;
            animationClock += Time.deltaTime;
            int index = swingFrame >= 0 ? Mathf.Min(swingFrame, frames.Length - 1)
                : Mathf.FloorToInt(animationClock * fps) % frames.Length;
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
                visual.localRotation = Quaternion.Euler(0f, 0f, (spinUntil - Time.time) / GameRules.SpinActiveTime * 1080f);
                bodyRenderer.enabled = false;
            }
            else if (Resting)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, -22f);
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
            bool blink = !DemoGod && Time.time < invulnerableUntil && Mathf.Repeat(Time.time * 12f, 1f) < .35f;
            bodyRenderer.color = shellRenderer.color = blink ? new Color(1f, 1f, 1f, .35f) : Color.white;
            if (Spinning) shellRenderer.color = Color.Lerp(Color.white, GameRules.Turquoise, .35f);
            AnimateWeapon();
        }

        // ---------------------------------------------------------------- Arma na pinça

        // Onde a pinça da frente prende o cabo em cada quadro do corpo: o pixel (no quadro de 64×64,
        // contado do canto de baixo à esquerda) e o retângulo da pinça redesenhado por cima do cabo,
        // para a pinça ficar na frente dele. Assim a arma sai de dentro da pinça em vez de flutuar.
        readonly struct Grip
        {
            public readonly Vector2Int Pixel;
            public readonly RectInt Fingers;
            public Grip(int x, int y) : this(x, y, new RectInt(44, 0, 20, y + 18)) { }
            public Grip(int x, int y, RectInt fingers) { Pixel = new Vector2Int(x, y); Fingers = fingers; }
        }

        static readonly Dictionary<string, Grip> Grips = new Dictionary<string, Grip>
        {
            { "casco-parado-1", new Grip(51, 9) },
            { "casco-parado-2", new Grip(51, 9) },
            { "casco-anda-1", new Grip(52, 9) },
            { "casco-anda-2", new Grip(51, 8) },
            { "casco-anda-3", new Grip(50, 9) },
            { "casco-anda-4", new Grip(50, 7) },
            { "casco-anda-5", new Grip(51, 8) },
            { "casco-anda-6", new Grip(52, 9) },
            { "casco-pulo", new Grip(55, 8, new RectInt(44, 3, 20, 24)) },
            { "casco-queda", new Grip(57, 5, new RectInt(45, 0, 19, 22)) },
            // Pinça erguida (preparo), aberta lá na frente (corte) e fechando embaixo (volta).
            { "casco-ataque-1", new Grip(54, 25, new RectInt(46, 16, 18, 15)) },
            { "casco-ataque-2", new Grip(54, 22, new RectInt(44, 9, 20, 22)) },
            { "casco-ataque-3", new Grip(57, 11, new RectInt(45, 5, 19, 20)) },
        };
        static readonly Grip DefaultGrip = new Grip(51, 9);

        // Golpe com arma, só no desenho (dano, alcance e tempo ativo continuam em Weapons):
        // prepara → corta rápido → segura um instante → volta para a guarda. O corte percorre o mesmo
        // arco de Effects.Slash (parâmetro From → To, em graus), então a lâmina passa pelo rastro.
        sealed class SwingStyle
        {
            public float Windup, Strike, Hold, Back;
            public float Guard;              // ângulo da arma parado/andando (frente e para cima)
            public float From, To;           // arco do rastro no começo e no fim do corte
            public bool Heavy;               // acelera até o impacto (martelo) em vez de sair rápido
            public bool Thrust;              // estocada: recua e estica deslizando a haste na pinça
            public float SlideGuard, SlideBack, SlideOut;
            public int LateStrikeFrame, HoldFrame;   // quadros de casco-ataque (0, 1, 2)
            public float Total => Windup + Strike + Hold + Back;
        }

        static readonly SwingStyle CanudoSwing = new SwingStyle
        {
            Windup = .04f, Strike = .08f, Hold = .07f, Back = .09f,
            Guard = 40f, From = 75f, To = -45f, LateStrikeFrame = 1, HoldFrame = 2,
        };
        static readonly SwingStyle GarfoSwing = new SwingStyle
        {
            Windup = .06f, Strike = .07f, Hold = .08f, Back = .1f,
            Guard = 25f, Thrust = true, SlideGuard = 0f, SlideBack = -.3f, SlideOut = .14f,
            LateStrikeFrame = 1, HoldFrame = 1,
        };
        static readonly SwingStyle EscovaSwing = new SwingStyle
        {
            Windup = .09f, Strike = .07f, Hold = .1f, Back = .14f,
            Guard = 58f, From = 90f, To = -30f, Heavy = true, LateStrikeFrame = 2, HoldFrame = 2,
        };

        static SwingStyle StyleOf(WeaponType type) =>
            type == WeaponType.Garfo ? GarfoSwing : type == WeaponType.Escova ? EscovaSwing : CanudoSwing;

        SpriteRenderer fingersRenderer;
        readonly Dictionary<Sprite, Sprite> fingerSprites = new Dictionary<Sprite, Sprite>();
        float swingFacing;

        bool WeaponVisible => Weapon != WeaponType.Pincas && !Dead && !Resting && !Dodging && !Spinning && !Charging;
        float SwingTime => Time.time - attackStartedAt;
        // Virar de lado no fim do golpe encerra o desenho do golpe (a arma volta à guarda do outro lado).
        bool Swinging => WeaponVisible && attackUntil > 0f && Facing == swingFacing && SwingTime < StyleOf(Weapon).Total;

        void CreateWeaponRenderers()
        {
            weaponRenderer = new GameObject("Arma", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            weaponRenderer.transform.SetParent(visual, false);
            weaponRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            weaponRenderer.sortingOrder = 22;   // na frente do corpo (20) e da concha (21)
            fingersRenderer = new GameObject("Dedos da pinça", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            fingersRenderer.transform.SetParent(bodyRenderer.transform, false);
            fingersRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
            fingersRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            fingersRenderer.sortingOrder = 23;  // os dedos por cima do cabo
            fingersRenderer.enabled = false;
        }

        // Quadro de casco-ataque durante o golpe com arma (-1 fora dele; as pinças usam o ciclo antigo).
        int SwingBodyFrame()
        {
            if (!Swinging) return -1;
            var s = StyleOf(Weapon);
            float u = SwingTime;
            if (u < s.Windup) return 0;
            if (u < s.Windup + s.Strike) return u < s.Windup + s.Strike * .5f ? 1 : s.LateStrikeFrame;
            if (u < s.Windup + s.Strike + s.Hold) return s.HoldFrame;
            return 2;
        }

        void AnimateWeapon()
        {
            bool visible = WeaponVisible && weaponRenderer.sprite != null;
            weaponRenderer.enabled = visible;
            fingersRenderer.enabled = false;
            if (!visible) return;

            var body = bodyRenderer.sprite;
            Grip grip = body != null && Grips.TryGetValue(body.name, out var found) ? found : DefaultGrip;
            Vector2 hand = bodyRenderer.transform.localPosition;
            if (body != null) hand += ((Vector2)grip.Pixel + new Vector2(.5f, .5f) - body.pivot) / body.pixelsPerUnit;

            WeaponPose(StyleOf(Weapon), hand, out float angle, out float slide, out float reachOut);
            var axis = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            weaponRenderer.transform.localPosition = hand + axis * slide;
            weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            // No meio do corte a lâmina estica um pouco (borrão de movimento) até encostar no rastro.
            float blade = weaponRenderer.sprite.bounds.max.x;
            float stretch = reachOut > 0f && blade > 0f ? Mathf.Clamp(reachOut / blade, 1f, 1.25f) : 1f;
            weaponRenderer.transform.localScale = new Vector3(stretch, 1f, 1f);
            // As cerdas da escova vão na frente da martelada.
            weaponRenderer.flipY = Weapon == WeaponType.Escova;
            weaponRenderer.color = bodyRenderer.color;

            if (body != null && bodyRenderer.enabled)
            {
                fingersRenderer.sprite = FingersSprite(body, grip);
                fingersRenderer.enabled = fingersRenderer.sprite != null;
                fingersRenderer.color = bodyRenderer.color;
            }
        }

        // Ângulo da arma, quanto a haste desliza na pinça (só a lança desliza) e, durante o corte,
        // a distância da pinça até o rastro (para esticar a lâmina; 0 fora do corte).
        void WeaponPose(SwingStyle s, Vector2 hand, out float angle, out float slide, out float reachOut)
        {
            reachOut = 0f;
            float guard = s.Guard + (Grounded ? 0f : Body.linearVelocity.y > 0f ? 10f : -6f);
            angle = guard;
            slide = s.SlideGuard;
            if (!Swinging) return;

            float reach = Weapons.Get(Weapon).Reach;
            float u = SwingTime;
            float raised = s.Thrust ? ThrustAngle(hand) + 6f : TrailAngle(s.From, hand, reach);
            if (u < s.Windup)
            {
                float k = EaseOut(u / s.Windup);
                angle = Mathf.LerpAngle(guard, raised, k);
                slide = Mathf.Lerp(s.SlideGuard, s.SlideBack, k);
                return;
            }
            u -= s.Windup;
            if (u < s.Strike)
            {
                float k = u / s.Strike;
                k = s.Heavy ? k * k : EaseOut(k);
                if (s.Thrust)
                {
                    angle = ThrustAngle(hand);
                    slide = Mathf.Lerp(s.SlideBack, s.SlideOut, k);
                }
                else
                {
                    float a = Mathf.Lerp(s.From, s.To, k);
                    angle = TrailAngle(a, hand, reach);
                    reachOut = (TrailPoint(a, reach) - hand).magnitude * Mathf.Sin(k * Mathf.PI);
                }
                return;
            }
            u -= s.Strike;
            float end = s.Thrust ? ThrustAngle(hand) : TrailAngle(s.To, hand, reach);
            float endSlide = s.Thrust ? s.SlideOut : s.SlideGuard;
            if (u < s.Hold)
            {
                float k = u / s.Hold;
                // A espada assenta um pouco; o martelo repica no impacto; a lança fica esticada.
                angle = end + (s.Thrust ? 0f : s.Heavy ? 7f * Mathf.Sin(k * Mathf.PI) * (1f - k) : 5f * EaseOut(k));
                slide = endSlide;
                return;
            }
            u -= s.Hold;
            float back = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / s.Back));
            angle = Mathf.LerpAngle(end + (s.Thrust || s.Heavy ? 0f : 5f), guard, back);
            slide = Mathf.Lerp(endSlide, s.SlideGuard, back);
        }

        static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

        // Direção da pinça até o ponto `a` (graus) do arco que Effects.Slash desenha a partir de Center.
        float TrailAngle(float a, Vector2 hand, float reach)
        {
            var point = TrailPoint(a, reach);
            return Mathf.Atan2(point.y - hand.y, point.x - hand.x) * Mathf.Rad2Deg;
        }

        // Ponto do arco do rastro nas coordenadas de `visual` (mesma elipse de Effects.Slash).
        Vector2 TrailPoint(float a, float reach)
        {
            var center = new Vector2(0f, Center.y - transform.position.y - visual.localPosition.y);
            return center + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad) * reach, Mathf.Sin(a * Mathf.Deg2Rad) * .6f);
        }

        // A estocada mira a linha reta do rastro da lança (Center + 0,05).
        float ThrustAngle(Vector2 hand)
        {
            float trailY = Center.y - transform.position.y - visual.localPosition.y + .05f;
            return Mathf.Atan2(trailY - hand.y, 1.6f) * Mathf.Rad2Deg;
        }

        // Recorte do próprio quadro do corpo só na região dos dedos, com o pivô alinhado ao do corpo.
        Sprite FingersSprite(Sprite body, Grip grip)
        {
            if (fingerSprites.TryGetValue(body, out var cached)) return cached;
            Sprite sprite = null;
            var full = body.rect;
            int xMin = Mathf.Max(0, grip.Fingers.xMin), yMin = Mathf.Max(0, grip.Fingers.yMin);
            int xMax = Mathf.Min((int)full.width, grip.Fingers.xMax), yMax = Mathf.Min((int)full.height, grip.Fingers.yMax);
            if (body.texture != null && xMax > xMin && yMax > yMin)
            {
                var rect = new Rect(full.x + xMin, full.y + yMin, xMax - xMin, yMax - yMin);
                var pivot = new Vector2((body.pivot.x - xMin) / rect.width, (body.pivot.y - yMin) / rect.height);
                sprite = Sprite.Create(body.texture, rect, pivot, body.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = body.name + " (dedos)";
            }
            fingerSprites[body] = sprite;
            return sprite;
        }

        void CancelActions()
        {
            attackUntil = spinUntil = chargeUntil = dodgeUntil = 0f;
            lastGroundedTime = lastJumpPressedTime = -10f;
            airJumpRequested = cutJump = false;
            push = lastPush = Vector2.zero;
            hitThisSwing.Clear();
            hitThisAbility.Clear();
        }

        // ---------------------------------------------------------------- Teste automático

        public void TeleportTo(Vector2 position)
        {
            CancelActions();
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
