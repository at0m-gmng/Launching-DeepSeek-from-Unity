﻿namespace GameResources.Features.DeepSeekService.Scripts.DeepSeekChecker
{
    using System.IO;
    using System.Threading.Tasks;

    public class DeepSeekChecker : GameResources.Features.FileChecker.Scripts.BaseFileCheker
    {
        public DeepSeekChecker(string _targetFolder, string[] _requiredFiles) : base(_targetFolder, _requiredFiles) { }

        public override async Task<bool> IsContains() => IsContainsInStreamingAssets() | await base.IsContains();

        /// <summary>
        /// Checks if all files (named requiredFiles) exist in the StreamingAssets directory.
        /// </summary>
        public override bool IsContainsInStreamingAssets()
        {
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                string path = Path.Combine(targetFolder, Path.GetFileName(requiredFiles[i]));
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            return true;
        }
    }
}