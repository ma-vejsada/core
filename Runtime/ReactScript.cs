using System;
using UnityEngine;

namespace ReactUnity
{
    [Serializable]
    public class ReactScript
    {
        public ScriptSource ScriptSource = ScriptSource.TextAsset;

        public TextAsset SourceAsset;
        public string SourcePath;
        public string SourceText;

        [SerializeField]
        [Tooltip(@"Editor only. Watches file for changes and refreshes the view on change.
Can be enabled outside the editor by adding define symbol REACT_WATCH_OUTSIDE_EDITOR to build.")]
        private bool Watch = false;

        private bool SourceIsTextAsset => ScriptSource == ScriptSource.TextAsset;
        private bool SourceIsPath => ScriptSource != ScriptSource.TextAsset && ScriptSource != ScriptSource.Text;
        private bool SourceIsText => ScriptSource == ScriptSource.Text;
        private bool SourceIsWatchable => ScriptSource != ScriptSource.Url && ScriptSource != ScriptSource.Text;


#if UNITY_EDITOR || REACT_WATCH_OUTSIDE_EDITOR
        IDisposable StartWatching(Action<string> callback)
        {
            string path = "";

            if (ScriptSource == ScriptSource.File)
                path = SourcePath;
#if UNITY_EDITOR
            else if (ScriptSource == ScriptSource.TextAsset)
                path = UnityEditor.AssetDatabase.GetAssetPath(SourceAsset);
            else if (ScriptSource == ScriptSource.Resource)
                path = UnityEditor.AssetDatabase.GetAssetPath(Resources.Load(SourcePath));
#endif

            if (string.IsNullOrWhiteSpace(path)) return null;

            return DetectChanges.WatchFileSystem(path, x => callback(System.IO.File.ReadAllText(path)));
        }
#endif

        public IDisposable GetScript(Action<string> changeCallback, out string result)
        {
            switch (ScriptSource)
            {
                case ScriptSource.TextAsset:
                    if (!SourceAsset) result = null;
#if UNITY_EDITOR
                    else result = System.IO.File.ReadAllText(UnityEditor.AssetDatabase.GetAssetPath(SourceAsset));
#else
                    else result = SourceAsset.text;
#endif
                    break;
                case ScriptSource.File:
                    result = System.IO.File.ReadAllText(SourcePath);
                    break;
                case ScriptSource.Url:
                    result = null;
                    // TODO: Maybe we don't need url 
                    //return new UnityWebRequest(SourcePath);
                    break;
                case ScriptSource.Resource:
                    var asset = Resources.Load(SourcePath) as TextAsset;
                    if (asset) result = asset.text;
                    else result = null;
                    break;
                case ScriptSource.Text:
                    result = SourceText;
                    break;
                default:
                    result = null;
                    break;
            }

#if UNITY_EDITOR || REACT_WATCH_OUTSIDE_EDITOR
            if (Watch && SourceIsWatchable) return StartWatching(changeCallback);
#endif
            return null;
        }
    }

    public enum ScriptSource
    {
        TextAsset = 0,
        File = 1,
        Url = 2,
        Resource = 3,
        Text = 4,
    }


#if UNITY_EDITOR || REACT_WATCH_OUTSIDE_EDITOR
    public class DetectChanges
    {
        public static IDisposable WatchFileSystem(string path, Action<string> callback)
        {
            System.IO.FileSystemWatcher fileSystemWatcher = new System.IO.FileSystemWatcher();

            fileSystemWatcher.Path = System.IO.Path.GetDirectoryName(path);
            fileSystemWatcher.Filter = System.IO.Path.GetFileName(path);
            fileSystemWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.Size;

            fileSystemWatcher.Changed += (x, y) => callback(y.FullPath);
            fileSystemWatcher.EnableRaisingEvents = true;

            return fileSystemWatcher;
        }
    }
#endif
}