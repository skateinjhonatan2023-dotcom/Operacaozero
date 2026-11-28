using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class OZFieldCapture
{
    // Capture the built scene only on workers with a graphics device. No synthetic image on headless hosts.
    internal static void TryCapture(OZGame game)
    {
        if (!Application.isBatchMode || SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) return;
        var existing=RenderTexture.active;
        var shot=RenderTexture.GetTemporary(960,540,24);
        var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        var cameraObject=new GameObject("Verificação visual");
        var actor=Object.Instantiate(game.soldier,game.spawns[0].position,Quaternion.identity);
        try
        {
            actor.gameObject.SetActive(true);
            var animator=actor.GetComponentInChildren<Animator>();
            animator.Rebind();
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                try {AnimationMode.SampleAnimationClip(animator.gameObject,actor.idle,actor.idle.length*.36f);}
                finally {AnimationMode.EndSampling();}
                var rig=actor.GetComponent<OZFieldRig>();
                var palm=Vector3.Lerp(rig.rightHand.position,rig.rightKnuckle.position,.5f);
                rig.grip.SetPositionAndRotation(palm,Quaternion.LookRotation(rig.leftHand.position-palm,Vector3.up));
                var cam=cameraObject.AddComponent<Camera>();cam.CopyFrom(game.view);cam.enabled=false;
                cam.transform.position=actor.transform.position+new Vector3(1.65f,1.48f,2.75f);
                cam.transform.LookAt(actor.transform.position+Vector3.up*.97f);
                cam.targetTexture=shot;cam.fieldOfView=36;cam.Render();
                RenderTexture.active=shot;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                var colors=pixels.GetPixels32();int lo=255,hi=0;
                for(int i=0;i<colors.Length;i+=23){int y=(colors[i].r+colors[i].g+colors[i].b)/3;lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
                if(hi-lo>30)File.WriteAllBytes("Assets/StreamingAssets/OZ09-Cena.png",pixels.EncodeToPNG());
            }
            finally {AnimationMode.StopAnimationMode();}
        }
        catch(System.Exception error){Debug.LogWarning("OZ09 captura: "+error.Message);}
        finally
        {
            Object.DestroyImmediate(actor.gameObject);Object.DestroyImmediate(cameraObject);
            RenderTexture.active=existing;RenderTexture.ReleaseTemporary(shot);Object.DestroyImmediate(pixels);
        }
    }
}
