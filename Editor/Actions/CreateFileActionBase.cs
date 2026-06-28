using System.Threading.Tasks;
using GPTUnity.Actions.Interfaces;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    public abstract class CreateFileActionBase : GPTAssistantAction, IGPTActionWithFiles, IGPTActionThatRequiresReload
    {
        [GPTParameter("Name of the file to create, without extension.", true, Name = "file_name")]
        public string FileName { get; set; }

        [GPTParameter("Optional file extension override, including or excluding the leading dot. Example: .cs", Name = "file_extension")]
        public string FileExtension { get; set; }

        [GPTParameter("Project-relative output folder. Defaults to the tool-specific folder when omitted.", Name = "output_directory")]
        public string PathToDirectory { get; set; }

        public virtual string Content { get; protected set; }
        protected virtual string DefaultFileExtension => string.Empty;
        protected virtual string DefaultDirectory => "Assets/";

        public void CreateFile(string overridePath = null)
        {
            var path = overridePath ?? GetOutputPath();
            CreateFile(Content, path);
        }

        public override async Task<string> Execute()
        {
            if (string.IsNullOrWhiteSpace(FileName))
                throw new System.Exception("FileName is required.");

            var outputPath = GetOutputPath();
            CreateFile(outputPath);
            
            return $"Created file at {outputPath}.";
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
            var finalPathToDirectory = string.IsNullOrWhiteSpace(PathToDirectory)
                ? DefaultDirectory
                : PathToDirectory;
            if (!finalPathToDirectory.EndsWith("/"))
                finalPathToDirectory += "/";
            var finalExtension = string.IsNullOrWhiteSpace(FileExtension)
                ? DefaultFileExtension
                : FileExtension;
            if (!string.IsNullOrWhiteSpace(finalExtension) && !finalExtension.StartsWith("."))
                finalExtension = "." + finalExtension;
            return finalPathToDirectory + FileName + finalExtension;
        }
    }
}
