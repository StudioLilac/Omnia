using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Players.Mixin {
    public class UseInput : MonoBehaviour {
        [SerializeField] internal Player self;
        [SerializeField] internal float delay;

        [Header("Input Actions (Player map)")]
        [SerializeField] private InputActionReference moveAction;   // Value, Vector2
        [SerializeField] private InputActionReference lookAction;   // Value, Vector2 (pointer position)
        [SerializeField] private InputActionReference fireAction;   // Button
        [SerializeField] private InputActionReference skillAction;  // Button
        [SerializeField] private InputActionReference jumpAction;   // Button
        [SerializeField] private InputActionReference rollAction;   // Button
        [SerializeField] private InputActionReference introAction;  // Button (held)

        private float jt;
        private float ft;
        private float skt;
        private float swt;
        private float rlt;
        private float ist;

        private TimeManager timeManager;
        private static readonly object inventoryLock = new();
        private static readonly object consoleLock = new();

        public void Start() {
            timeManager = this.Inject<TimeManager>();
            timeManager.OnFreezeChanged += OnFreezeChanged;
            InventoryManager.OnInventoryOpened += OnInventoryOpened;
            InventoryManager.OnInventoryClosed += OnInventoryClosed;
            DebugConsole.OnConsoleOpened += OnConsoleOpened;
            DebugConsole.OnConsoleClosed += OnConsoleClosed;
        }

        public void OnDestroy() {
            if (timeManager != null) timeManager.OnFreezeChanged -= OnFreezeChanged;
            Player.ControlsLocked.Unset(timeManager);
            Player.ControlsLocked.Unset(inventoryLock);
            Player.ControlsLocked.Unset(consoleLock);
            InventoryManager.OnInventoryOpened -= OnInventoryOpened;
            InventoryManager.OnInventoryClosed -= OnInventoryClosed;
            DebugConsole.OnConsoleOpened -= OnConsoleOpened;
            DebugConsole.OnConsoleClosed -= OnConsoleClosed;
        }

        public void Update() {
            if (Player.ControlsLocked) {
                self.moving = Vector2.zero;
                return;
            }

            var fire = fireAction.action.WasPressedThisFrame();
            var jump = jumpAction.action.WasPressedThisFrame();
            var held = jumpAction.action.IsPressed();
            var skill = skillAction.action.WasPressedThisFrame();
            var roll = rollAction.action.WasPressedThisFrame();
            var intro = introAction.action.IsPressed();

            ft = fire ? delay : Mathf.Max(0, ft - Time.deltaTime);
            jt = jump ? delay : Mathf.Max(0, jt - Time.deltaTime);
            skt = skill ? delay : Mathf.Max(0, skt - Time.deltaTime);
            rlt = roll ? delay : Mathf.Max(0, rlt - Time.deltaTime);
            ist = intro ? delay : Mathf.Max(0, ist - Time.deltaTime);

            self.fire = self.fire ? ft > 0 : fire;
            self.jump = self.jump ? jt > 0 : jump;
            self.skill = self.skill ? skt > 0 : skill;
            self.roll = self.roll ? rlt > 0 : roll;
            self.intro = self.intro ? ist > 0 : intro;

            self.facing = GetFacingInput(self);
            var move = GetMovingInput();
            if (move.x < 0f) self.lastDirectionIsRight = false;
            else if (move.x > 0f) self.lastDirectionIsRight = true;
            self.moving = move;
            self.held = self.jump && (self.grounded || self.slide.x != 0) || self.held && held;
        }

        private Vector2 GetMovingInput() {
            return moveAction.action.ReadValue<Vector2>();
        }

        private Vector2 GetFacingInput(Player it) {
            Vector2 pointer = lookAction.action.ReadValue<Vector2>();
            return it.cam.ScreenToWorldPoint(pointer) - it.sprite.transform.position;
        }

        private void OnFreezeChanged(bool frozen) {
            if (frozen) Player.ControlsLocked.Set(timeManager);
            else Player.ControlsLocked.Unset(timeManager);
        }

        // Putting this here since it feels better coupled, ie so that InventoryManager doesn't
        // know about Player.ControlsLocked
        private void OnInventoryOpened() => Player.ControlsLocked.Set(inventoryLock);
        private void OnInventoryClosed() => Player.ControlsLocked.Unset(inventoryLock);

        // Same here, DebugConsole probably shouldn't know about Player.ControlsLocked
        private void OnConsoleOpened() => Player.ControlsLocked.Set(consoleLock);
        private void OnConsoleClosed() => Player.ControlsLocked.Unset(consoleLock);
    }
}
