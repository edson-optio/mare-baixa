using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace MareBaixa
{
    // Layout HID 2dc8:6007 lido do Ultimate conectado ao Mac (relatório 1, 64 bytes).
    // Posições dos botões confirmadas pela entrada 2dc8:6007 do SDL_GameControllerDB:
    // https://github.com/mdqinc/SDL_GameControllerDB/blob/master/gamecontrollerdb.txt
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct EightBitDoUltimateState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        [InputControl(name = "reportId", layout = "Integer", format = "BYTE")]
        [FieldOffset(0)] public byte reportId;

        [InputControl(name = "buttonSouth", bit = 1, displayName = "B")]
        [InputControl(name = "buttonEast", bit = 0, displayName = "A")]
        [InputControl(name = "buttonWest", bit = 4, displayName = "Y")]
        [InputControl(name = "buttonNorth", bit = 3, displayName = "X")]
        [InputControl(name = "leftShoulder", bit = 6)]
        [InputControl(name = "rightShoulder", bit = 7)]
        [FieldOffset(1)] public byte buttons;

        [InputControl(name = "select", bit = 2)]
        [InputControl(name = "start", bit = 3)]
        [InputControl(name = "leftStickPress", bit = 5)]
        [InputControl(name = "rightStickPress", bit = 6)]
        [FieldOffset(2)] public byte menuButtons;

        [InputControl(name = "dpad", layout = "Dpad", format = "BIT", bit = 4, sizeInBits = 4, defaultState = 8)]
        [InputControl(name = "dpad/up", layout = "DiscreteButton", format = "BIT", bit = 4, sizeInBits = 4,
            parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7")]
        [InputControl(name = "dpad/right", layout = "DiscreteButton", format = "BIT", bit = 4, sizeInBits = 4,
            parameters = "minValue=1,maxValue=3")]
        [InputControl(name = "dpad/down", layout = "DiscreteButton", format = "BIT", bit = 4, sizeInBits = 4,
            parameters = "minValue=3,maxValue=5")]
        [InputControl(name = "dpad/left", layout = "DiscreteButton", format = "BIT", bit = 4, sizeInBits = 4,
            parameters = "minValue=5,maxValue=7")]
        [FieldOffset(3)] public byte hat;

        [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "leftStick/x", offset = 0, format = "BYTE", defaultState = 128, parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/y", offset = 1, format = "BYTE", defaultState = 128, parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "leftStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
        [InputControl(name = "leftStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "leftStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(4)] public byte leftX;
        [FieldOffset(5)] public byte leftY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "rightStick/x", offset = 0, format = "BYTE", defaultState = 128, parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/y", offset = 1, format = "BYTE", defaultState = 128, parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
        [InputControl(name = "rightStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
        [InputControl(name = "rightStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
        [InputControl(name = "rightStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
        [FieldOffset(6)] public byte rightX;
        [FieldOffset(7)] public byte rightY;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(8)] public byte rightTrigger;
        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(9)] public byte leftTrigger;

        public static EightBitDoUltimateState Neutral => new EightBitDoUltimateState
        { reportId = 1, hat = 0x80, leftX = 128, leftY = 128, rightX = 128, rightY = 128 };
    }

    [InputControlLayout(stateType = typeof(EightBitDoUltimateState), displayName = "8BitDo Ultimate (USB)")]
    public class EightBitDoUltimateHid : Gamepad, IInputStateCallbackReceiver
    {
        IntegerControl reportId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Register()
        {
            InputSystem.RegisterLayout<EightBitDoUltimateHid>(matches: new InputDeviceMatcher()
                .WithInterface("HID").WithCapability("vendorId", 0x2dc8).WithCapability("productId", 0x6007));
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            reportId = GetChildControl<IntegerControl>("reportId");
        }

        public void OnNextUpdate() { }
        public void OnStateEvent(InputEventPtr eventPtr)
        {
            // Relatórios 2/129 são auxiliares e não podem sobrescrever o estado dos botões.
            if (eventPtr.type == StateEvent.Type && reportId.ReadValueFromEvent(eventPtr) == 1)
                InputState.Change(this, eventPtr);
        }
        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset) => false;
    }
}
