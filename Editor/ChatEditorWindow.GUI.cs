using System.Linq;
using GPTUnity.Helpers;
using GPTUnity.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ChatEditorWindow
{
    #region Unity Editor Window

    private void CreateGUI()
    {
        if (!CheckApiKeysProvided()) 
            return;

        InitDerivedFields();

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
        _bottomBar.style.flexDirection = FlexDirection.Column;
        _bottomBar.style.minHeight = _bottomBar.style.maxHeight = 84 + 2 * 2 + 2 * 2 + 15;
        _bottomBar.style.width = Length.Percent(100);
        _bottomBar.style.marginBottom = 10;
        _bottomBar.style.marginTop = 10;
        _bottomBar.style.SetAllPadding(2);

        // Add blue outline
        _bottomBar.style.borderTopWidth =
            _bottomBar.style.borderBottomWidth =
                _bottomBar.style.borderLeftWidth =
                    _bottomBar.style.borderRightWidth = 2;
        _bottomBar.style.borderTopColor =
            _bottomBar.style.borderBottomColor =
                _bottomBar.style.borderLeftColor =
                    _bottomBar.style.borderRightColor = new Color(0.251f, 0.553f, 1.0f, 1.0f); // #408DFF in RGBA
        
        _inputField = new TextField();
        _inputField.multiline = true;
        _inputField.label = string.Empty;
        _inputField.style.flexGrow = 1;
        _inputField.style.whiteSpace = WhiteSpace.Normal; // This enables text wrapping in UI Toolkit
        _inputField.style.flexShrink = 1; // Allows shrinking
        _inputField.style.minWidth = 0; // Prevents overflow
        _inputField.style.marginRight = 0; // Optional: space between input and buttons

        // Make the input field more like a textarea
        _inputField.style.minHeight = 60;
        _inputField.style.maxHeight = 120;
        _inputField.style.height = 80;
        _inputField.style.unityTextAlign = TextAnchor.UpperLeft;
        _inputField.style.overflow = Overflow.Visible;
        //_inputField.style.resize = new StyleEnum<Resize>(Resize.Vertical);

        _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);

        var line = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
            }
        };
        
        _modelDropdown = new DropdownField("");
        _modelDropdown.style.height = 20;
        _modelDropdown.choices = _api.Models.ToList();
        _modelDropdown.value = _currentModel;
        _modelDropdown.RegisterValueChangedCallback(OnModelDropDownValueChanged);
        line.Add(_modelDropdown);
        line.Add(new VisualElement{ style = { flexGrow = 1}});

        // Stack container for buttons
        var buttonStack = new VisualElement();
        buttonStack.style.width = 20;
        buttonStack.style.height = 20;
        buttonStack.style.marginRight = 6; // Optional: space between input and buttons
        buttonStack.style.paddingTop = 2; // Optional: space between input and buttons
        

        _sendButton = new Button(SendCurrentMessageToServerAsync)
        {
            style =
            {
                width = 18,
                height = 18,
                backgroundImage = _iconsHelpers.LoadUnityIcon("d_PlayButton"),
                position = Position.Absolute,
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
                backgroundImage = _iconsHelpers.LoadUnityIcon("d_PreMatQuad"),
                position = Position.Absolute,
                // Add red tint to the icon
                unityBackgroundImageTintColor = Color.red
            },
            visible = false
        };

        buttonStack.Add(_stopButton);
        line.Add(buttonStack);
        
        _bottomBar.Add(line);
        _bottomBar.Add(_inputField);

        root.Add(_bottomBar);
        rootVisualElement.Add(root);
        
        _messagesScrollView.scrollOffset = _lastScrollOffset;

        OnGUICreated();
    }

    private void ShowNoApiKeysUI()
    {
        rootVisualElement.Clear();

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.justifyContent = Justify.Center;
        container.style.alignItems = Align.Center;
        container.style.flexGrow = 1;
        container.style.height = Length.Percent(100);
        container.style.backgroundColor = new Color(0.97f, 0.98f, 1.0f, 1.0f);

        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Column;
        card.style.alignItems = Align.Center;
        card.style.backgroundColor = Color.white;
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 10;
        card.style.paddingTop = 24;
        card.style.paddingBottom = 24;
        card.style.paddingLeft = 32;
        card.style.paddingRight = 32;
        card.style.marginTop = 40;
        card.style.marginBottom = 40;
        card.style.unityFontStyleAndWeight = FontStyle.Bold;

        // Icon
        var icon = new Image();
        icon.image = _iconsHelpers.LoadUnityIcon("d_console.erroricon"); // Unity's error icon
        icon.style.width = 40;
        icon.style.height = 40;
        icon.style.marginBottom = 10;
        card.Add(icon);

        // Main message
        var label = new Label("No API Key Found");
        label.style.fontSize = 18;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 6;
        card.Add(label);

        // Subtext
        var subLabel = new Label("Please add your OpenAI API key in the .env to use the chat.");
        subLabel.style.fontSize = 13;
        subLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        subLabel.style.marginBottom = 16;
        subLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        card.Add(subLabel);

        // Button row
        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.justifyContent = Justify.Center;
        buttonRow.style.alignItems = Align.Center;

        var openSettingsButton = new Button(() =>
        {
            // show .env file in STreamingAssets folder
            var envPath = System.IO.Path.Combine(Application.streamingAssetsPath, ".env");
            if (System.IO.File.Exists(envPath))
            {
                UnityEditor.EditorUtility.RevealInFinder(envPath);
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("File Not Found", "The .env file does not exist in the StreamingAssets folder.", "OK");
            }
        })
        {
            text = "Open .env File"
        };
        openSettingsButton.style.marginRight = 8;
        openSettingsButton.style.paddingLeft = 14;
        openSettingsButton.style.paddingRight = 14;
        openSettingsButton.style.height = 26;

        var retryButton = new Button(CreateGUI)
        {
            text = "Try Again"
        };
        retryButton.style.paddingLeft = 14;
        retryButton.style.paddingRight = 14;
        retryButton.style.height = 26;

        buttonRow.Add(openSettingsButton);
        buttonRow.Add(retryButton);

        card.Add(buttonRow);
        container.Add(card);

        rootVisualElement.Add(container);
    }

    #endregion
}