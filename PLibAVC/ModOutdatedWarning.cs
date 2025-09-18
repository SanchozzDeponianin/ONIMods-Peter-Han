﻿/*
 * Copyright 2025 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using UnityEngine;

namespace PeterHan.PLib.AVC {
	/// <summary>
	/// Added to the main menu to warn users if mods are out of date.
	/// </summary>
	internal sealed class ModOutdatedWarning : KMonoBehaviour {
		/// <summary>
		/// The singleton (should be!) instance of this class.
		/// </summary>
		internal static ModOutdatedWarning Instance { get; private set; }

		// Detour the "Resume Game" button
		private static readonly IDetouredField<MainMenu, KButton> RESUME_GAME = PDetours.
			DetourFieldLazy<MainMenu, KButton>(nameof(MainMenu.Button_ResumeGame));

		/// <summary>
		/// The button used to open the Mods screen.
		/// </summary>
		private GameObject modsButton;

		internal ModOutdatedWarning() {
			modsButton = null;
		}

		/// <summary>
		/// Finds the "Mods" button and stores it in the modsButton field.
		/// </summary>
		/// <param name="buttonParent">The parent of all the main menu buttons.</param>
		private void FindModsButton(Transform buttonParent) {
			int n = buttonParent.childCount;
			for (int i = 0; i < n; i++) {
				var button = buttonParent.GetChild(i).gameObject;
				// Match by text... sucks but unlikely to have changed this early
				if (button != null && button.GetComponentInChildren<LocText>()?.text ==
						STRINGS.UI.FRONTEND.MODS.TITLE) {
					modsButton = button;
					break;
				}
			}
			if (modsButton == null)
				PUtil.LogWarning("Unable to find Mods menu button, main menu update warning will not be functional");
		}

		protected override void OnCleanUp() {
			Instance = null;
			base.OnCleanUp();
		}

		protected override void OnPrefabInit() {
			base.OnPrefabInit();
			var mm = GetComponent<MainMenu>();
			Instance = this;
			try {
				Transform buttonParent;
				KButton resumeButton;
				// "Resume game" is in the same panel as "Mods"
				if (mm != null && (resumeButton = RESUME_GAME.Get(mm)) != null &&
						(buttonParent = resumeButton.transform.parent) != null)
					FindModsButton(buttonParent);
			} catch (DetourException) { }
			UpdateText();
		}

		/// <summary>
		/// Updates the Mods button text.
		/// </summary>
		private void UpdateText() {
			var inst = PVersionCheck.Instance;
			if (modsButton != null && inst != null) {
				var modsText = modsButton.GetComponentInChildren<LocText>();
				int outdated = inst.OutdatedMods;
				if (outdated > 0 && modsText != null) {
					string text = STRINGS.UI.FRONTEND.MODS.TITLE;
					if (outdated == 1)
						text += PLibStrings.MAINMENU_UPDATE_1;
					else
						text += string.Format(PLibStrings.MAINMENU_UPDATE, outdated);
					modsText.text = text;
				}
			}
		}

		/// <summary>
		/// Updates the button text in a coroutine after one frame.
		/// </summary>
		public System.Collections.IEnumerator UpdateTextThreaded() {
			yield return null;
			UpdateText();
		}
	}
}
