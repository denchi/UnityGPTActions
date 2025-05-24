using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    public abstract class CreateFileActionBase : GPTActionBase, IGPTActionWithFiles, IActionThatRequiresReload
    {
        [GPTParameter("Output file name without extension")]
        public string FileName { get; set; }

        [GPTParameter("Output file extension. Ex: .cs")]
        public string FileExtension { get; set; }

        [GPTParameter("Best location matching this file. Ex: Assets/")]
        public string PathToDirectory { get; set; } = "NewFile";

        public override string Description =>
            $"File <color=yellow>{FileName}{FileExtension}</color> created at <color=yellow>{PathToDirectory}</color>";

        public virtual string Content { get; protected set; }

        public void CreateFile(string overridePath = null)
        {
            var path = overridePath ?? GetOutputPath();
            CreateFile(Content, path);
        }

        public override async Task<string> Execute()
        {
            CreateFile();
            
            return $"File {FileName}{FileExtension} created at {PathToDirectory}";
        }

        //

        private string TryFixFolderStructure(string filePath)
        {
#if UNITY_EDITOR
            var folderPath = System.IO.Path.GetDirectoryName(filePath);
            if (UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                return filePath;

            // Remove Assets/ from the beginning of the folder path
            if (folderPath.StartsWith("Assets/"))
                folderPath = folderPath.Substring("Assets/".Length);

            UnityEditor.AssetDatabase.CreateFolder("Assets", folderPath);
#endif
            return filePath;
        }

        private void CreateFile(string content, string path)
        {
            path = TryFixFolderStructure(path);
            System.IO.File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            Debug.Log("Created new file at " + path);
        }

        protected string GetOutputPath()
        {
            var finalPathToDirectory = PathToDirectory;
            if (!finalPathToDirectory.EndsWith("/"))
                finalPathToDirectory += "/";
            var finalExtension = FileExtension;
            if (!string.IsNullOrWhiteSpace(finalExtension) && !finalExtension.StartsWith("."))
                finalExtension = "." + finalExtension;
            return finalPathToDirectory + FileName + finalExtension;
        }
    }
}