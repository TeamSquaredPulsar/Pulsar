using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pulsar.DataClasses
{
[Serializable]
public class SceneReference : AssetReference
{
    [SerializeField]
    private string sceneName;

    public string SceneName => sceneName;

#if UNITY_EDITOR
    public SceneReference(SceneAsset scene)
        : base(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scene)))
    {
        sceneName = scene.name;
    }

    public override bool ValidateAsset(string path) =>
        ValidateAsset(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));

    public override bool ValidateAsset(UnityEngine.Object obj) =>
        (obj != null) && (obj is SceneAsset);

    public override bool SetEditorAsset(UnityEngine.Object value)
    {
        if (!base.SetEditorAsset(value))
        {
            return false;
        }

        if (value is SceneAsset scene)
        {
            sceneName = scene.name;
            return true;
        }
        else
        {
            sceneName = string.Empty;
            return false;
        }
    }

#endif
}
}
