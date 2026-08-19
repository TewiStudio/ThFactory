using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIBackgroundShadowTween : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    private void OnValidate()
    {
        if (canvasGroup != null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {

    }

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        canvasGroup.alpha = .01f;
        Sequence.Create().ChainDelay(.25f).Chain(Tween.Custom(.01f, 1f, .45f, newVal => canvasGroup.alpha = newVal));
    }
}
