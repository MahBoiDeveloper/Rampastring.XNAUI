﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Globalization;
using System.IO;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls;

public class XNAPanel : XNAControl
{
    public XNAPanel(WindowManager windowManager) : base(windowManager)
    {
    }

    public PanelBackgroundImageDrawMode PanelBackgroundDrawMode { get; set; } = PanelBackgroundImageDrawMode.STRETCHED;

    public virtual Texture2D BackgroundTexture { get; set; }

    public virtual List<Texture2D> BackgroundAnimation { get; set; }
    public virtual List<int> Delays { get; set; }

    protected int currentFrameId = 0;
    protected int totalElapsedTime = 0;
    protected int frameDelay = 0;

    private Color? _borderColor;

    public Color BorderColor
    {
        get
        {
            if (_borderColor.HasValue)
                return _borderColor.Value;

            return UISettings.ActiveSettings.PanelBorderColor;
        }
        set { _borderColor = value; }
    }

    public bool DrawBorders { get; set; } = true;

    //RenderTarget2D renderTarget;

    // TODO implement custom border texture
    // Texture2D BorderTexture { get; set; }

    /// <summary>
    /// The panel's transparency changing rate per 100 milliseconds.
    /// If the panel is transparent, it'll become non-transparent at this rate.
    /// </summary>
    public float AlphaRate = 0.0f;

    public override void Initialize()
    {
        base.Initialize();

        // BorderTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "BorderColor":
                BorderColor = AssetLoader.GetColorFromString(value);
                return;
            case "DrawMode":
                if (value == "Tiled")
                    PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
                else if (value == "Centered")
                    PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;
                else
                    PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                return;
            case "AlphaRate":
                AlphaRate = Conversions.FloatFromString(value, 0.01f);
                return;
            case "BackgroundTexture":
                BackgroundTexture = AssetLoader.LoadTexture(value);
                return;
            case "BackgroundAnimation":
                BackgroundAnimation = new List<Texture2D>();
                Delays = new List<int>();
                var gif = AssetLoader.LoadAnimation(value);
                frameDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;
                Height = gif.Height;
                Width = gif.Width;

                try
                {
                    for (;;)
                    {
                        var delay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;
                        var currentFrame = gif.Frames.ExportFrame(0);

                        Delays.Add(delay);
                        BackgroundAnimation.Add(AssetLoader.TextureFromImage(currentFrame));
                    }
                }
                catch
                {
                }
                
                return;
            case "SolidColorBackgroundTexture":
                BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(value), 2, 2);
                PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                return;
            case "DrawBorders":
                DrawBorders = Conversions.BooleanFromString(value, true);
                return;
            case "Padding":
                string[] parts = value.Split(',');
                int left = int.Parse(parts[0], CultureInfo.InvariantCulture);
                int top = int.Parse(parts[1], CultureInfo.InvariantCulture);
                int right = int.Parse(parts[2], CultureInfo.InvariantCulture);
                int bottom = int.Parse(parts[3], CultureInfo.InvariantCulture);
                ClientRectangle = new Rectangle(X - left, Y - top,
                    Width + left + right, Height + top + bottom);
                foreach (XNAControl child in Children)
                {
                    child.ClientRectangle = new Rectangle(child.X + left,
                        child.Y + top, child.Width, child.Height);
                }
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Update(GameTime gameTime)
    {
        Alpha += AlphaRate * (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 100.0);

        if (BackgroundAnimation != null && BackgroundAnimation.Count != 0 && Delays.Count != 0)
        {
            int currentElapsedTime = Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);
            totalElapsedTime += currentElapsedTime;

            if (totalElapsedTime > frameDelay)
            {
                totalElapsedTime = 0;
                frameDelay = Delays[currentFrameId];
                BackgroundTexture = BackgroundAnimation[currentFrameId];
                currentFrameId = ++currentFrameId % BackgroundAnimation.Count;
            }
        }

        base.Update(gameTime);
    }

    protected void DrawBackgroundTexture(Texture2D texture, Color color)
    {
        if (texture != null)
        {
            if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.TILED)
            {
                if (Renderer.CurrentSettings.SamplerState != SamplerState.LinearWrap &&
                    Renderer.CurrentSettings.SamplerState != SamplerState.PointWrap)
                {
                    //Renderer.PushSettings(new SpriteBatchSettings(Renderer.CurrentSettings.SpriteSortMode,
                    //    Renderer.CurrentSettings.BlendState, SamplerState.LinearWrap));

                    //DrawTexture(texture, new Rectangle(0, 0, Width, Height), color);

                    //Renderer.PopSettings();
                    // ^ the above should work, but actually doesn't for some reason -
                    // the texture is just scaled instead
                    // it should have much higher performance than repeating the texture manually

                    for (int x = 0; x < Width; x += texture.Width)
                    {
                        for (int y = 0; y < Height; y += texture.Height)
                        {
                            if (x + texture.Width < Width)
                            {
                                if (y + texture.Height < Height)
                                {
                                    DrawTexture(texture, new Rectangle(x, y,
                                        texture.Width, texture.Height), color);
                                }
                                else
                                {
                                    DrawTexture(texture,
                                        new Rectangle(0, 0, texture.Width, Height - y),
                                        new Rectangle(x, y,
                                        texture.Width, Height - y), color);
                                }
                            }
                            else if (y + texture.Height < Height)
                            {
                                DrawTexture(texture,
                                    new Rectangle(0, 0, Width - x, texture.Height),
                                    new Rectangle(x, y,
                                    Width - x, texture.Height), color);
                            }
                            else
                            {
                                DrawTexture(texture,
                                    new Rectangle(0, 0, Width - x, Height - y),
                                    new Rectangle(x, y,
                                    Width - x, Height - y), color);
                            }
                        }
                    }
                }
                else
                {
                    DrawTexture(texture, new Rectangle(0, 0, Width, Height), color);
                }
            }
            else if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.CENTERED)
            {
                int x = (Width - texture.Width) / 2;
                int y = (Height - texture.Height) / 2;

                // Calculate texture source rectangle
                int sourceBeginX = x >= 0 ? 0 : -x;
                int sourceBeginY = y >= 0 ? 0 : -y;

                // Calculate draw destination rectangle
                int destBeginX = x >= 0 ? x : 0;
                int destBeginY = y >= 0 ? y : 0;

                // Width and height is shared between both rectangles
                int drawWidth = x >= 0 ? texture.Width : Width;
                int drawHeight = y >= 0 ? texture.Height : Height;

                DrawTexture(texture,
                    new Rectangle(sourceBeginX, sourceBeginY, drawWidth, drawHeight),
                    new Rectangle(destBeginX, destBeginY, drawWidth, drawHeight), color);
            }
            else // if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.STRECHED)
            {
                DrawTexture(texture, new Rectangle(0, 0, Width, Height), color);
            }
        }
    }

    protected void DrawPanel()
    {
        DrawBackgroundTexture(BackgroundTexture, RemapColor);
    }

    protected void DrawPanelBorders()
    {
        DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);
    }

    public override void Draw(GameTime gameTime)
    {
        DrawPanel();

        base.Draw(gameTime);

        if (DrawBorders)
            DrawPanelBorders();
    }
}

public enum PanelBackgroundImageDrawMode
{
    /// <summary>
    /// The texture is tiled to fill the whole surface of the panel.
    /// </summary>
    TILED,

    /// <summary>
    /// The texture is stretched to fill the whole surface of the panel.
    /// </summary>
    STRETCHED,

    /// <summary>
    /// The texture is drawn once, centered on the panel.
    /// If the texture is too large for the panel, parts
    /// that would end up outside of the panel are cut off.
    /// </summary>
    CENTERED
}
