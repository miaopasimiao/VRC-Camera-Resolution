using MelonLoader;
using UIExpansionKit.API;
using VRC.UserCamera;

[assembly: MelonModInfo(typeof(CameraResolution.Main), "Camera Resolution ", "0.1.0", "Dannie")]
[assembly: MelonModGame("VRChat", "VRChat")]

namespace CameraResolution
{
    public class Main : MelonMod
    {
		private const string ModCategory = "Camera Resolution";
		private const string Resolution1NamePref = "Resolution 1 Name";
		private const string Resolution2NamePref = "Resolution 2 Name";
		private const string Resolution3NamePref = "Resolution 3 Name";
		private const string Resolution1Pref = "Resolution 1";
		private const string Resolution2Pref = "Resolution 2";
		private const string Resolution3Pref = "Resolution 3"; 
		private const string ResolutionPref = "Resolution Default"; 

		public override void OnApplicationStart()
		{
			ModPrefs.RegisterCategory(ModCategory, "Camera Resolution");
			ModPrefs.RegisterPrefString(ModCategory, Resolution1NamePref, "1080p", "Resolution 1 Name:");
			ModPrefs.RegisterPrefString(ModCategory, Resolution2NamePref, "4K", "Resolution 2 Name:");
			ModPrefs.RegisterPrefString(ModCategory, Resolution3NamePref, "8K", "Resolution 3 Name:");
			ModPrefs.RegisterPrefInt(ModCategory, Resolution1Pref, 1080, "Resolution 1: (Photo height, unstable > 4320)");
			ModPrefs.RegisterPrefInt(ModCategory, Resolution2Pref, 2160, "Resolution 2: (Photo height, unstable > 4320)");
			ModPrefs.RegisterPrefInt(ModCategory, Resolution3Pref, 4320, "Resolution 3: (Photo height, unstable > 4320)");
			ModPrefs.RegisterPrefInt(ModCategory, ResolutionPref, 2, "Default resolution setting (1-3)");

			ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, ModPrefs.GetString(ModCategory, Resolution1NamePref), Resolution1);
			ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, ModPrefs.GetString(ModCategory, Resolution2NamePref), Resolution2);
			ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, ModPrefs.GetString(ModCategory, Resolution3NamePref), Resolution3);
		}

		public override void VRChat_OnUiManagerInit()
		{
			if(ModPrefs.GetInt(ModCategory, ResolutionPref) == 1)
			{
				Resolution1();
			}
			else if(ModPrefs.GetInt(ModCategory, ResolutionPref) == 2)
			{
				Resolution2();
			}
			else if(ModPrefs.GetInt(ModCategory, ResolutionPref) == 3)
			{
				Resolution3();
			}
			else
			{
				Resolution1();
			}
		}

		public void SetResolution(int photoHeight)
		{
			var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
			cameraController.photoHeight = photoHeight;
			cameraController.photoWidth = photoHeight * 16 / 9;
		}

		public void Resolution1()
		{
			SetResolution(ModPrefs.GetInt(ModCategory, Resolution1Pref));
		}

		public void Resolution2()
		{
			SetResolution(ModPrefs.GetInt(ModCategory, Resolution2Pref));
		}

		public void Resolution3()
		{
			SetResolution(ModPrefs.GetInt(ModCategory, Resolution3Pref));
		}

	}
}
