namespace GameResources.Features.DeepSeekService.Scripts.DeepSeekChecker
{
    using System.IO;

    public class DeepSeekChecker : GameResources.Features.FileChecker.Scripts.BaseFileCheker
    {
        public DeepSeekChecker(string _targetFolder, string[] _requiredFiles) : base(_targetFolder, _requiredFiles) { }

        public override bool IsContains() => IsContainsInStreamingAssets() | base.IsContains();

        /// <summary>
        /// Проверяет, существуют ли все файлы (с именами requiredFiles) в каталоге StreamingAssets.
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