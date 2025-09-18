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

using HarmonyLib;
using PeterHan.PLib.Core;
using System;
using System.Runtime.CompilerServices;

using ConduitUtilityNetworkManager = UtilityNetworkManager<FlowUtilityNetwork, Vent>;
using OnNetworksRebuilt = System.Action<System.Collections.Generic.IList<UtilityNetwork>,
	System.Collections.Generic.ICollection<int>>;
using TranspiledMethod = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;

namespace PeterHan.FastTrack.ConduitPatches {
	/// <summary>
	/// Extracts the UtilityNetworkManager.Update method for background updates. Will only be
	/// used for liquid and gas utility networks.
	/// </summary>
	[HarmonyPatch(typeof(ConduitUtilityNetworkManager), nameof(ConduitUtilityNetworkManager.
		Update))]
	public static class ConduitUtilityNetworkUpdater {
		internal static bool Prepare() => FastTrackOptions.Instance.ConduitOpts;

		/// <summary>
		/// Fires the update events for a conduit network manager after it is rebuilt.
		/// </summary>
		/// <param name="manager">The conduit network manager to trigger.</param>
		internal static void TriggerEvent(ConduitUtilityNetworkManager manager) {
			manager.onNetworksRebuilt?.Invoke(manager.GetNetworks(), manager.physicalNodes);
		}

		[HarmonyReversePatch]
		[HarmonyPatch(nameof(ConduitUtilityNetworkManager.Update))]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void Update(ConduitUtilityNetworkManager instance) {
			TranspiledMethod Transpiler(TranspiledMethod instructions) {
				var remove = typeof(OnNetworksRebuilt).GetMethodSafe(nameof(OnNetworksRebuilt.
					Invoke), false, PPatchTools.AnyArguments);
				return (remove == null) ? instructions : PPatchTools.RemoveMethodCall(
					instructions, remove);
			}
			_ = instance;
			_ = Transpiler(null);

			// Dummy code to ensure no inlining
			while (System.DateTime.Now.Ticks > 0L)
				throw new NotImplementedException("Reverse patch stub");
		}
	}
}
