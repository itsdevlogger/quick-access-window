using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DL.QuickAccess
{
    [FilePath("UserSettings/QuickAccessData.asset", FilePathAttribute.Location.ProjectFolder)]
    public class QuickAccessData : ScriptableSingleton<QuickAccessData>
    {
        [System.Serializable]
        public class SceneObjectEntry
        {
            public SceneAsset scene;
            public string globalObjectId;
            public string displayName;
        }

        public List<Object> assets = new List<Object>();
        public List<Object> scenes = new List<Object>();
        public List<SceneObjectEntry> sceneObjects = new List<SceneObjectEntry>();

        public void Save()
        {
            Save(true);
        }
    }
}
