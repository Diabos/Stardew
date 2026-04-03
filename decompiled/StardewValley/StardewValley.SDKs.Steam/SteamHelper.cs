using StardewValley.Menus;

namespace StardewValley.SDKs.Steam;

// Browser-friendly Steam helper shim. Keeps Steam-specific type checks working
// without loading Steamworks.NET in wasm.
public class SteamHelper : NullSDKHelper
{
	public bool active;

	public bool GalaxyConnected { get; private set; }

	public override string Name => "Steam";

	public bool HasActiveOverlay => false;

	public virtual bool IsRunningOnSteamDeck()
	{
		return false;
	}

	public bool RetroactiveAchievementsAllowed()
	{
		return true;
	}

	public void CancelKeyboard()
	{
	}

	public void ShowKeyboard(TextBox text_box)
	{
	}

	public new void Initialize()
	{
		active = false;
		GalaxyConnected = false;
	}
}
