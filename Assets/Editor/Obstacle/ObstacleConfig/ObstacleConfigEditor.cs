#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObstacleConfig))]
public class ObstacleConfigEditor : Editor
{
    private void OnSceneGUI()
    {
        ObstacleConfig config = (ObstacleConfig)target;

        if (config.BorderList == null)
            return;

        Handles.color = Color.green;

        for (int i = 0; i < config.BorderList.Count; i++)
        {
            Vector3 worldPos = config.BorderList[i];
                //config.transform.TransformPoint();

            EditorGUI.BeginChangeCheck();

            Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Move Area Point");

                config.BorderList[i] = newPos;
                   // config.transform.InverseTransformPoint(newPos);

                EditorUtility.SetDirty(config);
            }

            Handles.SphereHandleCap(
                0,
                worldPos,
                Quaternion.identity,
                0.15f,
                EventType.Repaint);

            Handles.Label(worldPos + Vector3.up * 0.2f, (i + 1).ToString());

            if (i < config.BorderList.Count - 1)
            {
                Handles.DrawLine(
                    worldPos,
                        // config.transform.TransformPoint(
                        config.BorderList[i + 1]);//));
            }
            else if (config.BorderList.Count > 2)
            {
                Handles.DrawLine(
                    worldPos,
                        //config.transform.TransformPoint(
                        config.BorderList[0]);//);
            }
        }
    }
}
#endif