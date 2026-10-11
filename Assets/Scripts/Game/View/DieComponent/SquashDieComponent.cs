using UnityEngine;
// 压扁
public class SquashDieComponent : DieBaseComponent
{
    [SerializeField] private float duration = 2f;

    private RenderComponent renderComponent;
    private float elapsed;
    private bool playing;

    private RenderComponent RenderComponent
    {
        get
        {
            if (renderComponent == null)
            {
                renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
            }
            return renderComponent;
        }
    }

    protected override void EnterDieState()
    {
        elapsed = 0f;
        playing = true;
        RenderComponent.SetPropertyBlockFloat("_SquashState", 1f);
    }

    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        elapsed += dt;
        if (elapsed < duration)
        {
            return;
        }

        playing = false;
        RenderComponent.SetPropertyBlockFloat("_SquashState", 0f);
        CycleUnitView();
    }
}
