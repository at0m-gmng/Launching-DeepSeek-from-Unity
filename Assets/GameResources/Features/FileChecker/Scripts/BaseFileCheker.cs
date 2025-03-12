namespace GameResources.Features.FileChecker.Scripts
{
    using System.IO;
    using UnityEngine;

    public class BaseFileCheker : IComponentChecker
    {
        public BaseFileCheker(string _targetFolder, string[] _requiredFiles)
        {
            targetFolder = _targetFolder;
            requiredFiles = _requiredFiles;
        }
        
        protected string targetFolder;
        protected string[] requiredFiles;

        /// <summary>
        /// Проверяет, существуют ли все файлы (с именами fileNames) в указанной папке.
        /// </summary>
        public virtual bool IsContains()
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
        /// Проверяет, существуют ли все файлы (с именами requiredFiles) в каталоге StreamingAssets.
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
        /// Проверяет, существуют ли все файлы (с именами requiredFiles) в каталоге StreamingAssets.
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