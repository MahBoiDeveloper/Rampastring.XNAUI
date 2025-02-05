﻿using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI;

class Frame
{
    public Texture2D Texture { get; set; }
    public TimeSpan Delay { get; set; }
}

public class Animation
{
    private List<Frame> Frames;
    private int currentFrameId = 0;
    private int currentDelay = 0;
    private int totalElapsedTime = 0;

    public Texture2D CurrentFrame { get; private set; }
    public int Height { get; }
    public int Width { get; }

    public Animation(string value) 
    {
        Frames = new List<Frame>();
        
        var gif = AssetLoader.LoadGIFAnimation(value);
        currentDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;

        Height = gif.Height;
        Width = gif.Width;

        // ImageSharp doesn't return last gif frame
        int len = gif.Frames.Count - 1;

        for (int i = 0; i < len; i++)
        {
            // ImageSharp returns not milliseconds, but decisecond
            var delay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;
            var currentFrame = gif.Frames.ExportFrame(0);

            Frames.Add(new Frame { Texture = AssetLoader.TextureFromImage(currentFrame), Delay = TimeSpan.FromMilliseconds(delay) });
        }

        CurrentFrame = Frames[0].Texture;
    }

    public Texture2D Next(GameTime gameTime)
    {
        totalElapsedTime += Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);

        if (totalElapsedTime > currentDelay)
        {
            totalElapsedTime = 0;
            return Next();
        }

        return null;
    }

    public Texture2D Next()
    {
        currentFrameId = (currentFrameId + 1) % Frames.Count;
        currentDelay = Frames[currentFrameId].Delay.Milliseconds;
        CurrentFrame = Frames[currentFrameId].Texture;
        return CurrentFrame;
    }
}
