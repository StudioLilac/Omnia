using UnityEngine;
using UnityEngine.InputSystem;

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

        public void Start() {
            InventoryManager.OnInventoryOpened += StopMoving;
        }

        private void OnEnable() {
            moveAction.action.Enable();
            lookAction.action.Enable();
            fireAction.action.Enable();
            skillAction.action.Enable();
            jumpAction.action.Enable();
            rollAction.action.Enable();
            introAction.action.Enable();
        }

        private void OnDisable() {
            moveAction.action.Disable();
            lookAction.action.Disable();
            fireAction.action.Disable();
            skillAction.action.Disable();
            jumpAction.action.Disable();
            rollAction.action.Disable();
            introAction.action.Disable();
        }

        public void Update() {
            if (Player.controlsLocked) {
                self.moving = Vector2.zero;
                return;
            }
            if (DialogueManager.Instance?.IsInDialogue() == true || InventoryManager.Instance?.IsInventoryOpen == true) return;
            if (PauseMenu.IsPaused) return;

            var fire = fireAction.action.WasPressedThisFrame();
            var jump = jumpAction.action.WasPressedThisFrame();
            var held = jumpAction.action.IsPressed();
            var skill = skillAction.action.WasPressedThisFrame();
            var roll = rollAction.action.WasPressedThisFrame() && self.shoeEquipped;
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
            self.moving = GetMovingInput();
            self.held = self.jump && (self.grounded || self.slide.x != 0) || self.held && held;
        }

        public void OnDestroy() {
            InventoryManager.OnInventoryOpened -= StopMoving;
        }

        private Vector2 GetMovingInput() {
            return moveAction.action.ReadValue<Vector2>();
        }

        private Vector2 GetFacingInput(Player it) {
            Vector2 pointer = lookAction.action.ReadValue<Vector2>();
            return it.cam.ScreenToWorldPoint(pointer) - it.sprite.transform.position;
        }

        private void StopMoving() {
            self.moving = Vector2.zero;
        }
    }
}
