using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MareBaixa
{
    // Diagnóstico opt-in: registra apenas dispositivos de jogo e seus comandos.
    public class GamepadDiagnostics : MonoBehaviour
    {
        int lastDirection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-inputDiagnostics") < 0) return;
            var host = new GameObject("Diagnóstico do controle");
            DontDestroyOnLoad(host);
            host.AddComponent<GamepadDiagnostics>();
        }

        void OnEnable()
        {
            InputSystem.onDeviceChange += DeviceChanged;
            foreach (var device in InputSystem.devices) DeviceChanged(device, InputDeviceChange.Added);
        }

        void OnDisable() => InputSystem.onDeviceChange -= DeviceChanged;

        static void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad || device is Joystick)
                Debug.Log($"MARE_GAMEPAD_DEVICE: {device.displayName} layout={device.layout} change={change}");
        }

        void Update()
        {
            var pad = Gamepad.current;
            if (pad == null) return;
            foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
                if (Controls.Pressed(action)) Debug.Log("MARE_GAMEPAD_ACTION: " + action);
            var direction = Controls.Navigation;
            int next = (direction.x > .5f ? 1 : direction.x < -.5f ? 2 : 0)
                     | (direction.y > .5f ? 4 : direction.y < -.5f ? 8 : 0);
            if (next != lastDirection) Debug.Log("MARE_GAMEPAD_MOVE: " + direction);
            lastDirection = next;
        }
    }
}
