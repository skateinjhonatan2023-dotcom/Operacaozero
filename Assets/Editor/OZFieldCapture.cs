using System.IO;
using System.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class OZFieldCapture
{
    // Capture the built scene only on workers with a graphics device. No synthetic image on headless hosts.
    internal static void TryCapture(OZGame game)
    {
        if (!Application.isBatchMode) return;
        bool graphics=SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null;
        var existing=RenderTexture.active;
        var shot=graphics ? RenderTexture.GetTemporary(960,540,24) : null;
        var pixels=graphics ? new Texture2D(960,540,TextureFormat.RGB24,false) : null;
        var cameraObject=new GameObject("Verificação visual");
        var actor=Object.Instantiate(game.soldier,game.spawns[0].position,Quaternion.identity);
        try
        {
            actor.gameObject.SetActive(true);
            var capsule=OZGame.SettleAtSpawn(actor,game.spawns[0].position);
            var rig=actor.GetComponent<OZFieldRig>();
            rig.BindPose();rig.adjustFootContacts=true;
            var animator=actor.GetComponentInChildren<Animator>();
            animator.Rebind();
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                try {AnimationMode.SampleAnimationClip(animator.gameObject,actor.idle,actor.idle.length*.36f);}
                finally {AnimationMode.EndSampling();}
                // Use exactly the same foot and grip solver as the player, after the capsule settles.
                rig.EvaluateContacts(.5f);
                if(!capsule.isGrounded)throw new System.InvalidOperationException("Cápsula sem piso na prévia.");
                float gap=MeasureSole(actor);
                if(Mathf.Abs(gap)>.075f)throw new System.InvalidOperationException("OZ09: distância da sola ao piso "+gap+" m.");
                File.WriteAllText("Assets/StreamingAssets/OZ09-Teste.txt",
                    "Pose idle, menor altura real da malha da bota sobre o piso (m): "+gap.ToString("F4",CultureInfo.InvariantCulture));
                if(!graphics)return;
                var cam=cameraObject.AddComponent<Camera>();cam.CopyFrom(game.view);cam.enabled=false;
                cam.transform.position=actor.transform.position+new Vector3(-2.05f,1.37f,3.45f);
                cam.transform.LookAt(actor.transform.position+Vector3.up*.97f);
                cam.targetTexture=shot;cam.fieldOfView=36;cam.Render();
                RenderTexture.active=shot;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                var colors=pixels.GetPixels32();int lo=255,hi=0;
                for(int i=0;i<colors.Length;i+=23){int y=(colors[i].r+colors[i].g+colors[i].b)/3;lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
                if(hi-lo>30)File.WriteAllBytes("Assets/StreamingAssets/OZ09-Cena.png",pixels.EncodeToPNG());
            }
            finally {AnimationMode.StopAnimationMode();}
        }
        finally
        {
            Object.DestroyImmediate(actor.gameObject);Object.DestroyImmediate(cameraObject);
            RenderTexture.active=existing;if(shot)RenderTexture.ReleaseTemporary(shot);if(pixels)Object.DestroyImmediate(pixels);
        }
    }

    static float MeasureSole(OZActor actor)
    {
        var mesh=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="FieldBody");
        var baked=new Mesh();
        try
        {
            mesh.BakeMesh(baked);
            Vector3 sole=mesh.transform.TransformPoint(baked.vertices.OrderBy(v=>mesh.transform.TransformPoint(v).y).First());
            var floor=Physics.RaycastAll(sole+Vector3.up*.45f,Vector3.down,1.2f,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.transform.IsChildOf(actor.transform) && h.normal.y>.7f).OrderBy(h=>h.distance).First();
            return sole.y-floor.point.y;
        }
        finally {Object.DestroyImmediate(baked);}
    }
}
