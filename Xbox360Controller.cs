#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using SlimDX;
using SlimDX.XInput;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Xbox360Controller",
                Category = "Devices",
                Version = "1.0.0",
                Help = "",
                Tags = "")]
    #endregion PluginInfo

    public class Xbox360Controller : IPluginEvaluate
    {
        #region fields & pins
        /*[Input("Gamepad Number", DefaultValue = 1, 
            MinValue = 1,  MaxValue = 4)]
        IDiffSpread<int> FGamepadIndex;*/

        [Output("Left Thumbstick")]
        ISpread<Vector2> FLeftThumbPin;

        [Output("Left Thumbstick Pressed")]
        ISpread<bool> FLeftThumbPressPin;

        [Output("Right Thumbstick")]
        ISpread<Vector2> FRightThumbPin;

        [Output("Right Thumbstick Pressed")]
        ISpread<bool> FRightThumbPressPin;

        [Output("Left Trigger")]
        ISpread<double> FLeftTriggerPin;

        [Output("Left Shoulder Button")]
        ISpread<bool> FLeftShoulderPin;

        [Output("Right Trigger")]
        ISpread<double> FRightTriggerPin;

        [Output("Right Shoulder Button")]
        ISpread<bool> FRightShoulderPin;

        [Output("A Button")]
        ISpread<bool> FAButtonPin;

        [Output("B Button")]
        ISpread<bool> FBButtonPin;

        [Output("X Button")]
        ISpread<bool> FXButtonPin;

        [Output("Y Button")]
        ISpread<bool> FYButtonPin;

        [Output("D-Pad Up")]
        ISpread<bool> FDpadUpPin;

        [Output("D-Pad Down")]
        ISpread<bool> FDpadDownPin;

        [Output("D-Pad Left")]
        ISpread<bool> FDpadLeftPin;

        [Output("D-Pad Right")]
        ISpread<bool> FDpadRightPin;

        [Output("Start")]
        ISpread<bool> FStartPin;

        [Output("Back")]
        ISpread<bool> FBackPin;

        [Import()]
        ILogger Flogger;

        private GamepadState FGamepad;
        #endregion fields & pins

        public Xbox360Controller()
        {
            try
            {
                FGamepad = new GamepadState(0);
            }
            catch (Exception e)
            {
                Flogger.Log(LogType.Error, e.Message);
            }
        }

        //called each frame by vvvv
        public void Evaluate(int SpreadMax)
        {
            FGamepad.Update();

            for (int i = 0; i < SpreadMax; i++)
            {

                FLeftThumbPin[i] = FGamepad.LeftStick.Position;
                FLeftThumbPressPin[i] = FGamepad.LeftStick.Clicked;
                FRightThumbPin[i] = FGamepad.RightStick.Position;
                FRightThumbPressPin[i] = FGamepad.RightStick.Clicked;

                FLeftTriggerPin[i] = FGamepad.LeftTrigger;
                FLeftShoulderPin[i] = FGamepad.LeftShoulder;
                FRightTriggerPin[i] = FGamepad.RightTrigger;
                FRightShoulderPin[i] = FGamepad.RightShoulder;

                FAButtonPin[i] = FGamepad.A;
                FBButtonPin[i] = FGamepad.B;
                FXButtonPin[i] = FGamepad.X;
                FYButtonPin[i] = FGamepad.Y;

                FDpadUpPin[i] = FGamepad.DPad.Up;
                FDpadDownPin[i] = FGamepad.DPad.Down;
                FDpadLeftPin[i] = FGamepad.DPad.Left;
                FDpadRightPin[i] = FGamepad.DPad.Right;

                FStartPin[i] = FGamepad.Start;
                FBackPin[i] = FGamepad.Back;
            }
        }
    }

    // SlimDX Wrapper developed by
    // Zaknafein/Renaud Bédard of The Instruction Limit
    // http://theinstructionlimit.com

    public class GamepadState
    {
        uint lastPacket;

        public GamepadState(UserIndex userIndex)
        {
            UserIndex = userIndex;
            Controller = new Controller(userIndex);

        }

        public readonly UserIndex UserIndex;
        public readonly Controller Controller;

        public DPadState DPad { get; private set; }
        public ThumbstickState LeftStick { get; private set; }
        public ThumbstickState RightStick { get; private set; }

        public bool A { get; private set; }
        public bool B { get; private set; }
        public bool X { get; private set; }
        public bool Y { get; private set; }

        public bool RightShoulder { get; private set; }
        public bool LeftShoulder { get; private set; }

        public bool Start { get; private set; }
        public bool Back { get; private set; }

        public float RightTrigger { get; private set; }
        public float LeftTrigger { get; private set; }

        public bool Connected
        {
            get { return Controller.IsConnected; }
        }

        public void Vibrate(float leftMotor, float rightMotor)
        {
            Controller.SetVibration(new Vibration
            {
                LeftMotorSpeed = (ushort)(MathHelper.Saturate(leftMotor) * ushort.MaxValue),
                RightMotorSpeed = (ushort)(MathHelper.Saturate(rightMotor) * ushort.MaxValue)
            });
        }

        public void Update()
        {
            // If not connected, nothing to update
            if (!Connected) return;

            // If same packet, nothing to update
            State state = Controller.GetState();
            if (lastPacket == state.PacketNumber) return;
            lastPacket = state.PacketNumber;

            var gamepadState = state.Gamepad;

            // Shoulders
            LeftShoulder = (gamepadState.Buttons & GamepadButtonFlags.LeftShoulder) != 0;
            RightShoulder = (gamepadState.Buttons & GamepadButtonFlags.RightShoulder) != 0;

            // Triggers
            LeftTrigger = gamepadState.LeftTrigger / (float)byte.MaxValue;
            RightTrigger = gamepadState.RightTrigger / (float)byte.MaxValue;

            // Buttons
            Start = (gamepadState.Buttons & GamepadButtonFlags.Start) != 0;
            Back = (gamepadState.Buttons & GamepadButtonFlags.Back) != 0;

            A = (gamepadState.Buttons & GamepadButtonFlags.A) != 0;
            B = (gamepadState.Buttons & GamepadButtonFlags.B) != 0;
            X = (gamepadState.Buttons & GamepadButtonFlags.X) != 0;
            Y = (gamepadState.Buttons & GamepadButtonFlags.Y) != 0;

            // D-Pad
            DPad = new DPadState((gamepadState.Buttons & GamepadButtonFlags.DPadUp) != 0,
                                 (gamepadState.Buttons & GamepadButtonFlags.DPadDown) != 0,
                                 (gamepadState.Buttons & GamepadButtonFlags.DPadLeft) != 0,
                                 (gamepadState.Buttons & GamepadButtonFlags.DPadRight) != 0);

            // Thumbsticks
            LeftStick = new ThumbstickState(
                Normalize(gamepadState.LeftThumbX, gamepadState.LeftThumbY, Gamepad.GamepadLeftThumbDeadZone),
                (gamepadState.Buttons & GamepadButtonFlags.LeftThumb) != 0);
            RightStick = new ThumbstickState(
                Normalize(gamepadState.RightThumbX, gamepadState.RightThumbY, Gamepad.GamepadRightThumbDeadZone),
                (gamepadState.Buttons & GamepadButtonFlags.RightThumb) != 0);
        }

        static Vector2 Normalize(short rawX, short rawY, short threshold)
        {
            var value = new Vector2(rawX, rawY);
            var magnitude = value.Length();
            var direction = value / (magnitude == 0 ? 1 : magnitude);

            var normalizedMagnitude = 0.0f;
            if (magnitude - threshold > 0)
                normalizedMagnitude = Math.Min((magnitude - threshold) / (short.MaxValue - threshold), 1);

            return direction * normalizedMagnitude;
        }

        public struct DPadState
        {
            public readonly bool Up, Down, Left, Right;

            public DPadState(bool up, bool down, bool left, bool right)
            {
                Up = up; Down = down; Left = left; Right = right;
            }
        }

        public struct ThumbstickState
        {
            public readonly Vector2 Position;
            public readonly bool Clicked;

            public ThumbstickState(Vector2 position, bool clicked)
            {
                Clicked = clicked;
                Position = position;
            }
        }
    }

    public static class MathHelper
    {
        public static float Saturate(float value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }

}
