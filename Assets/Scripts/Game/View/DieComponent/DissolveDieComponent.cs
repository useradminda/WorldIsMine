using UnityEngine;

public class DissolveDieComponent : DieBaseComponent
{
    [SerializeField] private float duration = 2f;

    private float elapsed;
    private bool playing;

    protected override void EnterDieState()
    {
        elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        mUnitView.SetDissolveAmount(0f);
        PlayDieActionForDuration(duration);
        playing = true;
    }

    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        elapsed += dt;
        mUnitView.SetDissolveAmount(Mathf.Clamp01(elapsed / duration));
        if (elapsed >= duration)
        {
            playing = false;
            mUnitView.SetDissolveAmount(0f);
            CycleUnitView();
        }
    }
}
