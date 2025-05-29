using System.Linq;
using GPTUnity.Helpers;
using GPTUnity.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ChatEditorWindow
{
    #region Unity Editor Window

    private void CreateGUI()
    {
        Debug.Log("===CreateGUI===");

        rootVisualElement.Clear();

        var root = new VisualElement();
        root.style.flexGrow = 1;
        root.style.backgroundColor = ChatSettings.instance.ColorChatBackground;

        // Model selection
        var topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.marginBottom = 5;
        topBar.style.minHeight = 20;

        // Add a flexible space
        var flexibleSpace = new VisualElement();
        flexibleSpace.style.flexGrow = 1;
        topBar.Add(flexibleSpace);

        var toolbar = new Toolbar();

        // Remove copy, paste, rebuild buttons and replace with 3-dot menu
        toolbar.Add(new ToolbarSpacer { flex = true });

        // "+" (New Chat) button remains as-is, rightmost
        var resetIconButton = new Button(ResetMessagesVisualElements) { text = "" };
        resetIconButton.style.width = resetIconButton.style.height = 18;
        resetIconButton.style.backgroundImage = _iconsHelpers.LoadUnityIcon("d_Toolbar Plus");
        resetIconButton.tooltip = "New Chat";
        toolbar.Add(resetIconButton);

        // 3-dot menu as a label (not a button)
        var menuLabel = new Label("\u22EE"); // Unicode vertical ellipsis
        menuLabel.tooltip = "More Options";
        menuLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        menuLabel.style.fontSize = 14;
        menuLabel.style.marginRight = 9;
        menuLabel.style.marginLeft = 5;
        menuLabel.style.alignSelf = Align.Center;
        
        //menuLabel.style.cursor = MouseCursor.Link;
        menuLabel.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                var menu = new UnityEditor.GenericMenu();
                menu.AddItem(new GUIContent("Copy Chat"), false, OnButtonCopyHistoryClicked);
                menu.AddItem(new GUIContent("Paste Chat"), false, OnButtonPasteHistoryClicked);
                menu.AddItem(new GUIContent("Rebuild UI"), false, CreateGUI);
                menu.DropDown(menuLabel.worldBound);
            }
        });
        toolbar.Add(menuLabel);

        root.Add(toolbar);

        // Messages area
        _messagesScrollView = new ScrollView();
        _messagesScrollView.style.flexGrow = 1;
        _messagesScrollView.style.marginBottom = 5;

        // Show template prompts if there are no messages
        if (_messages == null || _messages.ChatHistory.Count <= 1)
        {
            var promptList = new VisualElement();
            promptList.style.flexDirection = FlexDirection.Column;
            promptList.style.alignItems = Align.FlexStart;
            promptList.style.marginTop = 20;
            promptList.style.marginLeft = 10;

            var promptLabel = new Label("Try one of these prompts:");
            promptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            promptLabel.style.flexWrap = Wrap.Wrap;
            promptLabel.style.marginBottom = 6;
            promptList.Add(promptLabel);

            foreach (var (title, prompt) in Prompts.TemplatesDict)
            {
                var promptButton = new Button(() =>
                {
                    _inputField.value = prompt;
                    _inputField.Focus();
                })
                {
                    text = title
                };
                // Balloon-like styling
                promptButton.style.marginBottom = 8;
                promptButton.style.marginTop = 0;
                promptButton.style.marginLeft = 0;
                promptButton.style.marginRight = 0;
                
                promptButton.style.paddingLeft = 8;
                promptButton.style.paddingRight = 8;
                
                promptButton.style.SetAllBorder(8);
                
                promptButton.style.backgroundColor = Color.white.WithAlpha(0.8f);
                promptButton.style.color = new Color(0.13f, 0.18f, 0.22f, 1.0f);
                promptButton.style.unityFontStyleAndWeight = FontStyle.Normal;
                promptButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                //promptButton.style.width = Length.Percent(80);
                
                // promptButton.style.boxShadow = new StyleBackground(null); // Remove default shadow
                // Add a subtle shadow using Unity's shadow property if available
// #if UNITY_2022_2_OR_NEWER
//                 promptButton.style.boxShadow = new StyleBoxShadow(
//                     new BoxShadow(
//                         new Color(0, 0, 0, 0.08f),
//                         0, 2, 8, 0, BoxShadowType.Outer
//                     )
//                 );
// #endif
                promptList.Add(promptButton);
            }

            _messagesScrollView.Add(promptList);
        }
        // ...existing code for displaying messages...

        root.Add(_messagesScrollView);

        // Spacer element
        var spacer = new VisualElement();
        spacer.style.height = 10;
        root.Add(spacer);

        // Input area
        _bottomBar = new VisualElement();
        _bottomBar.style.flexDirection = FlexDirection.Row;
        _bottomBar.style.minHeight = 20;
        _bottomBar.style.maxHeight = 200;
        _bottomBar.style.width = Length.Percent(100);
        _bottomBar.style.marginBottom = 10;
        _bottomBar.style.marginTop = 10;

        _inputField = new TextField();
        _inputField.multiline = true;
        _inputField.label = string.Empty;
        _inputField.style.flexGrow = 1;
        _inputField.style.whiteSpace = WhiteSpace.Normal; // This enables text wrapping in UI Toolkit
        _inputField.style.flexShrink = 1; // Allows shrinking
        _inputField.style.minWidth = 0; // Prevents overflow
        _inputField.style.marginRight = 0; // Optional: space between input and buttons
        _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        _bottomBar.Add(_inputField);
        
        _modelDropdown = new DropdownField("");
        _modelDropdown.choices = _api.GetModels().ToList();
        _modelDropdown.value = _currentModel;
        _modelDropdown.RegisterValueChangedCallback(OnModelDropDownValueChanged);
        _bottomBar.Add(_modelDropdown);

        // Stack container for buttons
        var buttonStack = new VisualElement();
        buttonStack.style.width = 20;
        buttonStack.style.height = 20;
        buttonStack.style.position = Position.Relative; // or Absolute if you want more control
        buttonStack.style.marginRight = 6; // Optional: space between input and buttons

        _sendButton = new Button(SendCurrentMessageToServerAsync)
        {
            style =
            {
                width = 18,
                height = 18,
                backgroundImage = _iconsHelpers.LoadUnityIcon("d_PlayButton")
            },
            visible = true
        };

        buttonStack.Add(_sendButton);

        _stopButton = new Button(StopProcessing)
        {
            style =
            {
                width = 18,
                height = 18,
                backgroundImage = _iconsHelpers.LoadUnityIcon("d_PreMatQuad")
            },
            visible = false
        };

        buttonStack.Add(_stopButton);
        _bottomBar.Add(buttonStack);

        root.Add(_bottomBar);
        rootVisualElement.Add(root);
        
        _messagesScrollView.scrollOffset = _lastScrollOffset;

        OnGUICreated();
    }

    #endregion
}
