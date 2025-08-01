#if ADDRESSABLE && BLUEPRINT_WORKFLOW

namespace BuildHelper.Workflows
{
    using UnityEditor;

    public class BlueprintWorkFlowIos : BlueprintWorkFlowAndroid
    {
        protected override void SetActiveBuild() { EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS); }

        protected override string GetAALibrary() { return $"{CommonServicesHelper.GetProjectPath()}/Library/com.unity.addressables/aa/iOS"; }
    }
}
#endif