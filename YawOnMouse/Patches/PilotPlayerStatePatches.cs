using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using YawOnMouse.Helpers;

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

        /*
         * call BlacklistHelper::IsBlacklisted
         * brfalse -> yawLabel
         * ...
         * stfld PilotPlayerState::rollInput
         * br -> endLabel
         * yawLabel:
         *  ...
         *  stfld PilotPlayerState::yawInput
         * endlabel:
         */
        static IEnumerable<CodeInstruction> Yaw(List<CodeInstruction> codes)
        {
            var codeMatcher = new CodeMatcher(codes);
            MatchRollInputInstruction(codeMatcher, false);
            
            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not find instructions for Yaw");
                return codes;
            }

            int rollSeqStart = codeMatcher.Pos;

            // grab the entire sequence
            MatchRollInputInstruction(codeMatcher, true);

            int rollSeqEnd = codeMatcher.Pos;

            // clone the sequence
            var rollSequence = codes.GetRange(rollSeqStart, rollSeqEnd - rollSeqStart + 1);
            
            var yawSequence = new List<CodeInstruction>();
            foreach (var instruction in rollSequence)
            {
                if (instruction.opcode == OpCodes.Stfld && instruction.operand is FieldInfo fieldInfo &&
                    fieldInfo.Name == nameof(PilotPlayerState.rollInput))
                {
                    // set this.rollInput to this.yawInput
                    yawSequence.Add(new CodeInstruction(OpCodes.Stfld,
                        AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.yawInput)))
                    );
                }
                else
                {
                    yawSequence.Add(instruction.Clone());
                }
            }

            var yawLabel = new Label();
            var endLabel = new Label();
            
            // add label to yaw instructs
            yawSequence[0].labels.Add(yawLabel);
            
            // add label to end nop
            var endNop = new CodeInstruction(OpCodes.Nop);
            endNop.labels.Add(endLabel);

            // build replacement: [call IsBlacklisted, brfalse yawLabel, ...roll..., br endLabel, ...yaw..., nop]
            var newInstructions = new List<CodeInstruction>
            {
                new(OpCodes.Call, AccessTools.Method(typeof(BlacklistHelper), nameof(BlacklistHelper.IsBlacklisted))),
                new(OpCodes.Brfalse_S, yawLabel),
            };

            newInstructions.AddRange(rollSequence);
            newInstructions.Add(new CodeInstruction(OpCodes.Br_S, endLabel));
            newInstructions.AddRange(yawSequence);
            newInstructions.Add(endNop);

            // replace the original roll sequence with the new block
            codes.RemoveRange(rollSeqStart, rollSeqEnd - rollSeqStart + 1);
            codes.InsertRange(rollSeqStart, newInstructions);

            return codes;
        }

        private static void MatchRollInputInstruction(CodeMatcher codeMatcher, bool useEnd)
        {
            codeMatcher.MatchForward(
                useEnd,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldsfld),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Stfld,
                    AccessTools.Field(typeof(PilotPlayerState), nameof(PilotPlayerState.rollInput)))
            );
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

            // this.rollInput = 0.0
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