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
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            // default game behaviour is roll (no patch needed)
            if (Plugin.AxisPatchType.Value == AxisPatchType.Roll) return instructions;
            if (Plugin.AxisPatchType.Value == AxisPatchType.Yaw) return Yaw(codes, il);
            
            return codes;
        }

        /*
         * call PatchHelper::ShouldUseYaw
         * brtrue -> yawLabel
         * ...
         * stfld PilotPlayerState::rollInput
         * br -> endLabel
         * yawLabel:
         *  ...
         *  stfld PilotPlayerState::yawInput
         * endlabel:
         */
        static IEnumerable<CodeInstruction> Yaw(List<CodeInstruction> codes, ILGenerator il)
        {
            var codeMatcher = new CodeMatcher(codes);
            MatchRollInputInstruction(codeMatcher, false);
            
            if (codeMatcher.IsInvalid)
            {
                Plugin.Logger.LogError("Could not find instructions for Yaw");
                return codes;
            }

            int rollSeqStart = codeMatcher.Pos;
            var entryLabels = new List<Label>(codes[rollSeqStart].labels); // snapshot before removal
            
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

            var yawLabel = il.DefineLabel();
            var endLabel = il.DefineLabel();
            
            // add label to yaw instructs
            yawSequence[0].labels.Add(yawLabel);
            
            // add label to end nop
            var endNop = new CodeInstruction(OpCodes.Nop);
            endNop.labels.Add(endLabel);
            
            // build replacement: [call ShouldUseYaw, brtrue yawLabel, ...roll..., br endLabel, ...yaw..., nop]
            var newInstructions = new List<CodeInstruction>
            {
                new(OpCodes.Call, AccessTools.Method(typeof(PatchHelper), nameof(PatchHelper.ShouldUseYaw))),
                new(OpCodes.Brtrue_S, yawLabel),
            };
            
            newInstructions[0].labels.AddRange(entryLabels);
            
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
    }
}