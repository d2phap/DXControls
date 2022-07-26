﻿using DirectN;
using System.Runtime.InteropServices;

namespace DXControl;

public class DXControl : Control
{
    private readonly IComObject<ID2D1Factory> Direct2dFactory;
    private ID2D1HwndRenderTarget? RenderTarget;
    private ID2D1DeviceContext? DeviceContext;

    public DXControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

        Direct2dFactory = D2D1Functions.D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED);
    }


    #region Override functions

    protected override void CreateHandle()
    {
        base.CreateHandle();
        if (DesignMode) return;

        DoubleBuffered = false;

        CreateGraphicsResources();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_ERASEBKGND = 0x0014;
        const int WM_SIZE = 0x0005;
        const int WM_DESTROY = 0x0002;

        switch (m.Msg)
        {
            //case WM_ERASEBKGND:

            //    // to fix background is delayed to paint on launch
            //    if (_firstPaintBackground)
            //    {
            //        _firstPaintBackground = false;
            //        if (!_useHardwardAcceleration)
            //        {
            //            base.WndProc(ref m);
            //        }
            //        else
            //        {
            //            _graphics?.BeginRender(D2DColor.FromGDIColor(BackColor));
            //            _graphics?.EndRender();
            //        }
            //    }
            //    break;

            case WM_SIZE:
                base.WndProc(ref m);

                //RenderTarget?.Resize(new(ClientSize.Width, ClientSize.Height));
                break;


            default:
                base.WndProc(ref m);
                break;
        }
    }


    protected override void DestroyHandle()
    {
        base.DestroyHandle();

        DisposeResources();
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }


    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (DesignMode) return;


        RenderTarget?.Resize(new(ClientSize.Width, ClientSize.Height));

        // update the control once size/windows state changed
        ResizeRedraw = true;
    }


    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        if (DesignMode)
        {
            base.OnPaintBackground(pevent);
        }
    }


    [Obsolete("Use 'OnRender' to paint the control.", true)]
    protected override void OnPaint(PaintEventArgs e)
    {
        if (DesignMode)
        {
            e.Graphics.Clear(BackColor);

            using var brush = new SolidBrush(ForeColor);
            e.Graphics.DrawString("This control does not support rendering in design mode.",
                Font, brush, 10, 10);

            return;
        }

        // make sure the 
        CreateGraphicsResources();
        if (DeviceContext == null) return;


        // start drawing session
        DeviceContext.BeginDraw();
        ClearBackground(BackColor);

        OnRender(DeviceContext);

        // end drawing session
        DeviceContext.EndDraw();
    }


    #endregion


    #region Virtual functions

    /// <summary>
    /// Paints the control using <see cref="DXGraphics"/>.
    /// </summary>
    protected virtual void OnRender(ID2D1DeviceContext dc)
    {
        var brushProps = new D2D1_BRUSH_PROPERTIES()
        {
            opacity = 1f,
        };

        dc.CreateSolidColorBrush(new(1, 1, 0, 1), brushProps.StructureToPtr(), out var brush);

        dc.DrawLine(
            new(0, 0),
            new D2D_POINT_2F(ClientSize.Width, ClientSize.Height),
            brush, 3.0f);

        dc.DrawLine(
            new D2D_POINT_2F(ClientSize.Width, 0),
            new(0, ClientSize.Height),
            brush, 3.0f);
    }

    #endregion



    #region Private functions

    /// <summary>
    /// Create graphics resources.
    /// </summary>
    private void CreateGraphicsResources()
    {
        if (RenderTarget == null)
        {
            var hwndRenderTargetProps = new D2D1_HWND_RENDER_TARGET_PROPERTIES()
            {
                hwnd = Handle,
                pixelSize = new D2D_SIZE_U((uint)Width, (uint)Height),
            };

            Direct2dFactory.Object.CreateHwndRenderTarget(new D2D1_RENDER_TARGET_PROPERTIES(), hwndRenderTargetProps, out RenderTarget).ThrowOnError();

            RenderTarget.Resize(new(ClientSize.Width, ClientSize.Height));
            RenderTarget.SetDpi(96.0f, 96.0f);

            DeviceContext = (ID2D1DeviceContext)RenderTarget;
        }
    }


    /// <summary>
    /// Dispose graphics resources.
    /// </summary>
    private void DisposeResources()
    {
        if (DeviceContext != null)
        {
            Marshal.ReleaseComObject(DeviceContext);
            DeviceContext = null;
        }

        if (RenderTarget != null)
        {
            Marshal.ReleaseComObject(RenderTarget);
            RenderTarget = null;
        }

        Direct2dFactory.Dispose();
    }

    #endregion


    #region Public functions

    /// <summary>
    /// Clear the background by the given color.
    /// </summary>
    public void ClearBackground(Color color)
    {
        var value = DXHelper.ConvertColor(color);
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(_D3DCOLORVALUE)));
        Marshal.StructureToPtr(value, ptr, false);
        DeviceContext?.Clear(ptr);
        Marshal.FreeHGlobal(ptr);
    }

    #endregion

}
