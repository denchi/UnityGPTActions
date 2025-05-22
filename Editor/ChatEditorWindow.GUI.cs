using GPTUnity.Settings;
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

        _modelDropdown = new DropdownField("");
        _modelDropdown.choices = _models;
        _modelDropdown.value = _currentModel;
        _modelDropdown.RegisterValueChangedCallback(evt => _currentModel = evt.newValue);

        // Add "Copy History" button
        var copyHistoryButton = new Button(OnButtonCopyHistoryClicked) { text = "C" };
        copyHistoryButton.style.width = copyHistoryButton.style.height = 18;
        copyHistoryButton.tooltip = "New Chat";

        // Add "Copy History" button
        var pasteHistoryButton = new Button(OnButtonPasteHistoryClicked) { text = "P" };
        copyHistoryButton.style.width = copyHistoryButton.style.height = 18;
        copyHistoryButton.tooltip = "Paste Chat";

        // Add "Copy History" button
        var rebuildButton = new Button(CreateGUI) { text = "R" };
        rebuildButton.style.width = rebuildButton.style.height = 18;
        rebuildButton.tooltip = "Rebuild UI";

        // Add reset button with "+" icon
        var resetIconButton = new Button(ResetMessagesVisualElements) { text = "" };
        resetIconButton.style.width = resetIconButton.style.height = 18;
        resetIconButton.style.backgroundImage = iconsHelpers.LoadUnityIcon("d_Toolbar Plus");
        resetIconButton.tooltip = "New Chat";

        var toolbar = new Toolbar();

        toolbar.Add(copyHistoryButton);
        toolbar.Add(pasteHistoryButton);
        toolbar.Add(rebuildButton);

        toolbar.Add(new ToolbarSpacer { flex = true });

        //toolbar.Add(_modelDropdown);
        toolbar.Add(resetIconButton);
        root.Add(toolbar);

        // Messages area
        _messagesScrollView = new ScrollView();
        _messagesScrollView.style.flexGrow = 1;
        _messagesScrollView.style.marginBottom = 5;
        root.Add(_messagesScrollView);

        // Spacer element
        var spacer = new VisualElement();
        spacer.style.height = 10;
        root.Add(spacer);

        // Input area
        _bottomBar = new VisualElement();
        _bottomBar.style.flexDirection = FlexDirection.Row;
        _bottomBar.style.minHeight = 20;
        _bottomBar.style.maxHeight = 100;
        _bottomBar.style.width = Length.Percent(100);
        _bottomBar.style.marginBottom = 3;

        _inputField = new TextField();
        _inputField.multiline = true;
        _inputField.style.flexGrow = 1;
        _inputField.style.minWidth = 0;
        _inputField.label = string.Empty;
        _inputField.style.whiteSpace = WhiteSpace.Normal; // This enables text wrapping in UI Toolkit
        _inputField.style.flexShrink = 1; // Allows shrinking
        _inputField.style.minWidth = 0; // Prevents overflow
        _inputField.style.marginRight = 0; // Optional: space between input and buttons

        _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        _bottomBar.Add(_inputField);

        _modelDropdown = new DropdownField("");
        _modelDropdown.choices = _models;
        _modelDropdown.value = _currentModel;
        _modelDropdown.style.marginRight = 0;
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
                backgroundImage = iconsHelpers.LoadUnityIcon("d_PlayButton")
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
                backgroundImage = iconsHelpers.LoadUnityIcon("d_PreMatQuad")
            },
            visible = false
        };

        buttonStack.Add(_stopButton);
        _bottomBar.Add(buttonStack);

        root.Add(_bottomBar);
        rootVisualElement.Add(root);

        OnGUICreated();
    }

    #endregion
}