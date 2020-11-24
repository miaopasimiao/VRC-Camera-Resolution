using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnhollowerRuntimeLib.XrefScans;
using VRC.UserCamera;
using UIExpansionKit;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(CameraResolution.Main), "Camera Resolution", "1.1.0", "Dannie")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace CameraResolution
{
	public class Main : MelonMod
	{
		private const string camconfig = @"UserData/CamConfig.json";
		private Settings settings;
		private float aspectRatio;

		public override void OnApplicationStart()
		{
			if (!File.Exists(camconfig)) initConfig();
			readConfig();
		}

		public override void VRChat_OnUiManagerInit()
		{
			initButtons();
			SetResolution(settings.Resolutions[settings.DefaultRes - 1].ImageHeight, settings.Resolutions[settings.DefaultRes - 1].ImageWidth);
		}

		private void SetResolution(int photoHeight, int photoWidth)
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

		private void initButtons()
		{
			var camMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);
			ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Resolution", delegate ()
			{
				camMenu.Show();
			});

			createResButtons(ref camMenu);

			int numSpaces = 2;
			if (settings.Resolutions.Count % 4 != 0) numSpaces += 4 - (settings.Resolutions.Count % 4);
			for (int i = 0; i < numSpaces; i++)
			{
				camMenu.AddSpacer();
			}

			var settingsMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

			camMenu.AddSimpleButton("Settings", delegate ()
			{
				settingsMenu.Show();
			});
			camMenu.AddSimpleButton("Back", delegate ()
			{
				camMenu.Hide();
			});

			settingsMenu.AddLabel("\nChange Resolutions (requires restart)");
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
			settingsMenu.AddSpacer();
			settingsMenu.AddSimpleButton("Back", delegate ()
			{
				settingsMenu.Hide();
			});
		}

		private void createResButtons(ref ICustomShowableLayoutedMenu menu)
		{
			foreach (Resolution res in settings.Resolutions)
			{
				menu.AddSimpleButton(res.Name, delegate ()
				{
					SetResolution(res.ImageHeight, res.ImageWidth);
				});
			}
		}


		private IEnumerator addCam()
		{
			string name = "";
			int height = -2;
			int width = -2;

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Enter Resolution Name:", "", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
			{
				name = s;
			}, null, "Name...", true, null);
			while (name.Equals(""))
			{
				yield return new WaitForEndOfFrame();
			}

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Enter Resolution Height:", "", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Enter Resolution Width:", "-1", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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

			yield break;
		}

		private IEnumerator removeCam()
		{
			string name = "";
			int index = -1;
			bool flag = false;
			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Delete Resolution:", "", InputField.InputType.Standard, false, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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
			yield break;
		}

		private IEnumerator ChangeDefaultAspectRatio()
		{
			float AspectWidth = -1;
			float AspectHeight = -1;

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Aspect Ration Width:", "16", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Aspect Ration Height:", "9", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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

			UIExpansionKit.API.BuiltinUiUtils.ShowInputPopup("Default Mode:", "1", InputField.InputType.Standard, true, "Okay", delegate (string s, Il2CppSystem.Collections.Generic.List<KeyCode> k, Text t)
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

		private void writeConfig()
		{
			string conf = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText(camconfig, conf);
		}

		private void readConfig()
		{
			string conf = File.ReadAllText(camconfig);
			settings = JsonConvert.DeserializeObject<Settings>(conf);
			aspectRatio = settings.Width / settings.Height;
		}

		private void initConfig()
		{
			System.Collections.Generic.List<Resolution> res = new System.Collections.Generic.List<Resolution>();
			res.Add(new Resolution("1080p", 1080, -1));
			res.Add(new Resolution("4K", 2160, -1));
			res.Add(new Resolution("8K", 4320, -1));
			settings = new Settings(16, 9, 1, res);
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
		public System.Collections.Generic.List<Resolution> Resolutions { get; set; }
		public Settings(float w, float h, int defRes, System.Collections.Generic.List<Resolution> reses)
		{
			Width = w;
			Height = h;
			DefaultRes = defRes;
			Resolutions = reses;
		}
	}

}
