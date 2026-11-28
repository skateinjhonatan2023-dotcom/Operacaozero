using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal static class OZFieldAnimation
{
    // Mixamo suffixes survive both FBX namespace conventions (":" and "_").
    static string BoneSuffix(HumanBodyBones bone)
    {
        string n = bone.ToString();
        if (n == "Chest") return "Spine1";
        if (n == "UpperChest") return "Spine2";
        if (n.Contains("Eye") || n == "Jaw") return null;
        return n.Replace("UpperLeg","UpLeg").Replace("LowerLeg","Leg").Replace("Toes","ToeBase")
            .Replace("UpperArm","Arm").Replace("LowerArm","ForeArm").Replace("Little","Pinky")
            .Replace("Thumb","HandThumb").Replace("Index","HandIndex").Replace("Middle","HandMiddle")
            .Replace("Ring","HandRing").Replace("Pinky","HandPinky")
            .Replace("Proximal","1").Replace("Intermediate","2").Replace("Distal","3");
    }

    internal static void Import(ModelImporter importer, string path)
    {
        if (importer.animationType == ModelImporterAnimationType.Human &&
            importer.clipAnimations.Length >= 18 && importer.clipAnimations.Any(c => c.loopTime)) return;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var copy = UnityEngine.Object.Instantiate(source);
        try
        {
            copy.name = source.name;
            var nodes = copy.GetComponentsInChildren<Transform>(true);
            var mapping = new System.Collections.Generic.Dictionary<HumanBodyBones, Transform>();
            for (int i=0; i<(int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                string suffix = BoneSuffix(bone);
                if (suffix == null) continue;
                var t = nodes.FirstOrDefault(b => b.name.EndsWith(suffix,StringComparison.Ordinal));
                if (t) mapping[bone] = t;
            }
            // The source bind pose has lowered arms. Define the avatar's T-pose without changing the mesh bindposes.
            Vector3 right = (mapping[HumanBodyBones.RightUpperArm].position - mapping[HumanBodyBones.LeftUpperArm].position).normalized;
            void Align(HumanBodyBones joint, HumanBodyBones child, Vector3 axis)
            {
                Transform a=mapping[joint], b=mapping[child];
                a.rotation=Quaternion.FromToRotation(b.position-a.position,axis)*a.rotation;
            }
            Align(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,-right);
            Align(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-right);
            Align(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,right);
            Align(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,right);
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;
            importer.optimizeGameObjects=false;
            var desc=importer.humanDescription;
            desc.human=mapping.Select(k => new HumanBone { boneName=k.Value.name,
                humanName=HumanTrait.BoneName[(int)k.Key], limit=new HumanLimit {useDefaultValues=true} }).ToArray();
            desc.skeleton=nodes.Select(t=>new SkeletonBone {name=t.name,position=t.localPosition,
                rotation=t.localRotation,scale=t.localScale}).ToArray();
            desc.upperArmTwist=desc.lowerArmTwist=desc.upperLegTwist=desc.lowerLegTwist=.5f;
            desc.armStretch=desc.legStretch=.05f;
            importer.humanDescription=desc;
            var clips=importer.defaultClipAnimations;
            foreach(var motion in clips)
            {
                string n=motion.name.Split('|').Last();
                motion.loopTime=n.Contains("walk") || n.Contains("idle") || n=="ready_sprint" || n=="ready_run" || n=="jump_loop";
                motion.lockRootRotation=true; motion.lockRootPositionXZ=true; motion.lockRootHeightY=true;
                motion.keepOriginalPositionY=true; motion.keepOriginalOrientation=true;
            }
            importer.clipAnimations=clips;
            importer.SaveAndReimport();
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
    }

    internal static AnimatorController Grounded(OZActor actor,OZFieldRig rig)
    {
        const string path="Assets/Content/Generated/FieldLocomotion09.controller";
        var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(existing) return existing;
        var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Lateral",AnimatorControllerParameterType.Float);
        controller.AddParameter("Forward",AnimatorControllerParameterType.Float);
        var state=controller.CreateBlendTreeInController("Grounded",out BlendTree tree);
        tree.blendType=BlendTreeType.FreeformDirectional2D;
        tree.blendParameter="Lateral"; tree.blendParameterY="Forward";
        ChildMotion At(AnimationClip clip,float x,float y,float cadence=1) =>
            new ChildMotion {motion=clip,position=new Vector2(x,y),timeScale=cadence};
        tree.children=new[]{
            At(actor.idle,0,0),At(actor.walk,0,1.29f),At(rig.jog,0,2.28f),At(actor.run,0,3.15f),
            At(rig.left,-1.6f,0,2.02f),At(rig.right,1.6f,0,2.1f),At(rig.back,0,-1.6f,2f)
        };
        controller.layers[0].stateMachine.defaultState=state;
        EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);
        return controller;
    }
}
