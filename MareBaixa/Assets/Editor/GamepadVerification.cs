using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MareBaixa.Editor
{
    // Executável em batchmode: exercita o backend real com eventos de um dispositivo virtual.
    public static class GamepadVerification
    {
        const string PendingKey = "MareBaixa.GamepadVerification.Pending";

        public static void ObserveDevices()
        {
            double until = EditorApplication.timeSinceStartup + 60;
            string last = "";
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.onEvent += (evt, device) =>
            {
                if (!(device is Gamepad) && !(device is Joystick)) return;
                Debug.Log($"MARE_EVENT: {device.layout} type={evt.type} bytes={evt.sizeInBytes}");
                if (!(device is EightBitDoUltimateHid) || (evt.type != StateEvent.Type && evt.type != DeltaStateEvent.Type)) return;
                var pad = (EightBitDoUltimateHid)device;
                var id = pad.GetChildControl<UnityEngine.InputSystem.Controls.IntegerControl>("reportId").ReadValueFromEvent(evt);
                string sample = $"report={id} bytes={evt.sizeInBytes} stick={pad.leftStick.ReadValueFromEvent(evt)} dpad={pad.dpad.ReadValueFromEvent(evt)} south={pad.buttonSouth.ReadValueFromEvent(evt)} east={pad.buttonEast.ReadValueFromEvent(evt)}";
                if (sample != last) { Debug.Log("MARE_RAW: " + sample); last = sample; }
            };
            EditorApplication.update += () =>
            {
                InputSystem.Update();
                if (EditorApplication.timeSinceStartup > until) EditorApplication.Exit(0);
            };
            DescribeDevices();
            Debug.Log("MARE_OBSERVE_READY");
        }

        public static void DescribeDevices()
        {
            InputSystem.Update();
            foreach (var device in InputSystem.devices)
            {
                if (!(device is Gamepad) && !(device is Joystick)) continue;
                Debug.Log($"MARE_DEVICE: {device.name} type={device.GetType().Name} layout={device.layout} interface={device.description.interfaceName} product={device.description.product}");
                Debug.Log("MARE_HID: " + device.description.capabilities);
                foreach (var control in device.allControls)
                    Debug.Log($"MARE_CONTROL: {control.path} layout={control.layout} offset={control.stateBlock.byteOffset} bit={control.stateBlock.bitOffset} format={control.stateBlock.format}");
            }
            Debug.Log("MARE_DEVICES_READY");
        }

        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void ResumeAfterReload()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            try { Verify(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        static void Verify()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad = InputSystem.AddDevice<Gamepad>();
            bool simulated = Controls.Simulated;
            try
            {
                Controls.Simulated = false;
                Controls.ClearSimulation();
                var actions = new[] { GameAction.Jump, GameAction.Attack, GameAction.Dodge,
                    GameAction.Ability, GameAction.Interact, GameAction.Eat, GameAction.Pause };
                var buttons = new[] { GamepadButton.South, GamepadButton.West, GamepadButton.East,
                    GamepadButton.North, GamepadButton.RightShoulder, GamepadButton.LeftShoulder, GamepadButton.Start };
                for (int i = 0; i < actions.Length; i++)
                {
                    Set(pad, new GamepadState());
                    Set(pad, new GamepadState().WithButton(buttons[i]));
                    Require(Controls.Pressed(actions[i]) && Controls.Held(actions[i]), "Botão " + actions[i] + $" (value={pad[buttons[i]].ReadValue()}, pressed={Controls.Pressed(actions[i])}, held={Controls.Held(actions[i])}, current={Gamepad.current == pad}, enabled={pad.enabled}, update={InputState.currentUpdateType})");
                    for (int j = 0; j < actions.Length; j++)
                        if (i != j) Require(!Controls.Held(actions[j]), "Sem ação cruzada " + actions[j]);
                    if (i == 0) Require(Controls.ConfirmPressed, "A confirma");
                    if (i == 2) Require(Controls.BackPressed && Controls.CancelPressed, "B volta");
                    Set(pad, new GamepadState().WithButton(buttons[i]));
                    Require(!Controls.Pressed(actions[i]) && Controls.Held(actions[i]), "Segurar sem repetir " + actions[i]);
                    Set(pad, new GamepadState());
                    Require(!Controls.Held(actions[i]), "Soltar " + actions[i]);
                }
                Set(pad, new GamepadState { leftStick = new Vector2(.05f, 0f) });
                Require(Controls.Move == 0f, "Zona morta");
                Set(pad, new GamepadState { leftStick = new Vector2(-1f, -1f) });
                Require(Controls.Move < -.5f && Controls.Down, "Analógico e descer plataforma");
                Set(pad, new GamepadState().WithButton(GamepadButton.DpadRight));
                Require(Controls.Move == 1f, "Direcional horizontal");
                Set(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
                Require(Controls.Down && Controls.Navigation.y == -1f, "Direcional vertical");
                var nav = new UiKit.NavRepeat();
                Require(nav.Read() == 1 && nav.Read() == 0, "Menu não repete imediatamente");
                Set(pad, new GamepadState().WithButton(GamepadButton.South));
                Controls.Simulated = true;
                Require(!Controls.Pressed(GameAction.Jump) && Controls.Move == 0f, "Simulação isola gamepad");
                Controls.Simulated = false;
                InputSystem.RemoveDevice(pad);
                Require(Controls.Move == 0f && !Controls.Held(GameAction.Jump), "Desconexão libera comandos");
                pad = InputSystem.AddDevice<Gamepad>();
                Set(pad, new GamepadState().WithButton(GamepadButton.South));
                Require(Controls.Pressed(GameAction.Jump), "Reconexão");
                VerifyEightBitDo();
                Debug.Log("MARE_GAMEPAD_TESTS: PASSED");
            }
            finally
            {
                if (pad.added) InputSystem.RemoveDevice(pad);
                Controls.ClearSimulation();
                Controls.Simulated = simulated;
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }

        static void VerifyEightBitDo()
        {
            var device = InputSystem.AddDevice(new UnityEngine.InputSystem.Layouts.InputDeviceDescription
            {
                interfaceName = "HID", manufacturer = "8BitDo", product = "8BitDo Ultimate Controller",
                capabilities = "{\"vendorId\":11720,\"productId\":24583}",
            });
            try
            {
                Require(device is EightBitDoUltimateHid, "8BitDo HID é reconhecido como Gamepad");
                var pad = (EightBitDoUltimateHid)device;
                var neutral = EightBitDoUltimateState.Neutral;
                void Send(EightBitDoUltimateState state)
                {
                    InputSystem.QueueStateEvent(pad, state);
                    InputSystem.Update();
                    pad.MakeCurrent();
                }
                Send(neutral);
                Require(Controls.Move == 0f && Controls.Navigation == Vector2.zero, "8BitDo neutro sem drift");
                var actions = new[] { GameAction.Jump, GameAction.Attack, GameAction.Dodge,
                    GameAction.Ability, GameAction.Eat, GameAction.Interact, GameAction.Pause };
                var bits = new[] { 1, 4, 0, 3, 6, 7, 11 };
                for (int i = 0; i < actions.Length; i++)
                {
                    var state = neutral;
                    if (bits[i] < 8) state.buttons = (byte)(1 << bits[i]);
                    else state.menuButtons = (byte)(1 << (bits[i] - 8));
                    Send(state);
                    Require(Controls.Pressed(actions[i]) && Controls.Held(actions[i]), "8BitDo botão " + actions[i]);
                    if (i == 0) Require(Controls.ConfirmPressed, "8BitDo confirma");
                    if (i == 2) Require(Controls.CancelPressed, "8BitDo volta");
                    for (int j = 0; j < actions.Length; j++)
                        if (i != j) Require(!Controls.Held(actions[j]), "8BitDo sem ação cruzada " + actions[j]);
                    Send(neutral);
                    Require(!Controls.Held(actions[i]), "8BitDo solta " + actions[i]);
                }
                foreach (byte hat in new byte[] { 0, 2, 4, 6 })
                {
                    var state = neutral; state.hat = (byte)(hat << 4); Send(state);
                    var expected = hat == 0 ? Vector2.up : hat == 2 ? Vector2.right : hat == 4 ? Vector2.down : Vector2.left;
                    Require(Controls.Navigation == expected, "8BitDo direcional " + hat);
                }
                var stick = neutral; stick.leftX = 255; stick.leftY = 255; Send(stick);
                Require(Controls.Move > .5f && Controls.Down, "8BitDo analógico direita/baixo");
                stick.leftX = 0; stick.leftY = 0; Send(stick);
                Require(Controls.Move < -.5f && Controls.Navigation.y > .5f, "8BitDo analógico esquerda/cima");
                Send(neutral);
                var auxiliary = neutral; auxiliary.reportId = 2; auxiliary.buttons = 255; Send(auxiliary);
                Require(!Controls.Held(GameAction.Jump), "8BitDo ignora relatório auxiliar");
                Debug.Log("MARE_8BITDO_TESTS: PASSED");
            }
            finally { InputSystem.RemoveDevice(device); }
        }

        static void Set(Gamepad pad, GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Gamepad: " + message);
        }
    }
}
