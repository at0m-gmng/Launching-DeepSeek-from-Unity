namespace GameResources.Features.FileChecker.Scripts
{
    using System.IO;
    using System.Threading.Tasks;
    using UnityEngine;

    public class BaseFileCheker : IComponentChecker
    {
        public BaseFileCheker(string _targetFolder, string[] _requiredFiles)
        {
            targetFolder = _targetFolder;
            requiredFiles = _requiredFiles;
        }

        public string FoundPath => foundPath;
        
        protected string targetFolder = string.Empty;
        protected string foundPath = string.Empty;
        protected string[] requiredFiles = default;

        /// <summary>
        /// Checks if all files (named fileNames) exist in the specified folder.
        /// </summary>
        public virtual async Task<bool> IsContains()
        {
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(Path.Combine(targetFolder, requiredFiles[i])))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Checks if all files (named requiredFiles) exist in the StreamingAssets directory.
        /// </summary>
        public virtual bool IsContainsInStreamingAssets()
        {
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                string path = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(requiredFiles[i]));
                if (File.Exists(path))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if all files (named requiredFiles) exist in the StreamingAssets directory.
        /// </summary>
        public virtual string GetContainsStreamingAssetsPath()
        {
            foreach (string file in requiredFiles)
            {
                string path = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(file));
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return string.Empty;
        }
    }
}