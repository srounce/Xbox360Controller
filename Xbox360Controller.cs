#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using SlimDX.DirectInput;
using VVVV.PluginInterfaces.V2;

using SlimDX;
using SlimDX.XInput;

using VVVV.Core.Logging;

#endregion usings

[PluginInfo(Name = "Xbox360Controller", Category = "Devices", Version = "1.0.0", Help = "Allows you to use XBox360 Controller", Tags = "joystick xbox")]
public class Xbox360Controller : IPluginEvaluate, IPartImportsSatisfiedNotification
{
	#region fields & pins

	private const string ControllersEnumName = "XBox360Controllers";

	[Input("Device", EnumName = ControllersEnumName)]
	IDiffSpread<EnumEntry> FGamePadsEnumInput;

	[Input("Refresh", IsBang = true, IsSingle = true)]
	IDiffSpread<bool> FRefreshInput;

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

	[Import]
	ILogger Flogger;

	private readonly List<GamepadState> FGamepads = new List<GamepadState>(4);
	private readonly DirectInput FDirectInput = new DirectInput();
	private int FGamepadsCount;

	#endregion fields & pins

	public void OnImportsSatisfied()
	{
		CheckDevices();
	}

	private void CheckDevices()
	{
		var devices = FDirectInput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly);

		for (int i = 0; i < devices.Count; i++)
		{
			if (!devices[i].InstanceName.Contains("XBOX")) devices.RemoveAt(i);
		}

		string[] names = new string[devices.Count];

		for (int j = 0; j < devices.Count; j++)
		{
			names[j] = devices[j].InstanceName + " #" + j.ToString(CultureInfo.InvariantCulture);
		}

		EnumManager.UpdateEnum(ControllersEnumName, names[0], names);
	}

	private void InitControllers()
	{
		FGamepads.Clear();

		for (int i = 0; i < FGamePadsEnumInput.SliceCount; i++)
		{
			EnumEntry entry = FGamePadsEnumInput[i];

			if(entry == "(nil)") continue;

			int sliceIndex = int.Parse(entry.Name.Split('#')[1]);

			UserIndex index = (UserIndex) sliceIndex;
			
			FGamepads.Add(new GamepadState(index));
		}

		FGamepadsCount = FGamepads.Count;
	}

	//called each frame by vvvv
	public void Evaluate(int spreadMax)
	{
		if (FRefreshInput[0]) CheckDevices();

		if (FGamePadsEnumInput.IsChanged) InitControllers();

		FLeftThumbPin.SliceCount = FGamepadsCount;
		FLeftThumbPressPin.SliceCount = FGamepadsCount;
		FRightThumbPin.SliceCount = FGamepadsCount;
		FRightThumbPressPin.SliceCount = FGamepadsCount;
		FLeftTriggerPin.SliceCount = FGamepadsCount;
		FLeftShoulderPin.SliceCount = FGamepadsCount;
		FRightTriggerPin.SliceCount = FGamepadsCount;
		FRightShoulderPin.SliceCount = FGamepadsCount;
		FAButtonPin.SliceCount = FGamepadsCount;
		FBButtonPin.SliceCount = FGamepadsCount;
		FXButtonPin.SliceCount = FGamepadsCount;
		FYButtonPin.SliceCount = FGamepadsCount;
		FDpadUpPin.SliceCount = FGamepadsCount;
		FDpadDownPin.SliceCount = FGamepadsCount;
		FDpadLeftPin.SliceCount = FGamepadsCount;
		FDpadRightPin.SliceCount = FGamepadsCount;
		FStartPin.SliceCount = FGamepadsCount;
		FBackPin.SliceCount = FGamepadsCount;

		for (int i = 0; i < FGamepadsCount; i++)
		{
			FGamepads[i].Update();

			FLeftThumbPin[i] = FGamepads[i].LeftStick.Position;
			FLeftThumbPressPin[i] = FGamepads[i].LeftStick.Clicked;
			FRightThumbPin[i] = FGamepads[i].RightStick.Position;
			FRightThumbPressPin[i] = FGamepads[i].RightStick.Clicked;

			FLeftTriggerPin[i] = FGamepads[i].LeftTrigger;
			FLeftShoulderPin[i] = FGamepads[i].LeftShoulder;
			FRightTriggerPin[i] = FGamepads[i].RightTrigger;
			FRightShoulderPin[i] = FGamepads[i].RightShoulder;

			FAButtonPin[i] = FGamepads[i].A;
			FBButtonPin[i] = FGamepads[i].B;
			FXButtonPin[i] = FGamepads[i].X;
			FYButtonPin[i] = FGamepads[i].Y;

			FDpadUpPin[i] = FGamepads[i].DPad.Up;
			FDpadDownPin[i] = FGamepads[i].DPad.Down;
			FDpadLeftPin[i] = FGamepads[i].DPad.Left;
			FDpadRightPin[i] = FGamepads[i].DPad.Right;

			FStartPin[i] = FGamepads[i].Start;
			FBackPin[i] = FGamepads[i].Back;
		}
	}

}

// SlimDX Wrapper developed by
// Zaknafein/Renaud Bédard of The Instruction Limit
// http://theinstructionlimit.com

public class GamepadState
{
	uint FLastPacket;

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
		if (FLastPacket == state.PacketNumber) return;
		FLastPacket = state.PacketNumber;

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