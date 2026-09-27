using System;
using System.Collections;
using System.Collections.Generic;
using Enemies;
using Omnia.State;
using Omnia.Utils;
using Players.Animation;
using Players.Behaviour;
using Players.Buff;
using Puzzle;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using If = Omnia.State.FuncPredicate;

namespace Players {
    public class Player : MonoBehaviour {
        [SerializeField] internal SpriteRenderer sprite;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Rigidbody2D rb;
        [SerializeField] internal BoxCollider2D hitbox;
        [SerializeField] internal LayerMask ground;
        [SerializeField] internal LayerMask semisolid;
        [SerializeField] internal LayerMask destructable;
        public LayerMask GroundedMask => ground | semisolid | destructable;
        [SerializeField] internal BoxCollider2D[] checks;

        // Health is an integer. 1=HP = half a heart, meaning a full heart is 2HP.
        [SerializeField] internal int maximumHealth;
        [SerializeField] internal float maximumFlow;

        // health and flow are properties that broadcast an event whenever they are changed.
        private int _currentHealth;

        public int CurrentHealth {
            get => _currentHealth;
            set {
                if (_currentHealth != value) {
                    _currentHealth = Mathf.Clamp(value, 0, maximumHealth);
                    ;
                    OnHealthChanged?.Invoke(_currentHealth);
                }
            }
        }

        private float _currentFlow;

        public float CurrentFlow {
            get => _currentFlow;
            set {
                if (_currentFlow != value) {
                    _currentFlow = value;
                    OnFlowChanged?.Invoke(_currentFlow);
                }
            }
        }

        [SerializeField] internal float moveSpeed;
        [SerializeField] internal float jumpSpeed;
        [SerializeField] internal float pullSpeed;
        [SerializeField] internal float rollSpeed;
        [SerializeField] internal float rollDuration;
        [SerializeField] internal float rollCooldown;
        [SerializeField] internal float rollThreshold;
        [SerializeField] internal float moveAccel;
        [SerializeField] internal float fallAccel;
        [SerializeField] internal float jumpLockoutTime;
        [SerializeField] internal float flow;
        [SerializeField] internal float weaponRecoilLockoutTime;
        [SerializeField] internal float wallJumpLockoutTime;
        [SerializeField] internal float combatCooldown;
        [SerializeField] internal float skillCooldown;
        [SerializeField] internal float introCooldown;
        [SerializeField] internal float flowDrainRate;
        [SerializeField] internal float hurtInvulnerabilityTime;

        [SerializeField] internal WeaponClass[] weapons;
        [SerializeField] internal int selectedWeapon;

        [SerializeField] internal Vector2 facing;
        [SerializeField] internal Vector2 moving;
        [SerializeField] internal bool jump;
        [SerializeField] internal bool roll;
        [SerializeField] internal bool held;
        [SerializeField] internal bool fire;
        [SerializeField] internal bool skill;
        [SerializeField] internal bool intro;
        [SerializeField] internal bool grounded;
        [SerializeField] internal bool canRoll;
        [SerializeField] internal bool invulnerable;
        [SerializeField] internal bool inCombat;
        [SerializeField] internal Vector2 slide;

        [SerializeField] internal string debugBehaviour;
        [SerializeField] internal Transform debugPullToTargetTransform;

        [SerializeField] internal Transform buffsParent;

        // if this is false, disable swapping.
        [SerializeField] internal bool hasShotgun;

        [SerializeField] public float critChance;
        [SerializeField] public float critMultiplier;

        private List<float> additiveMultipliers = new List<float>();
        private List<float> multiplicativeMultipliers = new List<float>();

        public float BaseDamageMultiplier => 1f;

        public float damageMultiplier {
            get {
                float sumAdd = 0f;
                foreach (var val in additiveMultipliers)
                    sumAdd += val;

                float prodMul = 1f;
                foreach (var val in multiplicativeMultipliers)
                    prodMul *= val;

                return (BaseDamageMultiplier + sumAdd) * prodMul;
            }
        }

        public void AddAdditiveBuff(float value) {
            Debug.Log("Adding a buff with factor "+ value);
            additiveMultipliers.Add(value);
        }

        public void RemoveAdditiveBuff(float value) {
            additiveMultipliers.Remove(value);
        }

        public void AddMultiplicativeBuff(float value) {
            multiplicativeMultipliers.Add(value);
        }

        public void RemoveMultiplicativeBuff(float value) {
            multiplicativeMultipliers.Remove(value);
        }

        public void ClearAllBuffs() {
            additiveMultipliers.Clear();
            multiplicativeMultipliers.Clear();
        }


        // disable user input if this is true.
        public static bool controlsLocked = false;

        public static bool knockbackImmune;

        private bool _healthBoosted;

        public bool HealthBoosted {
            get => _healthBoosted;
            set {
                if (_healthBoosted != value) {
                    _healthBoosted = value;
                    OnHealthBoostChanged?.Invoke(_healthBoosted);
                }
            }
        }

        // these public fields are set by trinkets that determine aspects of player behaviour.
        public bool musicBoxEquipped;
        public bool shoeEquipped;
        public bool bearEquipped;

        // Describes how much healing you get from a swap.
        public const int SWAP_HEAL = 2;

        internal Camera cam;
        public event Action Spawn;
        public static event Action Death;

        public static event Action<float> OnFlowChanged;
        public static event Action<int> OnHealthChanged;
        public static event Action<int> OnWeaponChanged;
        public static event Action<float> OnSkillCooldownUpdated;

        public static event Action<float, Enemy> OnEnemyHit;

        public static event Action OnSkill;

        public static event Action<bool> OnBearEffectReady;
        private bool wasBearReady = false; // Track previous state


        public static event Action<bool> OnHealthBoostChanged;

        private float currentLockout;
        private float maximumLockout;
        private float currentHurtInvulnerability;

        private CountdownTimer combatTimer;
        private CountdownTimer rollCooldownTimer;
        private CountdownTimer skillCooldownTimer;
        private CountdownTimer introCooldownTimer;
        private CountdownTimer bearCooldownTimer;

        private IBehaviour behaviour;
        private StateMachine animationStateMachine;
        internal bool lockGravity = false;
        public Vector3 Center => this ? transform.position + new Vector3(0, 1, 0) : Vector3.zero;


        public void Awake() {
            cam = Camera.main;
            UseBehaviour(new Idle(this));
            UseAnimation(new StateMachine());
        }

        public void Start() {
            CurrentHealth = maximumHealth;

            CurrentFlow = 0;

            combatTimer = new CountdownTimer(combatCooldown);
            rollCooldownTimer = new CountdownTimer(rollCooldown);
            skillCooldownTimer = new CountdownTimer(skillCooldown);
            introCooldownTimer = new CountdownTimer(introCooldown);
            bearCooldownTimer = new CountdownTimer(TeddyBearBuff.cooldownTime);

            canRoll = true;

            // use persistence data from data manager
            selectedWeapon = PlayerDataManager.Instance.playerSelectedWeapon;

            for (int i = 0; i < weapons.Length; i++) {
                weapons[i].SetSpriteActive(i == selectedWeapon);
            }

            OnWeaponChanged?.Invoke(selectedWeapon);

            // initially fill out the skill bar
            OnFlowChanged?.Invoke(CurrentFlow);
            OnHealthChanged?.Invoke(CurrentHealth);
            OnSkillCooldownUpdated?.Invoke(1);
            Spawn?.Invoke();
        }

        public void Update() {
            currentLockout = Mathf.Clamp(currentLockout - Time.deltaTime, 0, maximumLockout);
            behaviour?.OnUpdate();
            animationStateMachine.Update();

            UpdateCombatTimer();
            UpdateRollCooldownTimer();
            UpdateSkillCooldownTimer();
            UpdateBearCooldownTimer();


            currentHurtInvulnerability = Mathf.Max(0, currentHurtInvulnerability - Time.deltaTime);
        }

        public void FixedUpdate() {
            if (!lockGravity) rb.gravityScale = held && rb.velocity.y > 0 ? 1 : MathUtils.Lerpish(rb.gravityScale, 3, Time.fixedDeltaTime * fallAccel);
            DoAttack();
            DoSkill();
            DoIntroSkill();
            behaviour?.OnTick();
            animationStateMachine.FixedUpdate();
        }

        public void UsePull(Transform target) {
            UseBehaviour(new Pull(this, target));
        }

        public void UseRecoil(float speed) {
            var recoil = -1 * speed * facing.normalized;
            UseExternalVelocity(new Vector2(rb.velocity.x + recoil.x, recoil.y), weaponRecoilLockoutTime);
        }

        // ***** Methods for handling player stats (HP, Flow) ***** //

        public void Hurt(float damage, Vector2 velocity = default, float lockout = 0) {
            if (invulnerable || currentHurtInvulnerability > 0) return;

            // Apply any buffs that reduce incoming damage
            foreach (var modifier in Buff.Buff.OnDamageTaken) {
                damage = modifier(damage);
            }

            if (bearEquipped && !bearCooldownTimer.IsRunning && damage > 0) {
                // Bear effect takes place and prevents this instance of damage.
                bearCooldownTimer.Start();
                damage = 0;
            }

            AudioManager.Instance.PlayHurtSound();

            combatTimer.Start();
            CurrentHealth -= (int)damage;
            currentHurtInvulnerability = hurtInvulnerabilityTime;
            if (!knockbackImmune) {
                UseExternalVelocity(velocity, lockout);
            }
            StartCoroutine(DoHurtInvincibilityFlicker());

            if (CurrentHealth == 0) Die();
        }

        public void OnHit(float damage, Enemy enemy) {
            combatTimer.Start();
            if (!hasShotgun) return;
            CurrentFlow = Mathf.Min(CurrentFlow + (damage / damageMultiplier) * WeaponClass.damageToFlowRatio, maximumFlow);
            OnEnemyHit?.Invoke(damage, enemy);
        }

        public void ConsumeAllFlow() {
            if (CurrentFlow > 0) {
                CurrentFlow = 0;
            }
        }


        public void Die() {
            Death?.Invoke();
        }

        // ***** Methods for behaviour + animation ***** //

        public void UseBehaviour(IBehaviour it) {
            if (it == null) return;
            debugBehaviour = it.GetType().Name;

            behaviour?.OnExit();
            behaviour = it;
            behaviour?.OnEnter();
        }

        public void UseExternalVelocity(Vector2 velocity, float lockout) {
            rb.velocity = velocity;
            currentLockout = maximumLockout = lockout;
        }

        public float HorizontalVelocityOf(float x, float acceleration) {
            if (maximumLockout == 0) return MathUtils.Lerpish(rb.velocity.x, x, acceleration);
            var control = 1 - currentLockout / maximumLockout;
            return MathUtils.Lerpish(rb.velocity.x, x, control * acceleration);
        }

        public void SetGravityLock(bool lockGravity, float gravity) {
            rb.gravityScale = gravity;
            this.lockGravity = lockGravity;
        }

        internal bool IsPhoon() {
            return Math.Abs(rb.velocity.x) > moveSpeed && Math.Sign(rb.velocity.x) == Math.Sign(moving.x);
        }

        internal bool IsAttackEnabled() {
            return behaviour is not Roll;
        }

        // Called by Behaviour.Roll to handle the cooldown timer
        internal void OnRoll() {
            canRoll = false;
            rollCooldownTimer.Start();
        }

        private void DoAttack() {
            if (!fire || !IsAttackEnabled()) return;
            fire = false;
            weapons[selectedWeapon].Attack();
        }

        private void DoSkill() {
            if (skillCooldownTimer.IsRunning) {
                OnSkillCooldownUpdated?.Invoke(skillCooldownTimer.Progress);
                return;
            }

            if (!skill) return;
            skill = false;
            if (weapons[selectedWeapon].UseSkill()) {
                skillCooldownTimer.Start();
                OnSkill?.Invoke();
            }
        }

        private void DoIntroSkill() {
            if (!intro || introCooldownTimer.IsRunning) return;
            intro = false;
            introCooldownTimer.Start();
            if (selectedWeapon == 0) {
                DoSwap(1);
            } else {
                DoSwap(0);
            }
        }


        public void DoSwap(int targetWeapon) {
            if (!hasShotgun) return;
            if (selectedWeapon != targetWeapon) {
                weapons[selectedWeapon].SetSpriteActive(false);

                selectedWeapon = targetWeapon;

                weapons[selectedWeapon].SetSpriteActive(true);

                if (Mathf.Approximately(CurrentFlow, maximumFlow)) {
                    ConsumeAllFlow();
                    weapons[selectedWeapon].IntroSkill();
                    if (musicBoxEquipped) {
                        CurrentHealth += SWAP_HEAL;
                    }
                }

                OnWeaponChanged?.Invoke(targetWeapon);
            }
        }


        // ***** Helpers ***** //

        private void UpdateCombatTimer() {
            combatTimer.Tick(Time.deltaTime);
        }

        private void UpdateRollCooldownTimer() {
            rollCooldownTimer.Tick(Time.deltaTime);

            if (!rollCooldownTimer.IsRunning) {
                canRoll = true;
            }
        }

        private void UpdateSkillCooldownTimer() {
            skillCooldownTimer.Tick(Time.deltaTime);
            introCooldownTimer.Tick(Time.deltaTime);
        }

        private void UpdateBearCooldownTimer() {
            bearCooldownTimer.Tick(Time.deltaTime);

            bool isBearReady = !bearCooldownTimer.IsRunning && bearEquipped;
            if (isBearReady != wasBearReady) {
                OnBearEffectReady?.Invoke(isBearReady);
                wasBearReady = isBearReady;
            }
        }

        /* This is kind of lazy but it works. */
        private IEnumerator DoHurtInvincibilityFlicker() {
            while (currentHurtInvulnerability > 0) {
                sprite.enabled = false;
                yield return new WaitForSeconds(0.02f);

                sprite.enabled = true;
                yield return new WaitForSeconds(0.10f);
            }
        }

        private void UseAnimation(StateMachine stateMachine) {
            var idleAnimation = new IdleAnimation(animator);
            var moveAnimation = new MoveAnimation(animator);
            var jumpAnimation = new JumpAnimation(animator);
            var fallAnimation = new FallAnimation(animator);
            var rollAnimation = new RollAnimation(animator);
            var slideLAnimation = new SlideLAnimation(animator);
            var slideRAnimation = new SlideRAnimation(animator);
            var moveBackwardAnimation = new MoveBackwardAnimation(animator);

            stateMachine.AddAnyTransition(moveAnimation, new If(() => behaviour is Idle or Move && moving.x != 0 && Math.Sign(facing.x) == Math.Sign(moving.x)));
            stateMachine.AddAnyTransition(idleAnimation, new If(() => behaviour is Idle && moving.x == 0));
            stateMachine.AddAnyTransition(jumpAnimation, new If(() => behaviour is Jump or WallJump or Pull));
            stateMachine.AddAnyTransition(fallAnimation, new If(() => behaviour is Fall));
            stateMachine.AddAnyTransition(rollAnimation, new If(() => behaviour is Roll));
            stateMachine.AddAnyTransition(slideLAnimation, new If(() => behaviour is Slide && Math.Sign(facing.x) != Math.Sign(slide.x)));
            stateMachine.AddAnyTransition(slideRAnimation, new If(() => behaviour is Slide && Math.Sign(facing.x) == Math.Sign(slide.x)));
            stateMachine.AddAnyTransition(moveBackwardAnimation, new If(() => behaviour is Move && Math.Sign(facing.x) != Math.Sign(moving.x)));

            stateMachine.SetState(idleAnimation);
            animationStateMachine = stateMachine;
        }
    }
}
