using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace YawOnMouse.Patches;

public class PilotPlayerStatePatches
{
    [HarmonyPatch(typeof(PilotPlayerState), "PlayerAxisControls")]
    static class PlayerAxisControls
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Plugin.Enabled.Value) return instructions;
            
            // default game behaviour is roll
            if (Plugin.AxisPatchType.Value == AxisPatchType.Roll) return instructions;
            
            var codes = new List<CodeInstruction>(instructions);

            if (Plugin.AxisPatchType.Value == AxisPatchType.NoRoll) return NoRoll(codes);
            if (Plugin.AxisPatchType.Value == AxisPatchType.Yaw) return Yaw(codes);
            
            return codes;
        }

        static IEnumerable<CodeInstruction> Yaw(List<CodeInstruction> codes)
        {
            /*
             *  IL_01f5: ldc.r4       150
                IL_01fa: div
                IL_01fb: stfld        float32 PilotPlayerState::rollInput
             */
            var codeMatcher = new CodeMatcher(codes)
                .MatchForward(
                    true, 
                    new CodeMatch(OpCodes.Ldc_R4), 
                    new CodeMatch(OpCodes.Div),
                    new CodeMatch(OpCodes.Stfld, 
                        AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.rollInput))
                    )
                )
                .Set(OpCodes.Stfld, AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.yawInput)));
            
            return codeMatcher.InstructionEnumeration();
        }

        static IEnumerable<CodeInstruction> NoRoll(List<CodeInstruction> codes)
        {
            int pitchIndex = 0; // To store the index of pitchInput's stfld
            int rollStartIndex = 0; // To store the starting index of rollInput's sequence

            var matcher = new CodeMatcher(codes);

            matcher
                .MatchForward(
                    true, 
                    new CodeMatch(OpCodes.Ldc_R4), 
                    new CodeMatch(OpCodes.Div), 
                    new CodeMatch(OpCodes.Stfld, 
                        AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.pitchInput))
                        )
                    );
            pitchIndex = matcher.Pos;
            
            matcher
                .MatchForward(
                    true, 
                    new CodeMatch(OpCodes.Ldc_R4), 
                    new CodeMatch(OpCodes.Div), 
                    new CodeMatch(OpCodes.Stfld,
                        AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.rollInput))
                        )
                    );
            rollStartIndex = matcher.Pos - 1;

            // Validate indices to avoid out-of-bounds access
            if (pitchIndex >= 0 && rollStartIndex > pitchIndex)
            {
                matcher
                    .Start()
                    .Advance(pitchIndex + 1)
                    .RemoveInstructions(rollStartIndex - pitchIndex)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldc_R4, 0.0f)
                    );
            }

            return matcher.InstructionEnumeration();
        }
    }
}