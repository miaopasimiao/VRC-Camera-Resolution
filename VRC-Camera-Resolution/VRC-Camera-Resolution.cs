using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using MelonLoader;
using VRC.UserCamera;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(VRC_Camera_Resolution.Main), "Camera Resolution", "1.2.0", "Dannie")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace VRC_Camera_Resolution
{
	public class Main : MelonMod
	{
		private const string configFile = @"UserData/CamConfig.json";
		private Settings settings;

		private ICustomShowableLayoutedMenu camMenu;
		private ICustomShowableLayoutedMenu settingsMenu;

		private float aspectRatio;

		public override void OnApplicationStart()
		{
			if (!File.Exists(configFile)) initConfig();
			readConfig();
		}

		public override void VRChat_OnUiManagerInit()
		{
			initMenus();
			SetResolution(settings.Resolutions[settings.DefaultRes - 1].ImageHeight, settings.Resolutions[settings.DefaultRes - 1].ImageWidth);
		}

		public override void OnLevelWasLoaded(int level)
		{
			
			switch (level)
			{
				case 0: 
				case 1: 
					break;
				default:
					MelonCoroutines.Start(CamClipping());
					break;
			}
		}

		public void SetResolution(int photoHeight, int photoWidth)
		{
			var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
			cameraController.photoHeight = photoHeight;
			if (photoWidth != -1)
			{
				cameraController.photoWidth = photoWidth;
			}
			else
			{
				cameraController.photoWidth = (int)(photoHeight * aspectRatio);
			}
		}

		public void initMenus()
		{
			ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Resolution", delegate ()
			{
				camMenu.Show();
			});

			settingsMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

			settingsMenu.AddLabel("\nChange Resolutions");
			settingsMenu.AddSimpleButton("Add Resolution", delegate ()
			{
				MelonCoroutines.Start(addCam());
			});
			settingsMenu.AddSimpleButton("Delete Resolution", delegate ()
			{
				MelonCoroutines.Start(removeCam());
			});
			settingsMenu.AddLabel("\nChange Default Settings");
			settingsMenu.AddSimpleButton("Default Aspect Ratio", delegate ()
			{
				MelonCoroutines.Start(ChangeDefaultAspectRatio());
			});
			settingsMenu.AddSimpleButton("Default Resolution", delegate ()
			{
				MelonCoroutines.Start(ChangeDefaultRes());
			});
			settingsMenu.AddSimpleButton("Clipping Planes", delegate ()
			{
				MelonCoroutines.Start(ChangeClipSetting());
			});
			settingsMenu.AddSpacer();
			settingsMenu.AddSimpleButton("Back", delegate ()
			{
				settingsMenu.Hide();
			});

			loadCamMenu();
		}

		private void loadCamMenu()
		{
			if (settings.Resolutions.Count <= 6)
			{
				camMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
			}
			else
			{
				camMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
			}
			addResButtons();
			addSettingsBackButtons();
		}

		private void addResButtons()
		{
			foreach (Resolution res in settings.Resolutions)
			{
				camMenu.AddSimpleButton(res.Name, delegate ()
				{
					SetResolution(res.ImageHeight, res.ImageWidth);
				});
			}
		}

		private void addSettingsBackButtons()
		{
			int numSpaces;

			if(settings.Resolutions.Count <= 6)
			{
				numSpaces = 1;
				if (settings.Resolutions.Count % 3 != 0)
				{
					numSpaces += (3 - (settings.Resolutions.Count % 3));
				}
			}
			else
			{
				numSpaces = 2;
				if (settings.Resolutions.Count % 4 != 0)
				{
					numSpaces += (4 - (settings.Resolutions.Count % 4));
				}
			}

			for (int i = 0; i < numSpaces; i++)
			{
				camMenu.AddSpacer();
			}

			camMenu.AddSimpleButton("Settings", delegate ()
			{
				settingsMenu.Show();
			});
			camMenu.AddSimpleButton("Back", delegate ()
			{
				camMenu.Hide();
			});
		}

		private IEnumerator addCam()
		{
			string name = "";
			int height = -2;
			int width = -2;

			BuiltinUiUtils.ShowInputPopup("Enter Resolution Name:", "", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				name = s;
			}, null, "Name...", true, null);
			while (name.Equals(""))
			{
				yield return new WaitForEndOfFrame();
			}

			BuiltinUiUtils.ShowInputPopup("Enter Resolution Height:", "", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!int.TryParse(s, out height) || height < 1)
				{
					height = 1080;
				}
			}, null, "Height...", true, null);
			while (height == -2)
			{
				yield return new WaitForEndOfFrame();
			}

			BuiltinUiUtils.ShowInputPopup("Enter Resolution Width:", "-1", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!int.TryParse(s, out width) || width < 1)
				{
					width = -1;
				}
			}, null, "Width...", true, null);
			while (width == -2)
			{
				yield return new WaitForEndOfFrame();
			}

			settings.Resolutions.Add(new Resolution(name, height, width));
			writeConfig();
			loadCamMenu();

			yield break;
		}

		private IEnumerator removeCam()
		{
			string name = "";
			int index = -1;
			bool flag = false;

			BuiltinUiUtils.ShowInputPopup("Delete Resolution:", "", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!int.TryParse(s, out index))
				{
					name = s;
					flag = true;
				}
				else
				{
					index -= 1;
				}
			}, null, "Name or Index (starting at 1)", true, null);
			while (name.Equals("") & index == -1)
			{
				yield return new WaitForEndOfFrame();
			}
			if (flag)
			{
				for (int i = 0; i < settings.Resolutions.Count; i++)
				{
					if (name.ToLower().Equals(settings.Resolutions[i].Name.ToLower()))
					{
						settings.Resolutions.RemoveAt(i);
					}
				}
			}
			else
			{
				try
				{
					settings.Resolutions.RemoveAt(index);
				}
				catch { }
			}
			writeConfig();
			loadCamMenu();

			yield break;
		}

		private IEnumerator ChangeDefaultAspectRatio()
		{
			float AspectWidth = -1;
			float AspectHeight = -1;

			BuiltinUiUtils.ShowInputPopup("Aspect Ration Width:", "16", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!float.TryParse(s, out AspectWidth) || AspectWidth < 0)
				{
					AspectWidth = 16;
				}
			}, null, "", true, null);
			while (AspectWidth == -1)
			{
				yield return new WaitForEndOfFrame();
			}

			BuiltinUiUtils.ShowInputPopup("Aspect Ration Height:", "9", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!float.TryParse(s, out AspectHeight) || AspectHeight < 0)
				{
					AspectHeight = 9;
				}
			}, null, "", true, null);
			while (AspectHeight == -1)
			{
				yield return new WaitForEndOfFrame();
			}

			settings.Width = AspectWidth;
			settings.Height = AspectHeight;
			aspectRatio = AspectWidth / AspectHeight;
			writeConfig();

			yield break;
		}

		private IEnumerator ChangeDefaultRes()
		{
			int defaultRes = -1;

			BuiltinUiUtils.ShowInputPopup("Default Mode:", "1", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!int.TryParse(s, out defaultRes) || !(defaultRes > 0 && defaultRes < settings.Resolutions.Count))
				{
					defaultRes = 1;
				}
			}, null, "Default mode (starts at 1)", true, null);
			while (defaultRes == -1)
			{
				yield return new WaitForEndOfFrame();
			}

			settings.DefaultRes = defaultRes;
			writeConfig();
			SetResolution(settings.Resolutions[settings.DefaultRes - 1].ImageHeight, settings.Resolutions[settings.DefaultRes - 1].ImageWidth);

			yield break;
		}

		private IEnumerator ChangeClipSetting()
		{
			float nearclip = -1;
			float farclip = -1;
			BuiltinUiUtils.ShowInputPopup("Near Clipping point", "0.01", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!float.TryParse(s, out nearclip) || nearclip < 0)
				{
					nearclip = 0.01f;
				}
			}, null, "Near Clipping Point", true, null);
			while (nearclip == -1)
			{
				yield return new WaitForEndOfFrame();
			}
			BuiltinUiUtils.ShowInputPopup("Far Clipping point", "2500", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				if (!float.TryParse(s, out farclip) || farclip < 0)
				{
					nearclip = 2500f;
				}
			}, null, "Near Clipping Point", true, null);
			while (farclip == -1)
			{
				yield return new WaitForEndOfFrame();
			}
			settings.NearClip = nearclip;
			settings.FarClip = farclip;
			writeConfig();
			MelonCoroutines.Start(CamClipping());
		}

		private IEnumerator CamClipping()
		{
			yield return new WaitForSecondsRealtime(15f);
			var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
			Camera cameraCam = cameraController.photoCamera.GetComponent<Camera>();
			cameraCam.nearClipPlane = settings.NearClip;
			cameraCam.farClipPlane = settings.FarClip;
		}

		private void writeConfig()
		{
			string conf = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText(configFile, conf);
		}

		private void readConfig()
		{
			string conf = File.ReadAllText(configFile);
			settings = JsonConvert.DeserializeObject<Settings>(conf);
			aspectRatio = settings.Width / settings.Height;
		}

		private void initConfig()
		{
			System.Collections.Generic.List<Resolution> res = new System.Collections.Generic.List<Resolution>();
			res.Add(new Resolution("1080p", 1080, -1));
			res.Add(new Resolution("4K", 2160, -1));
			res.Add(new Resolution("8K", 4320, -1));
			settings = new Settings(16, 9, 1, 0.01f, 2000f, res);
			writeConfig();
		}
	}

	public class Resolution
	{
		public string Name { get; set; }
		public int ImageHeight { get; set; }
		public int ImageWidth { get; set; }
		public Resolution(string name, int imageH, int imageW)
		{
			Name = name;
			ImageHeight = imageH;
			ImageWidth = imageW;
		}
	}

	public class Settings
	{
		public float Width { get; set; }
		public float Height { get; set; }
		public int DefaultRes { get; set; }
		public float NearClip { get; set; }
		public float FarClip { get; set; }
		public System.Collections.Generic.List<Resolution> Resolutions { get; set; }
		public Settings(float w, float h, int defRes, float nearclip, float farclip, System.Collections.Generic.List<Resolution> reses)
		{
			Width = w;
			Height = h;
			DefaultRes = defRes;
			NearClip = nearclip;
			FarClip = farclip;
			Resolutions = reses;
		}
	}


}
