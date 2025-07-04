#if ADDRESSABLE && BLUEPRINT_WORKFLOW
using UnityEditor;

public class BlueprintWorkFlowIos:BlueprintWorkFlowAndroid
{
    protected override void SetActiveBuild()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
    }

    protected override string GetAALibrary()
    {
        return  $"{CommonServices.GetProjectPath()}/Library/com.unity.addressables/aa/iOS";
    }
}
#endif