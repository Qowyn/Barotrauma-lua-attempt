﻿using EventInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Barotrauma
{

    public delegate void TextBoxEvent(GUITextBox sender, Keys key);

    public class GUITextBox : GUIComponent, IKeyboardSubscriber
    {        
        public event TextBoxEvent OnSelected;
        public event TextBoxEvent OnDeselected;
        
        bool caretVisible;
        float caretTimer;

        private readonly GUIFrame frame;
        private readonly GUITextBlock textBlock;
        private readonly GUIImage icon;

        public Func<string, string> textFilterFunction;

        public delegate bool OnEnterHandler(GUITextBox textBox, string text);
        public OnEnterHandler OnEnterPressed;
        
        public event TextBoxEvent OnKeyHit;

        public delegate bool OnTextChangedHandler(GUITextBox textBox, string text);
        /// <summary>
        /// Don't set the Text property on delegates that register to this event, because modifying the Text will launch this event -> stack overflow. 
        /// If the event launches, the text should already be up to date!
        /// </summary>
        public event OnTextChangedHandler OnTextChanged;
        public OnTextChangedHandler OnTextChangedDelegate
		{
			set
			{
                OnTextChanged += (GUITextBox a, string b) =>
                {
                    return value.Invoke(a, b);
                };
            }
        }

        public bool CaretEnabled { get; set; }
        public Color? CaretColor { get; set; }
        public bool DeselectAfterMessage = true;

        private int? maxTextLength;

        private int _caretIndex;
        public int CaretIndex
        {
            get { return _caretIndex; }
            set
            {
                if (value >= 0)
                {
                    _caretIndex = value;
                    caretPosDirty = true;
                }
            }
        }
        private bool caretPosDirty;
        protected Vector2 caretPos;
        public Vector2 CaretScreenPos => Rect.Location.ToVector2() + caretPos;

        private bool isSelecting;
        private string selectedText = string.Empty;
        private int selectedCharacters;
        private int selectionStartIndex;
        private int selectionEndIndex;
        private bool IsLeftToRight => selectionStartIndex <= selectionEndIndex;
        private Vector2 selectionStartPos;
        private Vector2 selectionEndPos;
        private Vector2 selectionRectSize;

        private bool mouseHeldInside;

        private readonly Memento<string> memento = new Memento<string>();
        
        // Skip one update cycle, fixes Enter key instantly deselecting the chatbox
        private bool skipUpdate;

        public GUIFrame Frame
        {
            get { return frame; }
        }

        public GUITextBlock.TextGetterHandler TextGetter
        {
            get { return textBlock.TextGetter; }
            set { textBlock.TextGetter = value; }
        }

        private new bool selected;
        public new bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                if (!selected && value)
                {
                    Select();
                }
                else if (selected && !value)
                {
                    Deselect();
                }
            }
        }

        public bool Wrap
        {
            get { return textBlock.Wrap; }
            set
            {
                textBlock.Wrap = value;
            }
        }

        public GUITextBlock TextBlock
        {
            get { return textBlock; }
        }

        //should the text be limited to the size of the box
        //ignored when MaxTextLength is set or text wrapping is enabled
        public bool ClampText
        {
            get;
            set;
        }

        public int? MaxTextLength
        {
            get { return maxTextLength; }
            set
            {
                textBlock.OverflowClip = value != null;                
                maxTextLength = value;
                if (Text.Length > MaxTextLength)
                {
                    SetText(textBlock.Text.Substring(0, (int)maxTextLength));
                }
            }
        }

        public bool OverflowClip
        {
            get { return textBlock.OverflowClip; }
            set { textBlock.OverflowClip = value; }
        }

        public override bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = frame.Enabled = textBlock.Enabled = value;
                if (icon != null) { icon.Enabled = value; }
                if (!enabled && Selected)
                {
                    Deselect();
                }
            }
        }

        public bool Censor
        {
            get { return textBlock.Censor; }
            set { textBlock.Censor = value; }
        }

        public override string ToolTip
        {
            get
            {
                return base.ToolTip;
            }
            set
            {
                base.ToolTip = value;
                textBlock.ToolTip = value;
            }
        }

        public override ScalableFont Font
        {
            get { return textBlock?.Font ?? base.Font; }
            set
            {
                base.Font = value;
                if (textBlock == null) { return; }
                textBlock.Font = value;
            }
        }

        public override Color Color
        {
            get { return color; }
            set
            {
                color = value;
                textBlock.Color = color;
            }
        }

        public Color TextColor
        {
            get { return textBlock.TextColor; }
            set { textBlock.TextColor = value; }
        }

        public override Color HoverColor
        {
            get
            {
                return base.HoverColor;
            }
            set
            {
                base.HoverColor = value;
                textBlock.HoverColor = value;
            }
        }

        public Vector4 Padding
        {
            get { return textBlock.Padding; }
            set { textBlock.Padding = value; }
        }

        // TODO: should this be defined in the stylesheet?
        public Color SelectionColor { get; set; } = Color.White * 0.25f;

        public string Text
        {
            get
            {
                return textBlock.Text;
            }
            set
            {
                SetText(value, store: false);
                CaretIndex = Text.Length;
                OnTextChanged?.Invoke(this, Text);
            }
        }

        public string WrappedText
        {
            get { return textBlock.WrappedText; }
        }

        public bool Readonly { get; set; }

        public GUITextBox(RectTransform rectT, string text = "", Color? textColor = null, ScalableFont font = null,
                          Alignment textAlignment = Alignment.Left, bool wrap = false, string style = "", Color? color = null, bool createClearButton = false, bool createPenIcon = true)
            : base(style, rectT)
        {
            HoverCursor = CursorState.IBeam;
            CanBeFocused = true;

            this.color = color ?? Color.White;
            frame = new GUIFrame(new RectTransform(Vector2.One, rectT, Anchor.Center), style, color);
            GUI.Style.Apply(frame, style == "" ? "GUITextBox" : style);
            textBlock = new GUITextBlock(new RectTransform(Vector2.One, frame.RectTransform, Anchor.CenterLeft), text, textColor, font, textAlignment, wrap, playerInput: true);
            GUI.Style.Apply(textBlock, "", this);
            CaretEnabled = true;
            caretPosDirty = true;

            new GUICustomComponent(new RectTransform(Vector2.One, frame.RectTransform), onDraw: DrawCaretAndSelection);

            int clearButtonWidth = 0;
            if (createClearButton)
            {
                var clearButton = new GUIButton(new RectTransform(new Vector2(0.6f, 0.6f), frame.RectTransform, Anchor.CenterRight, scaleBasis: ScaleBasis.BothHeight) { AbsoluteOffset = new Point(5, 0) }, style: "GUICancelButton")
                {
                    OnClicked = (bt, userdata) =>
                    {
                        Text = "";
                        frame.Flash(Color.White);
                        return true;
                    }
                };
                textBlock.RectTransform.MaxSize = new Point(frame.Rect.Width - clearButton.Rect.Height - clearButton.RectTransform.AbsoluteOffset.X * 2, int.MaxValue);
                clearButtonWidth = (int)(clearButton.Rect.Width * 1.2f);
            }

            if (this.style != null && this.style.ChildStyles.ContainsKey("textboxicon") && createPenIcon)
            {
                icon = new GUIImage(new RectTransform(new Vector2(0.6f, 0.6f), frame.RectTransform, Anchor.CenterRight, scaleBasis: ScaleBasis.BothHeight) { AbsoluteOffset = new Point(5 + clearButtonWidth, 0) }, null, scaleToFit: true);
                icon.ApplyStyle(this.style.ChildStyles["textboxicon"]);
                textBlock.RectTransform.MaxSize = new Point(frame.Rect.Width - icon.Rect.Height - clearButtonWidth - icon.RectTransform.AbsoluteOffset.X * 2, int.MaxValue);
            }
            Font = textBlock.Font;
            
            Enabled = true;

            rectT.SizeChanged += () => 
            {
                if (icon != null) { textBlock.RectTransform.MaxSize = new Point(frame.Rect.Width - icon.Rect.Height - icon.RectTransform.AbsoluteOffset.X * 2, int.MaxValue); }
                caretPosDirty = true; 
            };
            rectT.ScaleChanged += () =>
            {
                if (icon != null) { textBlock.RectTransform.MaxSize = new Point(frame.Rect.Width - icon.Rect.Height - icon.RectTransform.AbsoluteOffset.X * 2, int.MaxValue); }
                caretPosDirty = true; 
            };
        }

        private bool SetText(string text, bool store = true)
        {
            if (textFilterFunction != null)
            {
                text = textFilterFunction(text);
            }
            if (textBlock.Text == text) { return false; }
            textBlock.Text = text;
            if (textBlock.Text == null) textBlock.Text = "";
            if (textBlock.Text != "" && !Wrap)
            {
                if (maxTextLength != null)
                {
                    if (textBlock.Text.Length > maxTextLength)
                    {
                        textBlock.Text = textBlock.Text.Substring(0, (int)maxTextLength);
                    }
                }
                else
                {
                    while (ClampText && textBlock.Text.Length > 0 && Font.MeasureString(textBlock.Text).X * TextBlock.TextScale > (int)(textBlock.Rect.Width - textBlock.Padding.X - textBlock.Padding.Z))
                    {
                        textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Length - 1);
                    }
                }
            }
            if (store)
            {
                memento.Store(textBlock.Text);
            }
            return true;
        }

        private void CalculateCaretPos()
        {
            if (Censor || !Wrap)
            {
                string textDrawn = textBlock.CensoredText;
                CaretIndex = Math.Min(CaretIndex, textDrawn.Length);
                textDrawn = Censor ? textBlock.CensoredText : textBlock.Text;
                Vector2 textSize = Font.MeasureString(textDrawn[..CaretIndex]) * TextBlock.TextScale;
                caretPos = new Vector2(textSize.X, 0) + textBlock.TextPos - textBlock.Origin * TextBlock.TextScale;
            }
            else
            {
                CaretIndex = Math.Min(CaretIndex, textBlock.Text.Length);
                textBlock.Font.WrapText(
                    textBlock.Text,
                    (textBlock.Rect.Width - textBlock.Padding.X - textBlock.Padding.Z) / TextBlock.TextScale,
                    CaretIndex,
                    out Vector2 requestedCharPos);
                caretPos = requestedCharPos * TextBlock.TextScale + textBlock.TextPos - textBlock.Origin * TextBlock.TextScale;
            }
            caretPosDirty = false;
        }

        public void Select(int forcedCaretIndex = -1)
        {
            skipUpdate = true;
            if (memento.Current == null)
            {
                memento.Store(Text);
            }
            CaretIndex = forcedCaretIndex == - 1 ? textBlock.GetCaretIndexFromScreenPos(PlayerInput.MousePosition) : forcedCaretIndex;
            CalculateCaretPos();
            ClearSelection();
            selected = true;
            GUI.KeyboardDispatcher.Subscriber = this;
            OnSelected?.Invoke(this, Keys.None);
        }

        public void Deselect()
        {
            memento.Clear();
            selected = false;

            if (GUI.KeyboardDispatcher.Subscriber == this)
            {
                GUI.KeyboardDispatcher.Subscriber = null;
            }
            OnDeselected?.Invoke(this, Keys.None);
        }

        public override void Flash(Color? color = null, float flashDuration = 1.5f, bool useRectangleFlash = false, bool useCircularFlash = false, Vector2? flashRectOffset = null)
        {
            frame.Flash(color, flashDuration, useRectangleFlash, useCircularFlash, flashRectOffset);
        }
        
        protected override void Update(float deltaTime)
        {
            if (!Visible) return;

            if (flashTimer > 0.0f) flashTimer -= deltaTime;
            if (!Enabled) { return; }

            if (skipUpdate)
            {
                skipUpdate = false;
                return;
            }

            if (MouseRect.Contains(PlayerInput.MousePosition) && (GUI.MouseOn == null || (!(GUI.MouseOn is GUIButton) && GUI.IsMouseOn(this))))
            {
                State = ComponentState.Hover;
                if (PlayerInput.PrimaryMouseButtonDown())
                {
                    mouseHeldInside = true;
                    Select();
                }
                else
                {
                    isSelecting = PlayerInput.PrimaryMouseButtonHeld();
                }
                if (PlayerInput.DoubleClicked())
                {
                    SelectAll();
                }
                if (isSelecting)
                {
                    if (!MathUtils.NearlyEqual(PlayerInput.MouseSpeed.X, 0))
                    {
                        CaretIndex = textBlock.GetCaretIndexFromScreenPos(PlayerInput.MousePosition);
                        CalculateCaretPos();
                        CalculateSelection();
                    }
                }
            }
            else
            {
                if ((PlayerInput.LeftButtonClicked() || PlayerInput.RightButtonClicked()) && selected) 
                {
                    if (!mouseHeldInside) { Deselect(); }
                    mouseHeldInside = false;
                }
                isSelecting = false;
                State = ComponentState.None;
            }
            if (!isSelecting)
            {
                isSelecting = PlayerInput.IsShiftDown();
            }

            if (mouseHeldInside && !PlayerInput.PrimaryMouseButtonHeld())
            {
                mouseHeldInside = false;
            }

            if (CaretEnabled)
            {
                if (textBlock.OverflowClipActive)
                {
                    if (CaretScreenPos.X < textBlock.Rect.X + textBlock.Padding.X)
                    {
                        textBlock.TextPos = new Vector2(textBlock.TextPos.X + ((textBlock.Rect.X + textBlock.Padding.X) - CaretScreenPos.X), textBlock.TextPos.Y);
                        CalculateCaretPos();
                    }
                    else if (CaretScreenPos.X > textBlock.Rect.Right - textBlock.Padding.Z)
                    {
                        textBlock.TextPos = new Vector2(textBlock.TextPos.X - (CaretScreenPos.X - (textBlock.Rect.Right - textBlock.Padding.Z)), textBlock.TextPos.Y);
                        CalculateCaretPos();
                    }
                }
                caretTimer += deltaTime;
                caretVisible = ((caretTimer * 1000.0f) % 1000) < 500;
                if (caretVisible && caretPosDirty)
                {
                    CalculateCaretPos();
                }
            }

            if (GUI.KeyboardDispatcher.Subscriber == this)
            {
                State = ComponentState.Selected;
                Character.DisableControls = true;
                if (OnEnterPressed != null &&  PlayerInput.KeyHit(Keys.Enter))
                {
                    OnEnterPressed(this, Text);
                }
            }
            else if (Selected)
            {
                Deselect();
            }

            textBlock.State = State;
        }

        private void DrawCaretAndSelection(SpriteBatch spriteBatch, GUICustomComponent customComponent)
        {
            if (!Visible) { return; }
            if (Selected)
            {
                if (caretVisible )
                {
                    GUI.DrawLine(spriteBatch,
                        new Vector2(Rect.X + (int)caretPos.X + 2, Rect.Y + caretPos.Y + 3),
                        new Vector2(Rect.X + (int)caretPos.X + 2, Rect.Y + caretPos.Y + Font.MeasureString("I").Y * textBlock.TextScale - 3),
                        CaretColor ?? textBlock.TextColor * (textBlock.TextColor.A / 255.0f));
                }
                if (selectedCharacters > 0)
                {
                    DrawSelectionRect(spriteBatch);
                }
                //GUI.DrawString(spriteBatch, new Vector2(100, 0), selectedCharacters.ToString(), Color.LightBlue, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 20), selectionStartIndex.ToString(), Color.White, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(140, 20), selectionEndIndex.ToString(), Color.White, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 40), selectedText.ToString(), Color.Yellow, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 60), $"caret index: {CaretIndex.ToString()}", GUI.Style.Red, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 80), $"caret pos: {caretPos.ToString()}", GUI.Style.Red, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 100), $"caret screen pos: {CaretScreenPos.ToString()}", GUI.Style.Red, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 120), $"text start pos: {(textBlock.TextPos - textBlock.Origin).ToString()}", Color.White, Color.Black);
                //GUI.DrawString(spriteBatch, new Vector2(100, 140), $"cursor pos: {PlayerInput.MousePosition.ToString()}", Color.White, Color.Black);
            }
        }

        private void DrawSelectionRect(SpriteBatch spriteBatch)
        {
            if (textBlock.WrappedText.Contains("\n"))
            {
                // Multiline selection
                var characterPositions = textBlock.GetAllCaretPositions();
                (int startIndex, int endIndex) = selectionStartIndex < selectionEndIndex
                    ? (selectionStartIndex, selectionEndIndex)
                    : (selectionEndIndex, selectionStartIndex);
                endIndex--;

                void drawRect(Vector2 topLeft, Vector2 bottomRight)
                {
                    int minWidth = GUI.IntScale(5);
                    if (bottomRight.X - topLeft.X < minWidth) { bottomRight.X = topLeft.X + minWidth; }
                    GUI.DrawRectangle(spriteBatch,
                        Rect.Location.ToVector2() + topLeft,
                        bottomRight - topLeft,
                        SelectionColor, isFilled: true);
                }
                
                Vector2 topLeft = characterPositions[startIndex];
                for (int i = startIndex+1; i <= endIndex; i++)
                {
                    Vector2 currPos = characterPositions[i];
                    if (!MathUtils.NearlyEqual(topLeft.Y, currPos.Y))
                    {
                        Vector2 bottomRight = characterPositions[i - 1];
                        bottomRight += Font.MeasureChar(Text[i - 1]);
                        drawRect(topLeft, bottomRight);
                        topLeft = currPos;
                    }
                }
                Vector2 finalBottomRight = characterPositions[endIndex];
                finalBottomRight += Font.MeasureChar(Text[endIndex]);
                drawRect(topLeft, finalBottomRight);
            }
            else
            {
                // Single line selection
                Vector2 topLeft = IsLeftToRight ? selectionStartPos : selectionEndPos;
                GUI.DrawRectangle(spriteBatch, Rect.Location.ToVector2() + topLeft, selectionRectSize, SelectionColor, isFilled: true);
            }
        }

        public void ReceiveTextInput(char inputChar)
        {
            ReceiveTextInput(inputChar.ToString());
        }

        public void ReceiveTextInput(string input)
        {
            if (Readonly) { return; }
            if (selectedCharacters > 0)
            {
                RemoveSelectedText();
            }
            Vector2 textPos = textBlock.TextPos;
            bool wasOverflowClipActive = textBlock.OverflowClipActive;
            if (SetText(Text.Insert(CaretIndex, input)))
            {
                CaretIndex = Math.Min(Text.Length, CaretIndex + input.Length);
                OnTextChanged?.Invoke(this, Text);
                if (textBlock.OverflowClipActive && wasOverflowClipActive && !MathUtils.NearlyEqual(textBlock.TextPos, textPos))
                {
                    textBlock.TextPos = textPos + Vector2.UnitX * Font.MeasureString(input).X * TextBlock.TextScale;
                }
            }
        }

        public void ReceiveCommandInput(char command)
        {
            if (Text == null) Text = "";

            // Prevent alt gr from triggering any of these as that combination is often needed for special characters
            if (PlayerInput.IsAltDown()) return;

            switch (command)
            {
                case '\b' when !Readonly: //backspace
                    if (PlayerInput.KeyDown(Keys.LeftControl) || PlayerInput.KeyDown(Keys.RightControl))
                    {
                        SetText(string.Empty, false);
                        CaretIndex = Text.Length;
                    }
                    else if (selectedCharacters > 0)
                    {
                        RemoveSelectedText();
                    }
                    else if (Text.Length > 0 && CaretIndex > 0)
                    {
                        CaretIndex--;
                        SetText(Text.Remove(CaretIndex, 1));
                        CalculateCaretPos();
                        ClearSelection();
                    }
                    OnTextChanged?.Invoke(this, Text);
                    break;
                case (char)0x3: // ctrl-c
                    CopySelectedText();
                    break;
                case (char)0x16 when !Readonly: // ctrl-v
                    string text = GetCopiedText();
                    RemoveSelectedText();
                    if (SetText(Text.Insert(CaretIndex, text)))
                    {
                        CaretIndex = Math.Min(Text.Length, CaretIndex + text.Length);
                        OnTextChanged?.Invoke(this, Text);
                    }
                    break;
                case (char)0x18: // ctrl-x
                    CopySelectedText();
                    if (!Readonly)
                    {
                        RemoveSelectedText();
                    }
                    break;
                case (char)0x1: // ctrl-a
                    if (PlayerInput.IsCtrlDown())
                    {
                        SelectAll();
                    }
                    break;
                case (char)0x1A when !Readonly && !SubEditorScreen.IsSubEditor(): // ctrl-z
                    text = memento.Undo();
                    if (text != Text)
                    {
                        ClearSelection();
                        SetText(text, false);
                        CaretIndex = Text.Length;
                        OnTextChanged?.Invoke(this, Text);
                    }
                    break;
                case (char)0x12 when !Readonly && !SubEditorScreen.IsSubEditor(): // ctrl-r
                    text = memento.Redo();
                    if (text != Text)
                    {
                        ClearSelection();
                        SetText(text, false);
                        CaretIndex = Text.Length;
                        OnTextChanged?.Invoke(this, Text);
                    }
                    break;
            }
        }

        public void ReceiveSpecialInput(Keys key)
        {
            switch (key)
            {
                case Keys.Left:
                    if (isSelecting)
                    {
                        InitSelectionStart();
                    }
                    CaretIndex = Math.Max(CaretIndex - 1, 0);
                    caretTimer = 0;
                    HandleSelection();
                    break;
                case Keys.Right:
                    if (isSelecting)
                    {
                        InitSelectionStart();
                    }
                    CaretIndex = Math.Min(CaretIndex + 1, Text.Length);
                    caretTimer = 0;
                    HandleSelection();
                    break;
                case Keys.Up:
                    if (isSelecting)
                    {
                        InitSelectionStart();
                    }
                    float lineHeight = Font.LineHeight * TextBlock.TextScale;
                    int newIndex = textBlock.GetCaretIndexFromLocalPos(new Vector2(caretPos.X, caretPos.Y - lineHeight * 0.5f));
                    textBlock.Font.WrapText(
                        textBlock.Text,
                        (textBlock.Rect.Width - textBlock.Padding.X - textBlock.Padding.Z) / TextBlock.TextScale,
                        newIndex,
                        out Vector2 requestedCharPos);
                    requestedCharPos *= TextBlock.TextScale;
                    if (MathUtils.NearlyEqual(requestedCharPos.Y, caretPos.Y)) { newIndex = 0; }
                    CaretIndex = newIndex;
                    caretTimer = 0;
                    HandleSelection();
                    break;
                case Keys.Down:
                    if (isSelecting)
                    {
                        InitSelectionStart();
                    }
                    lineHeight = Font.LineHeight * TextBlock.TextScale;
                    newIndex = textBlock.GetCaretIndexFromLocalPos(new Vector2(caretPos.X, caretPos.Y + lineHeight * 1.5f));
                    textBlock.Font.WrapText(
                        textBlock.Text,
                        (textBlock.Rect.Width - textBlock.Padding.X - textBlock.Padding.Z) / TextBlock.TextScale,
                        newIndex,
                        out Vector2 requestedCharPos2);
                    requestedCharPos2 *= TextBlock.TextScale;
                    if (MathUtils.NearlyEqual(requestedCharPos2.Y, caretPos.Y)) { newIndex = Text.Length; }
                    CaretIndex = newIndex;
                    caretTimer = 0;
                    HandleSelection();
                    break;
                case Keys.Delete when !Readonly:
                    if (selectedCharacters > 0)
                    {
                        RemoveSelectedText();
                    }
                    else if (Text.Length > 0 && CaretIndex < Text.Length)
                    {
                        SetText(Text.Remove(CaretIndex, 1));
                        OnTextChanged?.Invoke(this, Text);
                        caretPosDirty = true;
                    }
                    break;
                case Keys.Tab:
                    // Select the next text box.
                    var editor = RectTransform.GetParents().Select(p => p.GUIComponent as SerializableEntityEditor).FirstOrDefault(e => e != null);
                    if (editor == null) { break; }
                    var allTextBoxes = GetAndSortTextBoxes(editor).ToList();
                    if (allTextBoxes.Any())
                    {
                        int currentIndex = allTextBoxes.IndexOf(this);
                        int nextIndex = Math.Min(allTextBoxes.Count - 1, currentIndex + 1);
                        var next = allTextBoxes[nextIndex];
                        if (next != this)
                        {
                            next.Select();
                            next.Flash(Color.White * 0.5f, 0.5f);
                        }
                        else
                        {
                            // Select the first text box in the next editor that has text boxes.
                            var listBox = RectTransform.GetParents().Select(p => p.GUIComponent as GUIListBox).FirstOrDefault(lb => lb != null);
                            if (listBox == null) { break; }
                            // TODO: The get's out of focus if the selection is out of view.
                            // Not sure how's that possible, but it seems to work when the auto scroll is disabled and you handle the scrolling manually.
                            listBox.SelectNext();
                            while (SelectNextTextBox(listBox) == null)
                            {
                                var previous = listBox.SelectedComponent;
                                listBox.SelectNext();
                                if (listBox.SelectedComponent == previous) { break; }
                            }
                        }
                    }
                    IEnumerable<GUITextBox> GetAndSortTextBoxes(GUIComponent parent) => parent.GetAllChildren<GUITextBox>().OrderBy(t => t.Rect.Y).ThenBy(t => t.Rect.X);
                    GUITextBox SelectNextTextBox(GUIListBox listBox)
                    {
                        var textBoxes = GetAndSortTextBoxes(listBox.SelectedComponent);
                        if (textBoxes.Any())
                        {
                            var next = textBoxes.First();
                            next.Select();
                            next.Flash(Color.White * 0.5f, 0.5f);
                            return next;
                        }
                        return null;
                    }
                    break;
            }
            if (caretPosDirty) { CalculateCaretPos(); }
            OnKeyHit?.Invoke(this, key);
            void HandleSelection()
            {
                if (isSelecting)
                {
                    InitSelectionStart();
                    CalculateSelection();
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        public void SelectAll()
        {
            CaretIndex = 0;
            CalculateCaretPos();
            selectionStartPos = caretPos;
            selectionStartIndex = 0;
            CaretIndex = Text.Length;
            CalculateSelection();
        }

        private void CopySelectedText()
        {
            Clipboard.SetText(selectedText);
        }

        private void ClearSelection()
        {
            selectedCharacters = 0;
            selectionStartIndex = -1;
            selectionEndIndex = -1;
            selectedText = string.Empty;
        }

        private string GetCopiedText()
        {
            string t;
            t = Clipboard.GetText();

            return t;
        }

        private void RemoveSelectedText()
        {
            if (selectedText.Length == 0) { return; }

            selectionStartIndex = Math.Max(0, Math.Min(selectionEndIndex, Math.Min(selectionStartIndex, Text.Length - 1)));
            int selectionLength = Math.Min(Text.Length - selectionStartIndex, selectedText.Length);
            SetText(Text.Remove(selectionStartIndex, selectionLength));
            CaretIndex = Math.Min(Text.Length, selectionStartIndex);

            ClearSelection();
            OnTextChanged?.Invoke(this, Text);
        }

        private void InitSelectionStart()
        {
            if (caretPosDirty)
            {
                CalculateCaretPos();
            }
            if (selectionStartIndex == -1)
            {
                selectionStartIndex = CaretIndex;
                selectionStartPos = caretPos;
            }
        }

        private void CalculateSelection()
        {
            string textDrawn = Censor ? textBlock.CensoredText : textBlock.WrappedText;
            InitSelectionStart();
            selectionEndIndex = Math.Min(CaretIndex, textDrawn.Length);
            selectionEndPos = caretPos;
            selectedCharacters = Math.Abs(selectionStartIndex - selectionEndIndex);
            try
            {
                if (IsLeftToRight)
                {
                    selectedText = Text.Substring(selectionStartIndex, Math.Min(selectedCharacters, Text.Length));
                    selectionRectSize = Font.MeasureString(textDrawn.Substring(selectionStartIndex, Math.Min(selectedCharacters, textDrawn.Length))) * TextBlock.TextScale;
                }
                else
                {
                    selectedText = Text.Substring(selectionEndIndex, Math.Min(selectedCharacters, Text.Length));
                    selectionRectSize = Font.MeasureString(textDrawn.Substring(selectionEndIndex, Math.Min(selectedCharacters, textDrawn.Length))) * TextBlock.TextScale;
                }
            }
            catch (ArgumentOutOfRangeException exception)
            {
                DebugConsole.ThrowError($"GUITextBox: Invalid selection: ({exception})");
            }
        }
    }
}
