﻿/*
 * Copyright 2019 Peter Han
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

using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PeterHan.PLib {
	/// <summary>
	/// Used for creating and managing UI elements.
	/// </summary>
	public sealed class PUIElements {
		/// <summary>
		/// A white color used for default backgrounds.
		/// </summary>
		public static readonly Color BG_WHITE = new Color32(255, 255, 255, 255);

		/// <summary>
		/// Represents an anchor in the center.
		/// </summary>
		private static readonly Vector2f CENTER = new Vector2f(0.5f, 0.5f);

		/// <summary>
		/// Represents an anchor in the lower left corner.
		/// </summary>
		private static readonly Vector2f LOWER_LEFT = new Vector2f(1.0f, 0.0f);

		/// <summary>
		/// Represents an anchor in the upper right corner.
		/// </summary>
		private static readonly Vector2f UPPER_RIGHT = new Vector2f(0.0f, 1.0f);

		/// <summary>
		/// Adds text describing a particular component if available.
		/// </summary>
		/// <param name="result">The location to append the text.</param>
		/// <param name="component">The component to describe.</param>
		private static void AddComponentText(StringBuilder result, Component component) {
			// Include all fields
			var fields = component.GetType().GetFields(BindingFlags.DeclaredOnly |
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			// Class specific
			if (component is LocText lt)
				result.AppendFormat(", Text={0}, Color={1}, Font={2}", lt.text, lt.color,
					lt.font);
			else if (component is Image ki)
				result.AppendFormat(", Color={0}", ki.color);
			else if (component is HorizontalOrVerticalLayoutGroup lg)
				result.AppendFormat(", Child Align={0}, Control W={1}, Control H={2}",
					lg.childAlignment, lg.childControlWidth, lg.childControlHeight);
			foreach (var field in fields) {
				object value = field.GetValue(component) ?? "null";
				// Value type specific
				if (value is LayerMask lm)
					value = "Layer #" + lm.value;
				result.AppendFormat(", {0}={1}", field.Name, value);
			}
		}

		/// <summary>
		/// Adds an auto-fit resizer to a UI element.
		/// </summary>
		/// <param name="uiElement">The element to resize.</param>
		/// <param name="mode">The sizing mode to use.</param>
		public static void AddSizeFitter(GameObject uiElement, ContentSizeFitter.FitMode mode =
				ContentSizeFitter.FitMode.MinSize) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var fitter = uiElement.AddOrGet<ContentSizeFitter>();
			fitter.horizontalFit = mode;
			fitter.verticalFit = mode;
			fitter.enabled = true;
			fitter.SetLayoutHorizontal();
			fitter.SetLayoutVertical();
		}

		/// <summary>
		/// Creates a button.
		/// </summary>
		/// <param name="parent">The parent which will contain the button.</param>
		/// <param name="name">The button name.</param>
		/// <param name="onClick">The action to execute on click (optional).</param>
		/// <returns>The matching button.</returns>
		public static GameObject CreateButton(GameObject parent, string name = null,
				System.Action onClick = null) {
			if (parent == null)
				throw new ArgumentNullException("parent");
			var button = CreateUI(parent, name ?? "Button");
			// Background
			var kImage = button.AddComponent<KImage>();
			kImage.colorStyleSetting = PUITuning.ButtonStylePink;
			kImage.color = PUITuning.ButtonColorPink;
			kImage.sprite = PUITuning.ButtonImage.sprite;
			kImage.type = Image.Type.Sliced;
			// Set on click event
			var kButton = button.AddComponent<KButton>();
			if (onClick != null)
				kButton.onClick += onClick;
			kButton.additionalKImages = new KImage[0];
			kButton.soundPlayer = PUITuning.ButtonSounds;
			kButton.bgImage = kImage;
			// Set colors
			kButton.colorStyleSetting = kImage.colorStyleSetting;
			button.AddComponent<LayoutElement>().flexibleWidth = 0;
			button.AddComponent<ToolTip>();
			// Add text to the button
			var textChild = CreateUI(button, "Text", new Insets());
			textChild.SetActive(false);
			// Add text component to display the text
			var text = textChild.AddComponent<LocText>();
			text.key = string.Empty;
			text.alignment = TMPro.TextAlignmentOptions.Center;
			text.textStyleSetting = PUITuning.ButtonTextStyle;
			text.font = PUITuning.ButtonFont;
			textChild.SetActive(true);
			button.SetActive(true);
			return button;
		}

		/// <summary>
		/// Creates a UI game object.
		/// </summary>
		/// <param name="parent">The object's parent.</param>
		/// <param name="name">The object name.</param>
		/// <param name="margins">The margins inside the parent object. Leave out to disable anchoring to parent.</param>
		/// <returns>The UI object with transform and canvas initialized.</returns>
		private static GameObject CreateUI(GameObject parent, string name, Insets margins =
				null) {
			var element = Util.NewGameObject(parent, name);
			// Size and position
			var transform = element.AddOrGet<RectTransform>();
			transform.localScale = Vector3.one;
			transform.pivot = CENTER;
			transform.anchoredPosition = CENTER;
			transform.anchorMax = UPPER_RIGHT;
			transform.anchorMin = LOWER_LEFT;
			if (margins != null) {
				transform.offsetMax = margins.GetOffsetMax();
				transform.offsetMin = margins.GetOffsetMin();
			}
			element.AddComponent<CanvasRenderer>();
			element.layer = LayerMask.NameToLayer("UI");
			return element;
		}

		/// <summary>
		/// Dumps information about the parent tree of the specified GameObject to the debug
		/// log.
		/// </summary>
		/// <param name="item">The item to determine hierarchy.</param>
		public static void DebugObjectHierarchy(GameObject item) {
			string info = "null";
			if (item != null) {
				var result = new StringBuilder(256);
				do {
					result.Append("- ");
					result.Append(item.name ?? "Unnamed");
					item = item.transform?.parent?.gameObject;
					if (item != null)
						result.AppendLine();
				} while (item != null);
				info = result.ToString();
			}
			PUtil.LogDebug("Object Tree:" + Environment.NewLine + info);
		}

		/// <summary>
		/// Dumps information about the specified GameObject to the debug log.
		/// </summary>
		/// <param name="root">The root hierarchy to dump.</param>
		public static void DebugObjectTree(GameObject root) {
			string info = "null";
			if (root != null)
				info = GetObjectTree(root, 0);
			PUtil.LogDebug("Object Dump:" + Environment.NewLine + info);
		}

		/// <summary>
		/// Creates a string recursively describing the specified GameObject.
		/// </summary>
		/// <param name="root">The root GameObject hierarchy.</param>
		/// <returns>A string describing this game object.</returns>
		private static string GetObjectTree(GameObject root, int indent) {
			var result = new StringBuilder(1024);
			// Calculate indent to make nested reading easier
			var solBuilder = new StringBuilder(indent);
			for (int i = 0; i < indent; i++)
				solBuilder.Append(' ');
			string sol = solBuilder.ToString();
			var transform = root.transform;
			int n = transform.childCount;
			// Basic information
			result.Append(sol).AppendFormat("GameObject[{0}, {1:D} child(ren), Layer {2:D}, " +
				"Active={3}]", root.name, n, root.layer, root.activeInHierarchy).AppendLine();
			// Transformation
			result.Append(sol).AppendFormat(" Translation={0} [{3}] Rotation={1} [{4}] " +
				"Scale={2}", transform.position, transform.rotation, transform.
				localScale, transform.localPosition, transform.localRotation).AppendLine();
			// Components
			foreach (var component in root.GetComponents<Component>()) {
				if (component is RectTransform rt) {
					// UI rectangle
					var rect = rt.rect;
					Vector2 pivot = rt.pivot, aMin = rt.anchorMin, aMax = rt.anchorMax;
					result.Append(sol).AppendFormat(" Rect[Coords=({0:F2},{1:F2}) " +
						"Size=({2:F2},{3:F2}) Pivot=({4:F2},{5:F2}) ", rect.xMin,
						rect.yMin, rect.width, rect.height, pivot.x, pivot.y);
					result.AppendFormat("AnchorMin=({0:F2},{1:F2}), AnchorMax=({2:F2}," +
						"{3:F2})]", aMin.x, aMin.y, aMax.x, aMax.y).AppendLine();
				} else if (component != null && !(component is Transform)) {
					// Exclude destroyed components and Transform objects
					result.Append(sol).Append(" Component[").Append(component.GetType().
						FullName);
					AddComponentText(result, component);
					result.AppendLine("]");
				}
			}
			// Children
			if (n > 0)
				result.Append(sol).AppendLine(" Children:");
			for (int i = 0; i < n; i++) {
				var child = transform.GetChild(i).gameObject;
				if (child != null)
					// Exclude destroyed objects
					result.AppendLine(GetObjectTree(child, indent + 2));
			}
			return result.ToString().TrimEnd();
		}

		/// <summary>
		/// Sets a UI element's minimum size.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="minSize">The minimum size in units.</param>
		public static void SetSize(GameObject uiElement, Vector2f minSize) {
			float minX = minSize.x, minY = minSize.y;
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var le = uiElement.GetComponent<LayoutElement>();
			if (le != null) {
				le.minWidth = minX;
				le.minHeight = minY;
			}
			/*var rt = uiElement.rectTransform();
			if (rt != null) {
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minX);
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minY);
				LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
			}*/
		}

		/// <summary>
		/// Sets a UI element's text.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="text">The text to display on the element.</param>
		public static void SetText(GameObject uiElement, string text) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var title = uiElement.GetComponentInChildren<LocText>();
			if (title != null)
				title.SetText(text ?? string.Empty);
		}

		/// <summary>
		/// Sets a UI element's tool tip.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="tooltip">The tool tip text to display when hovered.</param>
		public static void SetToolTip(GameObject uiElement, string tooltip) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			if (!string.IsNullOrEmpty(tooltip)) {
				var tooltipComponent = uiElement.AddOrGet<ToolTip>();
				tooltipComponent.toolTip = tooltip;
			}
		}

		/// <summary>
		/// Shows a confirmation or message dialog based on a prefab.
		/// </summary>
		/// <param name="prefab">The dialog to show.</param>
		/// <param name="parent">The dialog's parent.</param>
		/// <param name="message">The message to display.</param>
		/// <returns>The dialog created.</returns>
		public static ConfirmDialogScreen ShowConfirmDialog(GameObject prefab,
				GameObject parent, string message) {
			if (prefab == null)
				throw new ArgumentNullException("prefab");
			if (parent == null)
				throw new ArgumentNullException("parent");
			var confirmDialog = Util.KInstantiateUI(prefab, parent, false).GetComponent<
				ConfirmDialogScreen>();
			confirmDialog.PopupConfirmDialog(message, null, null, null, null,
				null, null, null, null, true);
			confirmDialog.gameObject.SetActive(true);
			return confirmDialog;
		}
	}
}