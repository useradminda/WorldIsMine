using UnityEngine;
using UnityEngine.UI;

public class BattleSoldierQueuePanel : MonoBehaviour
{
    [SerializeField] private Text redWaitingSoldierText;
    [SerializeField] private Text blueWaitingSoldierText;

    public void SetWaitingSoldierCount(int redCount, int blueCount)
    {
        SetText(redWaitingSoldierText, redCount);
        SetText(blueWaitingSoldierText, blueCount);
    }

    private void SetText(Text target, int value)
    {
        if (target != null)
        {
            target.text = Mathf.Max(0, value).ToString();
        }
    }
}
