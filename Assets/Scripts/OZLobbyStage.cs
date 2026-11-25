using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class OZLobbyStage : MonoBehaviour
{
    public Camera cameraView;
    public Transform backdrop;
    public Texture2D backdropTexture;
    public Animator animator;
    public AnimationClip idle;
    public Transform operatorRoot;
    public Transform rifle;

    PlayableGraph graph;
    AnimationClipPlayable motion;
    float aspect;

    void OnEnable()
    {
        if (!animator || !idle) return;
        graph = PlayableGraph.Create("Vigia • Lobby");
        motion = AnimationClipPlayable.Create(graph, idle);
        var output = AnimationPlayableOutput.Create(graph, "Vigia", animator);
        output.SetSourcePlayable(motion);
        graph.Play();
    }

    void LateUpdate()
    {
        if (graph.IsValid() && motion.GetTime() >= idle.length)
            motion.SetTime(motion.GetTime() % idle.length);
        if (!backdrop || !backdropTexture || !cameraView || Mathf.Approximately(aspect, cameraView.aspect)) return;

        aspect = cameraView.aspect;
        float depth = Vector3.Distance(cameraView.transform.position, backdrop.position);
        float frustumHeight = 2f * depth * Mathf.Tan(cameraView.fieldOfView * .5f * Mathf.Deg2Rad);
        float imageAspect = (float)backdropTexture.width / backdropTexture.height;
        float height = frustumHeight * Mathf.Max(1f, aspect / imageAspect) * 1.008f;
        backdrop.localScale = new Vector3(height * imageAspect, height, 1);
    }

    void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    internal void Apply(OZLobbyInventory inventory)
    {
        if (operatorRoot) inventory.Apply(operatorRoot.gameObject, rifle, true);
    }
}
