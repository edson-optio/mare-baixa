using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MareBaixa
{
    public enum GameAction { Jump, Attack, Dodge, Ability, Interact, Eat, Pause }

    // Controles do GDD p. 4 (teclado legado e gamepad com layout normalizado pelo Input System).
    // O teste automático "aperta" botões por Press/Hold sem precisar de teclado.
    public static class Controls
    {
        public static bool Simulated;
        public static float SimulatedMove;
        public static bool SimulatedDown;

        static readonly Dictionary<GameAction, int> simulatedPress = new Dictionary<GameAction, int>();
        static readonly HashSet<GameAction> simulatedHeld = new HashSet<GameAction>();

        static readonly Dictionary<GameAction, KeyCode[]> Keys = new Dictionary<GameAction, KeyCode[]>
        {
            { GameAction.Jump, new[] { KeyCode.Space, KeyCode.W, KeyCode.UpArrow } },
            { GameAction.Attack, new[] { KeyCode.J } },
            { GameAction.Dodge, new[] { KeyCode.K, KeyCode.LeftShift } },
            { GameAction.Ability, new[] { KeyCode.L } },
            { GameAction.Interact, new[] { KeyCode.E } },
            { GameAction.Eat, new[] { KeyCode.Q } },
            { GameAction.Pause, new[] { KeyCode.Escape, KeyCode.P } },
        };

        // O layout do Input System normaliza Xbox/PlayStation e acompanha conexão/desconexão.
        public static Vector2 Navigation
        {
            get
            {
                if (Simulated) return Vector2.zero;
                float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                        - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                        - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
                var pad = Gamepad.current;
                if (pad == null) return new Vector2(x, y);
                // ReadValue aplica a zona morta do stick; o direcional tem prioridade por eixo.
                var stick = pad.leftStick.ReadValue();
                var dpad = pad.dpad.ReadValue();
                return new Vector2(x != 0f ? x : dpad.x != 0f ? dpad.x : stick.x,
                                   y != 0f ? y : dpad.y != 0f ? dpad.y : stick.y);
            }
        }

        // O layout HID do Switch Pro só existe no Input System para desktop; no build web ele não é compilado.
        public static bool NintendoLayout => Gamepad.current is EightBitDoUltimateHid
#if UNITY_EDITOR || UNITY_STANDALONE
            || Gamepad.current is UnityEngine.InputSystem.Switch.SwitchProControllerHID
#endif
            ;
        public static string FormatHints(string text) => text
            .Replace("{padJump}", NintendoLayout ? "B" : "A (×)")
            .Replace("{padDodge}", NintendoLayout ? "A" : "B (círculo)")
            .Replace("{padAttack}", NintendoLayout ? "Y" : "X (quadrado)")
            .Replace("{padAbility}", NintendoLayout ? "X" : "Y (triângulo)")
            .Replace("{padConfirm}", NintendoLayout ? "B" : "A");

        public static string InteractHint => !Simulated && Gamepad.current != null ? "RB/R1" : "E";

        public static float Move => Simulated ? SimulatedMove : Navigation.x;
        public static bool Down => Simulated ? SimulatedDown : Navigation.y < -.5f;
        public static bool ConfirmPressed => Pressed(GameAction.Interact)
            || (!Simulated && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space) || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false)));
        public static bool BackPressed => !Simulated && (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false);
        public static bool CancelPressed => Pressed(GameAction.Pause) || BackPressed;

        static ButtonControl PadButton(GameAction action)
        {
            var pad = Gamepad.current;
            if (pad == null) return null;
            return action switch
            {
                GameAction.Jump => pad.buttonSouth,
                GameAction.Attack => pad.buttonWest,
                GameAction.Dodge => pad.buttonEast,
                GameAction.Ability => pad.buttonNorth,
                GameAction.Interact => pad.rightShoulder,
                GameAction.Eat => pad.leftShoulder,
                GameAction.Pause => pad.startButton,
                _ => null,
            };
        }

        public static bool Pressed(GameAction action)
        {
            if (simulatedPress.TryGetValue(action, out int frame) && frame == Time.frameCount) return true;
            if (Simulated) return false;
            foreach (KeyCode key in Keys[action]) if (Input.GetKeyDown(key)) return true;
            return PadButton(action)?.wasPressedThisFrame ?? false;
        }

        public static bool Held(GameAction action)
        {
            if (simulatedHeld.Contains(action)) return true;
            if (Simulated) return Pressed(action);
            foreach (KeyCode key in Keys[action]) if (Input.GetKey(key)) return true;
            return PadButton(action)?.isPressed ?? false;
        }

        // Vale no próximo quadro, para quem lê no Update não perder o aperto.
        public static void Press(GameAction action) => simulatedPress[action] = Time.frameCount + 1;

        public static void Hold(GameAction action, bool held)
        {
            if (held) simulatedHeld.Add(action);
            else simulatedHeld.Remove(action);
        }

        public static void ClearSimulation()
        {
            SimulatedMove = 0f;
            SimulatedDown = false;
            simulatedPress.Clear();
            simulatedHeld.Clear();
        }
    }
}
