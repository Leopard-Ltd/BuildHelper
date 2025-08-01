#if ADDRESSABLE && BLUEPRINT_WORKFLOW && UNITY_WEBGL
namespace BuildHelper.Workflows{
using UnityEditor;

public class BlueprintWorkFlowWebGl : BlueprintWorkFlowAndroid
{
    protected override void SetActiveBuild() { EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL); }

    protected override string GetAALibrary() { return $"{CommonServices.GetProjectPath()}/Library/com.unity.addressables/aa/WebGL"; }
}
}
#endif