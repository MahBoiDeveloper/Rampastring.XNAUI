﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A text input control.
    /// </summary>
    public class XNATextBox : XNAControl
    {
        const int TEXT_HORIZONTAL_MARGIN = 3;
        const int TEXT_VERTICAL_MARGIN = 2;
        const double SCROLL_REPEAT_TIME = 0.03;
        const double FAST_SCROLL_TRIGGER_TIME = 0.4;

        public XNATextBox(WindowManager windowManager) : base(windowManager)
        {
            IdleBorderColor = UISettings.PanelBorderColor;
            ActiveBorderColor = UISettings.AltColor;
            TextColor = UISettings.AltColor;
            BackColor = UISettings.BackgroundColor;
        }

        public event EventHandler EnterPressed;
        public event EventHandler SelectedChanged;
        public event EventHandler InputReceived;

        public virtual Color TextColor { get; set; }

        public Color IdleBorderColor { get; set; }

        public Color ActiveBorderColor { get; set; }

        public Color BackColor { get; set; }

        public int FontIndex { get; set; }

        int _maximumTextLength = int.MaxValue;

        public int MaximumTextLength
        {
            get { return _maximumTextLength; }
            set { _maximumTextLength = value; }
        }

        public override string Text
        {
            get
            {
                return text;
            }

            set
            {
                text = value;
                InputPosition = 0;
                TextStartPosition = 0;

                if (text.Length > MaximumTextLength)
                    text = text.Substring(0, MaximumTextLength);

                TextEndPosition = text.Length;

                while (!TextFitsBox())
                {
                    TextEndPosition--;

                    if (TextEndPosition < TextStartPosition)
                    {
                        TextEndPosition = TextStartPosition;
                        break;
                    }
                }
            }
        }

        bool active = false;

        /// <summary>
        /// Gets a bool that determines whether the text-box is currently activated.
        /// </summary>
        public bool IsSelected
        {
            get { return active; }
            set
            {
                bool oldValue = active;

                active = value;

                if (active != oldValue)
                    OnSelectedChanged();
            }
        }

        string text = string.Empty;
        string savedText = string.Empty;

        /// <summary>
        /// The input character index inside the textbox text.
        /// </summary>
        public int InputPosition { get; set; }

        /// <summary>
        /// The start character index of the visible part of the text string.
        /// </summary>
        public int TextStartPosition { get; set; }

        /// <summary>
        /// The end character index of the visible part of the text string.
        /// </summary>
        public int TextEndPosition { get; set; }

        bool leftClickHandled = false;

        TimeSpan scrollKeyTime = TimeSpan.Zero;
        TimeSpan timeSinceLastScroll = TimeSpan.Zero;
        bool isScrollingQuickly = false;

        public override void Initialize()
        {
            base.Initialize();

            KeyboardEventInput.CharEntered += KeyboardEventInput_CharEntered;
            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (!active || !Enabled || !Parent.Enabled)
                return;

            switch (e.PressedKey)
            {
                case Keys.Home:
                    TextStartPosition = 0;
                    TextEndPosition = 0;
                    InputPosition = 0;

                    while (true)
                    {
                        if (TextEndPosition < text.Length &&
                            TextFitsBox())
                        {
                            TextEndPosition++;
                            continue;
                        }

                        break;
                    }

                    break;
                case Keys.End:
                    TextEndPosition = text.Length;
                    InputPosition = text.Length;
                    TextStartPosition = 0;

                    while (true)
                    {
                        if (!TextFitsBox())
                        {
                            TextStartPosition++;
                            continue;
                        }

                        break;
                    }

                    break;
                case Keys.X:
                    if (!IsCtrlHeldDown())
                        break;

                    System.Windows.Forms.Clipboard.SetText(text);
                    Text = string.Empty;

                    break;
                case Keys.V:
                    if (!IsCtrlHeldDown())
                        break;

                    Text = System.Windows.Forms.Clipboard.GetText();

                    break;
                case Keys.C:
                    if (!IsCtrlHeldDown())
                        break;

                    System.Windows.Forms.Clipboard.SetText(text);

                    break;
            }
        }

        private void KeyboardEventInput_CharEntered(object sender, KeyboardEventArgs e)
        {
            if (!active || !Enabled || !Parent.Enabled)
                return;

            switch (e.Character)
            {
                case '\r': // Enter / return
                    EnterPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case '\x0009': // Tab
                    break;
                case '\b': // Backspace
                    if (text.Length > 0 && InputPosition > 0)
                    {
                        text = text.Remove(InputPosition - 1, 1);
                        InputPosition--;

                        if (TextStartPosition > 0)
                            TextStartPosition--;

                        TextEndPosition--;
                    }
                    break;
                case '\x001b': // ESC
                    InputPosition = 0;
                    text = string.Empty;
                    TextStartPosition = 0;
                    TextEndPosition = 0;
                    break;
                default:
                    if (text.Length == MaximumTextLength)
                        break;

                    // Don't allow typing characters that don't exist in the spritefont
                    if (Renderer.GetSafeString(e.Character.ToString(), FontIndex) != e.Character.ToString())
                        break;

                    text = text.Insert(InputPosition, e.Character.ToString());
                    InputPosition++;

                    if (TextEndPosition == text.Length - 1 ||
                        InputPosition > TextEndPosition)
                    {
                        TextEndPosition++;

                        while (!TextFitsBox())
                        {
                            TextStartPosition++;
                        }
                    }

                    break;
            }

            InputReceived?.Invoke(this, EventArgs.Empty);
        }

        private bool IsCtrlHeldDown()
        {
            return Keyboard.IsKeyHeldDown(Keys.RightControl) ||
                        Keyboard.IsKeyHeldDown(Keys.LeftControl);
        }

        private bool TextFitsBox()
        {
            if (String.IsNullOrEmpty(text))
                return true;

            return Renderer.GetTextDimensions(
                        text.Substring(TextStartPosition, TextEndPosition - TextStartPosition),
                        FontIndex).X < ClientRectangle.Width - TEXT_HORIZONTAL_MARGIN * 2;
        }

        public override void OnLeftClick()
        {
            IsSelected = true;

            leftClickHandled = true;

            InputPosition = TextEndPosition;

            base.OnLeftClick();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Cursor.LeftClicked && !leftClickHandled)
                IsSelected = false;

            if (IsSelected)
            {
                if (Keyboard.IsKeyHeldDown(Keys.Left))
                    HandleScrollKeyDown(gameTime, ScrollLeft);
                else if (Keyboard.IsKeyHeldDown(Keys.Right))
                    HandleScrollKeyDown(gameTime, ScrollRight);
                else if (Keyboard.IsKeyHeldDown(Keys.Delete))
                    HandleScrollKeyDown(gameTime, DeleteCharacter);
                else
                {
                    isScrollingQuickly = false;
                    timeSinceLastScroll = TimeSpan.Zero;
                    scrollKeyTime = TimeSpan.Zero;
                }
            }

            leftClickHandled = false;
        }

        void ScrollLeft()
        {
            if (InputPosition == 0)
                return;

            InputPosition--;
            if (InputPosition < TextStartPosition)
            {
                TextStartPosition--;

                while (!TextFitsBox())
                    TextEndPosition--;
            }
        }

        void ScrollRight()
        {
            if (InputPosition >= text.Length)
                return;

            InputPosition++;

            if (InputPosition > TextEndPosition)
            {
                TextEndPosition++;

                while (!TextFitsBox())
                {
                    TextStartPosition++;
                }
            }
        }

        void DeleteCharacter()
        {
            if (text.Length > InputPosition)
            {
                text = text.Remove(InputPosition, 1);

                if (TextStartPosition > 0)
                {
                    TextStartPosition--;
                }

                //Logger.Log(textEndPosition.ToString());
                //Logger.Log(text.Length.ToString());

                if (TextEndPosition >= text.Length || !TextFitsBox())
                    TextEndPosition--;
            }
        }

        void HandleScrollKeyDown(GameTime gameTime, Action action)
        {
            if (scrollKeyTime.Equals(TimeSpan.Zero))
                action();

            scrollKeyTime += gameTime.ElapsedGameTime;

            if (isScrollingQuickly)
            {
                timeSinceLastScroll += gameTime.ElapsedGameTime;

                if (timeSinceLastScroll > TimeSpan.FromSeconds(SCROLL_REPEAT_TIME))
                {
                    timeSinceLastScroll = TimeSpan.Zero;
                    action();
                }
            }

            if (scrollKeyTime > TimeSpan.FromSeconds(FAST_SCROLL_TRIGGER_TIME) && !isScrollingQuickly)
            {
                isScrollingQuickly = true;
                timeSinceLastScroll = TimeSpan.Zero;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle displayRectangle = WindowRectangle();

            Renderer.FillRectangle(displayRectangle, BackColor);

            if (active && Enabled)
                Renderer.DrawRectangle(displayRectangle, ActiveBorderColor);
            else
                Renderer.DrawRectangle(displayRectangle, IdleBorderColor);

            Renderer.DrawStringWithShadow(Text.Substring(TextStartPosition, TextEndPosition - TextStartPosition),
                FontIndex, new Vector2(displayRectangle.X + TEXT_HORIZONTAL_MARGIN, displayRectangle.Y + TEXT_VERTICAL_MARGIN),
                TextColor);

            if (active && Enabled)
            {
                int barLocationX = TEXT_HORIZONTAL_MARGIN;

                string inputText = text.Substring(TextStartPosition, InputPosition - TextStartPosition);
                barLocationX += (int)Renderer.GetTextDimensions(inputText, FontIndex).X;

                Renderer.DrawRectangle(new Rectangle(displayRectangle.X + barLocationX,
                    displayRectangle.Y + 2, 1, displayRectangle.Height - 4), Color.White);
            }

            base.Draw(gameTime);
        }

        public virtual void OnSelectedChanged()
        {
            SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
