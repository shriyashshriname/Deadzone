using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Deadzone.Editor
{
    /// <summary>Small helpers to (re)build DEADZONE animator controllers idempotently.</summary>
    public static class AnimatorControllerUtil
    {
        /// <summary>Loads or creates the controller and clears params/states/blend trees (keeps the GUID).</summary>
        public static AnimatorController Reset(string path)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>().ToArray())
                AssetDatabase.RemoveObjectFromAsset(o);
            return ctrl;
        }

        /// <summary>Speed-driven 1D blend tree state, playback scaled by AnimSpeed. Becomes the default state.</summary>
        public static AnimatorState AddLocomotion(AnimatorController ctrl, AnimatorStateMachine sm, params (AnimationClip clip, float threshold)[] motions)
        {
            var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            foreach (var (clip, th) in motions)
                if (clip != null) tree.AddChild(clip, th);
            var state = sm.AddState("Locomotion");
            state.motion = tree;
            state.speedParameterActive = true;
            state.speedParameter = "AnimSpeed";
            sm.defaultState = state;
            return state;
        }

        /// <summary>Trigger-driven one-shot (Attack / Hit) from Any State that returns to locomotion.</summary>
        public static AnimatorState AddOneShot(AnimatorController ctrl, AnimatorStateMachine sm, AnimatorState returnTo, string trigger, AnimationClip clip, float speed = 1f)
        {
            if (clip == null) return null;
            if (!ctrl.parameters.Any(p => p.name == trigger)) ctrl.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            var st = sm.AddState(trigger);
            st.motion = clip;
            st.speed = speed;
            var any = sm.AddAnyStateTransition(st);
            any.AddCondition(AnimatorConditionMode.If, 0, trigger);
            any.duration = 0.06f;
            any.canTransitionToSelf = false;
            var back = st.AddTransition(returnTo);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.12f;
            return st;
        }

        /// <summary>Terminal death state (no exit).</summary>
        public static AnimatorState AddDeath(AnimatorController ctrl, AnimatorStateMachine sm, AnimationClip clip)
        {
            if (clip == null) return null;
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
            var st = sm.AddState("Dead");
            st.motion = clip;
            var any = sm.AddAnyStateTransition(st);
            any.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            any.duration = 0.05f;
            any.canTransitionToSelf = false;
            return st;
        }
    }
}
