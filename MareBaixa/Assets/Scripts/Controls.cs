using System.Collections.Generic;
using UnityEngine;

namespace MareBaixa
{
    public enum GameAction { Jump, Attack, Dodge, Ability, Interact, Eat, Pause }

    // Controles do GDD p. 4 (teclado e gamepad pelo Input Manager clássico).
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
            { GameAction.Jump, new[] { KeyCode.Space, KeyCode.W, KeyCode.UpArrow, KeyCode.JoystickButton0 } },
            { GameAction.Attack, new[] { KeyCode.J, KeyCode.JoystickButton2 } },
            { GameAction.Dodge, new[] { KeyCode.K, KeyCode.LeftShift, KeyCode.JoystickButton1 } },
            { GameAction.Ability, new[] { KeyCode.L, KeyCode.JoystickButton3 } },
            { GameAction.Interact, new[] { KeyCode.E, KeyCode.JoystickButton5 } },
            { GameAction.Eat, new[] { KeyCode.Q, KeyCode.JoystickButton4 } },
            { GameAction.Pause, new[] { KeyCode.Escape, KeyCode.P, KeyCode.JoystickButton7 } },
        };

        public static float Move => Simulated ? SimulatedMove : Input.GetAxisRaw("Horizontal");
        public static bool Down => Simulated ? SimulatedDown
            : Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetAxisRaw("Vertical") < -.5f;

        public static bool Pressed(GameAction action)
        {
            if (simulatedPress.TryGetValue(action, out int frame) && frame == Time.frameCount) return true;
            if (Simulated) return false;
            foreach (KeyCode key in Keys[action]) if (Input.GetKeyDown(key)) return true;
            return false;
        }

        public static bool Held(GameAction action)
        {
            if (simulatedHeld.Contains(action)) return true;
            if (Simulated) return Pressed(action);
            foreach (KeyCode key in Keys[action]) if (Input.GetKey(key)) return true;
            return false;
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
