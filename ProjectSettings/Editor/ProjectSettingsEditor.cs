using nickeltin.ProjectSettings.Runtime;

namespace nickeltin.ProjectSettings.Editor
{
    /// <summary>
    /// Inherit from this class to make custom editor for <see cref="ProjectSettingsAsset"/>
    /// </summary>
    public abstract class ProjectSettingsEditor : UnityEditor.Editor
    {
        protected void OnEnable()
        {
            //Shinanigans to pass "SerializedObjectNotCreatableException: Object at index 0 is null"
            try { var getterTest = serializedObject; }
            catch { return; }
            
            OnEnable_Internal();
        }

        protected virtual void OnEnable_Internal()
        {
            
        }
    }
}