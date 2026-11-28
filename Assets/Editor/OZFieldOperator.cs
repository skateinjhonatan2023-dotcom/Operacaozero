using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class OZFieldOperator
{
    const string Folder="Assets/Content/FieldOperator/";
    const string File=Folder+"FieldOperator.fbx";
    internal static GameObject Create()
    {
        var importer=AssetImporter.GetAtPath(File) as ModelImporter;
        if(!importer) throw new InvalidOperationException("FBX do operador de campo ausente.");
        OZFieldAnimation.Import(importer,File);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(File);
        var clips=AssetDatabase.LoadAllAssetsAtPath(File).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))
            .ToDictionary(c=>c.name.Split('|').Last());
        AnimationClip Clip(string key)=>clips.TryGetValue(key,out var clip) && clip.length>0 ? clip :
            throw new InvalidOperationException("Tomada ausente: "+key);
        var root=new GameObject("Operador de campo");
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);
            var animator=model.GetComponentInChildren<Animator>();
            if(!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Avatar Humanoid de campo inválido.");
            animator.applyRootMotion=false;
            var field=root.AddComponent<OZFieldRig>();
            var bones=model.GetComponentsInChildren<Transform>(true);
            Transform Bone(string suffix)=>bones.FirstOrDefault(b=>b.name.EndsWith(suffix,StringComparison.Ordinal)) ??
                throw new InvalidOperationException("Osso ausente: "+suffix);
            field.body=model.transform;
            field.leftToe=Bone("LeftToeBase");field.rightToe=Bone("RightToeBase");
            field.spine=Bone("Spine1");field.leftHand=Bone("LeftHand");field.rightHand=Bone("RightHand");
            field.leftKnuckle=Bone("LeftHandMiddle1");field.rightKnuckle=Bone("RightHandMiddle1");
            var front=Vector3.ProjectOnPlane(field.leftToe.position-Bone("LeftFoot").position,Vector3.up);
            if(front.sqrMagnitude>.00001f) model.transform.rotation=Quaternion.FromToRotation(front.normalized,Vector3.forward)*model.transform.rotation;
            float stature=Bone("Head").position.y-field.leftToe.position.y;
            if(stature>1.2f && stature<2.4f) model.transform.localScale*=1.67f/stature;
            model.transform.localPosition+=Vector3.up*(root.transform.position.y+.025f-Mathf.Min(field.leftToe.position.y,field.rightToe.position.y));
            field.grip=new GameObject("WeaponGrip").transform;field.grip.SetParent(root.transform,false);
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var close=skins.Single(r=>r.name=="FieldBody");var far=skins.Single(r=>r.name=="FieldDistant");
            foreach(var mesh in skins)mesh.sharedMaterials=mesh.sharedMaterials.Select(m=>Surface(m.name)).ToArray();
            var lod=root.AddComponent<LODGroup>();
            lod.SetLODs(new[]{new LOD(.10f,new Renderer[]{close}),new LOD(.003f,new Renderer[]{far})});lod.RecalculateBounds();
            var actor=root.AddComponent<OZActor>();
            actor.idle=Clip("ready_idle");actor.walk=Clip("ready_walk");actor.run=Clip("ready_sprint");
            actor.aim=Clip("aim_idle");actor.fire=Clip("shoot");actor.death=Clip("death");actor.hit=Clip("hit");
            actor.crouch=Clip("ready_crouch_idle");actor.crouchWalk=Clip("ready_crouch_walk");
            actor.airborne=Clip("jump_loop");actor.reload=Clip("reload");
            const string maskPath="Assets/Content/Generated/FieldUpper.mask";
            var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if(!mask){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,maskPath);}
            var hierarchy=animator.GetComponentsInChildren<Transform>(true);
            mask.transformCount=hierarchy.Length;
            for(int i=0;i<hierarchy.Length;i++)
            {
                mask.SetTransformPath(i,AnimationUtility.CalculateTransformPath(hierarchy[i],animator.transform));
                mask.SetTransformActive(i,hierarchy[i].IsChildOf(field.spine));
            }
            EditorUtility.SetDirty(mask);
            for(int part=0;part<(int)AvatarMaskBodyPart.LastBodyPart;part++)
            {
                var id=(AvatarMaskBodyPart)part;
                mask.SetHumanoidBodyPartActive(id,id==AvatarMaskBodyPart.Body || id==AvatarMaskBodyPart.Head ||
                    id==AvatarMaskBodyPart.LeftArm || id==AvatarMaskBodyPart.RightArm ||
                    id==AvatarMaskBodyPart.LeftFingers || id==AvatarMaskBodyPart.RightFingers);
            }
            field.upperMask=mask;
            field.jog=Clip("ready_run");field.aimWalk=Clip("aim_walk");field.back=Clip("mocap_walk_B@0.7285");
            field.left=Clip("mocap_walk_L@0.7455");field.right=Clip("mocap_walk_R@0.7148");
            field.takeoff=Clip("jump_start");field.land=Clip("jump_land");
            field.groundedController=OZFieldAnimation.Grounded(actor,field);
            Debug.Log("OZ09: Avatar Humanoid, Blend Tree, 18 tomadas e dois níveis de detalhe.");
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    static Material Surface(string name)
    {
        string path="Assets/Content/Generated/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        foreach(string id in new[]{"FieldColor","FieldNormal","FieldMask"})
            OZContent.ConfigureTexture(Folder+id+".png",1024,id=="FieldNormal" ? TextureImporterType.NormalMap : TextureImporterType.Default,id=="FieldMask");
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"FieldColor.png"));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"FieldNormal.png"));
        m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"FieldMask.png"));
        m.SetFloat("_BumpScale",.48f);m.SetFloat("_Smoothness",.85f);
        m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);return m;
    }
}
