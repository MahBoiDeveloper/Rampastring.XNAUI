using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A tool tip.
/// </summary>
public class XNAToolTip : XNAControl
{
    /// <summary>
    /// If set to true - makes tooltip not appear and instantly hides it if currently shown.
    /// </summary>
    public bool Blocked { get; set; }

    /// <summary>
    /// Whether the tooltip should move with the cursor after it was shown.
    /// </summary>
    public bool FollowCursor { get; set; }

    /// <summary>
    /// Defines what font should be used from pool.
    /// </summary>
    public int FontIndex { get; set; }

    /// <summary>
    /// Tooltip alpha rate per second.
    /// </summary>
    public float AlphaRate { get; set; }

    /// <summary>
    /// Tooltip margin.
    /// </summary>
    public int Margin { get; set; }

    /// <summary>
    /// Tooltip offset per X axis.
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// Tooltip offset per Y axis.
    /// </summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// Delay in milliseconds before tooltip would be shown.
    /// </summary>
    public float Delay {  get; set; }

    /// <summary>
    /// Creates a new tool tip and attaches it to the given control.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    /// <param name="masterControl">The control to attach the tool tip to.</param>
    public XNAToolTip(WindowManager windowManager, XNAControl masterControl) : base(windowManager)
    {
        this.masterControl = masterControl ?? throw new ArgumentNullException("masterControl");
        masterControl.MouseEnter += MasterControl_MouseEnter;
        masterControl.MouseLeave += MasterControl_MouseLeave;
        masterControl.MouseMove += MasterControl_MouseMove;
        masterControl.EnabledChanged += MasterControl_EnabledChanged;
        InputEnabled = false;
        DrawOrder = int.MaxValue;
        masterControl.Parent.AddChild(this);
        Visible = false;
    }

    private void MasterControl_EnabledChanged(object sender, EventArgs e)
        => Enabled = masterControl.Enabled;

    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            Vector2 textSize = Renderer.GetTextDimensions(base.Text ?? string.Empty, FontIndex);
            Width = (int)textSize.X + Margin * 2;
            Height = (int)textSize.Y + Margin * 2;

            if (string.IsNullOrEmpty(Text))
            {
                Alpha = 0f;
                Visible = false;
            }
        }
    }

    public override float Alpha { get; set; }
    public bool IsMasterControlOnCursor { get; set; }

    private XNAControl masterControl;

    private TimeSpan cursorTime = TimeSpan.Zero;

    private void MasterControl_MouseEnter(object sender, EventArgs e)
    {
        IsMasterControlOnCursor = true;

        if (string.IsNullOrEmpty(Text))
            return;

        DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
            new Point(OffsetX, OffsetY)));
    }

    private void MasterControl_MouseLeave(object sender, EventArgs e)
    {
        IsMasterControlOnCursor = false;
        cursorTime = TimeSpan.Zero;
    }

    private void MasterControl_MouseMove(object sender, EventArgs e)
    {
        if ((FollowCursor || !Visible) && !string.IsNullOrEmpty(Text))
        {
            // Move the tooltip if the cursor has moved while staying 
            // on the control area and we're invisible or we follow the cursor
            DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
                new Point(OffsetX, OffsetY)));
        }
    }

    /// <summary>
    /// Sets the tool tip's location, checking that it doesn't exceed the window's bounds.
    /// </summary>
    /// <param name="location">The point at location coordinates.</param>
    public void DisplayAtLocation(Point location)
    {
        X = location.X + Width > WindowManager.RenderResolutionX ?
            WindowManager.RenderResolutionX - Width : location.X;
        Y = location.Y - Height < 0 ? 0 : location.Y - Height;
    }

    public override void Update(GameTime gameTime)
    {
        if (Blocked || string.IsNullOrEmpty(Text))
        {
            Alpha = 0f;
            Visible = false;
            return;
        }

        if (IsMasterControlOnCursor)
        {
            cursorTime += gameTime.ElapsedGameTime;

            if (cursorTime > TimeSpan.FromSeconds(Delay))
            {
                Alpha += AlphaRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                Visible = true;
                if (Alpha > 1.0f)
                    Alpha = 1.0f;
                return;
            }
        }

        Alpha -= AlphaRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (Alpha < 0f)
        {
            Alpha = 0f;
            Visible = false;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Renderer.FillRectangle(ClientRectangle,
            UISettings.ActiveSettings.BackgroundColor * Alpha);
        Renderer.DrawRectangle(ClientRectangle,
            UISettings.ActiveSettings.AltColor * Alpha);
        Renderer.DrawString(Text, FontIndex,
            new Vector2(X + Margin, Y + Margin),
            UISettings.ActiveSettings.AltColor * Alpha, 1.0f);
    }

    private Point SumPoints(Point p1, Point p2)
        // This is also needed for XNA compatibility
        #if XNA
            => new Point(p1.X + p2.X, p1.Y + p2.Y);
        #else
            => p1 + p2;
        #endif
}