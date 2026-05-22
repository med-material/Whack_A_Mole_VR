using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SandboxRunner))]
[CanEditMultipleObjects]
public class SandboxRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SandboxRunner runner = (SandboxRunner)target;
        if (runner == null || runner.CurrentExperimentMode != SandboxRunner.ExperimentMode.Participant)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Participant Run Preview", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            $"Participant ID: P{runner.CurrentStudyGlobalParticipantId} | Auto-assigned input: {runner.CurrentStudyInputMode} | Modality index: {runner.CurrentStudyParticipantIndex} | Session: {runner.CurrentStudySessionIndex}/8\n" +
            $"Task order: {runner.CurrentStudyTaskOrderLabel}\n" +
            $"Current run: {runner.CurrentStudyResolvedTaskLabel} / {runner.CurrentStudyResolvedEffectLabel}\n" +
            $"Effect order for current task: {runner.CurrentStudyEffectSequencePreview}",
            MessageType.Info
        );

        string[] lines = runner.GetStudyRunPreviewLines();
        int currentRunIndex = Mathf.Clamp(runner.CurrentStudySessionIndex - 1, 0, Mathf.Max(0, lines.Length - 1));

        GUIStyle normalStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true
        };

        GUIStyle currentStyle = new GUIStyle(EditorStyles.helpBox)
        {
            richText = true,
            wordWrap = true,
            fontStyle = FontStyle.Bold
        };

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == currentRunIndex)
            {
                EditorGUILayout.LabelField($"<b>Current:</b> {lines[i]}", currentStyle);
            }
            else
            {
                EditorGUILayout.LabelField(lines[i], normalStyle);
            }
        }
    }
}
