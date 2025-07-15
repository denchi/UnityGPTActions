using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DeathByGravity.GPTActions;
using GPTUnity.Indexing;
using GPTUnity.Settings;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GPTUnity.Data
{
    public class ChatSettingsProvider
    {
        private static readonly string PreferencesPath = "Project/Chat Settings";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(PreferencesPath, SettingsScope.Project)
            {
                label = "Chat Settings",
                guiHandler = (searchContext) =>
                {
                    var settings = ChatSettings.instance;
                    EditorGUI.BeginChangeCheck();

                    // Color fields
                    var newColorBackgroundUser =
                        EditorGUILayout.ColorField("User Background Color", settings.ColorBackgroundUser);
                    var newColorBackgroundAssistant = EditorGUILayout.ColorField("Assistant Background Color",
                        settings.ColorBackgroundAssistant);
                    var newColorChatBackground =
                        EditorGUILayout.ColorField("Chat Background Color", settings.ColorChatBackground);
                    
                    // Search API settings
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Search API Settings", EditorStyles.boldLabel);
                    settings.SearchApiHost = EditorGUILayout.TextField("Search API Host", settings.SearchApiHost);
                    settings.SearchApiPythonPath = EditorGUILayout.TextField("Search API Python Path", settings.SearchApiPythonPath);
                    
                    if (GUILayout.Button("Test Search API Connection"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        CheckSearchApiAvailableAsync(settings);
                    }
                    
                    if (GUILayout.Button("Create Python Environment"))
                    {
                        ExtractorRunner.TryCreatePythonEnvironment(settings.SearchApiPythonPath);
                    }
                    
                    if (GUILayout.Button("Create a new index"))
                    {
                        ExtractorRunner.RunExtractor(settings);
                        ExtractorRunner.RunIndexer(settings);
                    }
                    
                    if (GUILayout.Button("Start Server"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        StartServerAsync(settings);
                    }
                    
                    if (GUILayout.Button("Stop Server"))
                    {
                        // Run the check asynchronously to avoid blocking the main thread
                        StopServerAsync(settings);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ColorBackgroundUser = newColorBackgroundUser;
                        settings.ColorBackgroundAssistant = newColorBackgroundAssistant;
                        settings.ColorChatBackground = newColorChatBackground;
                    }
                }
            };
        }

        private static async void CheckSearchApiAvailableAsync(ChatSettings settings)
        {
            bool isAvailable = false;
            
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking Search API availability: {e.Message}");
                    isAvailable = false;
                }
            }
            else
            {
                // If the window or client is not available, create a new client
                var client = new DeepSearchClient(settings.SearchApiHost, settings.SearchApiPythonPath);
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch
                {
                    isAvailable = false;
                }
            }
            
            EditorUtility.DisplayDialog("Search API Test", isAvailable ? "Connection successful!" : "Connection failed!", "OK");
        }
        
        private static async void StartServerAsync(ChatSettings settings)
        {
            bool isAvailable = false;
            
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    isAvailable = await client.IsServerAvailable();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking Search API availability: {e.Message}");
                    isAvailable = false;
                }

                if (!isAvailable)
                {
                    await client.StartSearchServerAsync();
                    Debug.Log("[ChatSettingsProvider] Search server started successfully.");
                    return;
                }
                
                Debug.Log("[ChatSettingsProvider] Search server is already running.");
            }
        }
        
        private static async void StopServerAsync(ChatSettings settings)
        {
            var window = EditorWindow.GetWindow<ChatEditorWindow>();
            if (window != null && window.SearchApiClient != null)
            {
                var client = window.SearchApiClient;
                try
                {
                    await client.StopSearchServer();
                    Debug.Log("[ChatSettingsProvider] Search server stopped successfully.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error stopping Search API server: {e.Message}");
                }
            }
        }
    }
}