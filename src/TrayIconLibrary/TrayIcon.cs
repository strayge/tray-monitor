﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TrayIconLibrary;

namespace IconLibrary
{
    class TrayIcon
    {
        private NotifyIcon notifyIcon;

        // for store state and divide enable/disable clicks
        MenuItem contextMenuAutostartItem;

        public TrayIcon()
        {
            notifyIcon = new NotifyIcon();
            CreateContextMenu();

            updateIconTimer = new Timer();
            updateIconTimer.Interval = 1000;
            updateIconTimer.Tick += new EventHandler(UpdateIconTick);

            // icon's tooltip
            notifyIcon.MouseMove += IconMouseMoveEvent;
            // balloon tip
            notifyIcon.MouseClick += IconMouseClickEvent;
        }

        public void SetUpdateInterval(int interval)
        {
            updateIconTimer.Interval = interval;
        }

        private void CreateContextMenu()
        {
            contextMenuAutostartItem = new MenuItem("&Autostart", ContextMenuAutostart)
            {
                Checked = Autostart.IsEnabled()
            };
            notifyIcon.ContextMenu = new ContextMenu(new[]
            {
                contextMenuAutostartItem,
                new MenuItem("-"),
                new MenuItem("&Settings", ContextMenuSettings),
                new MenuItem("E&xit", ContextMenuExit),
            });
        }

        private void ContextMenuExit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        // custom form with settings can be showed here
        public virtual void ContextMenuSettings(object sender, EventArgs e) { }

        private void ContextMenuAutostart(object sender, EventArgs e)
        {
            if (contextMenuAutostartItem.Checked)
            {
                Autostart.Disable();
            }
            else
            {
                Autostart.Enable();
            }
            contextMenuAutostartItem.Checked = Autostart.IsEnabled();
        }

        public void DisableIcon()
        {
            updateIconTimer.Stop();
            notifyIcon.Visible = false;
        }

        public void EnableIcon()
        {
            updateIconTimer.Start();
            notifyIcon.Visible = true;
            UpdateIconTick();
        }

        // for update icon
        public virtual void UpdateIconTick(object sender = null, EventArgs e = null) { }

        private Timer updateIconTimer;

        protected int GetTrayIconsSize()
        {
            // get size for tray icons on current system (depends on DPI)
            return WinApi.GetSystemMetrics(WinApi.SM_CXSMICON);
        }

        // depends on DPI
        public int WidthSingleMeasurement()
        {
            if (GetTrayIconsSize() <= 16)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        // old icon cached to allow changing only tooltip
        private Bitmap cachedIconBitmap;

        // update both icon and tooltip
        public void ChangeIcon(Bitmap bitmap = null, string tooltip = null)
        {
            if (tooltip != null)
            {
                notifyIcon.Text = tooltip;
            }

            if (bitmap == null)
            {
                bitmap = cachedIconBitmap;
            }
            else
            {
                if (cachedIconBitmap != null)
                {
                    WinApi.DestroyIcon(cachedIconBitmap.GetHicon());
                    cachedIconBitmap.Dispose();
                }
                cachedIconBitmap = (Bitmap)bitmap.Clone();
            }

            if (bitmap != null)
            {
                using (Icon icon = Icon.FromHandle(bitmap.GetHicon()))
                {
                    notifyIcon.Icon = icon;
                }
            }
        }

        // update only icon
        public void SetIcon(Bitmap bitmap)
        {
            ChangeIcon(bitmap, null);
        }

        // update only tooltip
        public void SetTooltip(string text)
        {
            ChangeIcon(null, text);
        }

        // will be executed only after hovering mouse
        // it allow contains here more heavy calculations
        // than at each icon updating
        public virtual void IconHovered() { }

        private DateTime latestTooltipHoverUpdate;

        private void IconMouseMoveEvent(object sender = null, EventArgs e = null)
        {
            // fire TooltipHovered event with minimum delay equals 1000 ms
            if (latestTooltipHoverUpdate == null || (DateTime.UtcNow - latestTooltipHoverUpdate).TotalMilliseconds > 1000)
            {
                IconHovered();
                latestTooltipHoverUpdate = DateTime.UtcNow;
            }
        }

        // show balloontip on left moube button
        private void IconMouseClickEvent(object sender, MouseEventArgs e)
        {
            if (balloonText != null && e.Button == MouseButtons.Left)
            {
                ShowBalloon(balloonText, balloonTitle);
            }
        }

        private Timer balloonTimer;
        private string balloonText;
        private string balloonTitle;

        // set data for balloontip
        public void SetBalloon(string text = null, string title = null)
        {
            balloonText = text;
            balloonTitle = title;
        }

        private void ShowBalloon(string text, string title = null)
        {
            if (balloonTimer != null)
            {
                return;
            }
            balloonTimer = new Timer();
            balloonTimer.Interval = 2000;
            balloonTimer.Tick += HideBalloon;
            balloonTimer.Start();
            notifyIcon.BalloonTipClosed += HideBalloon;
            notifyIcon.ShowBalloonTip(2000, title, text, ToolTipIcon.Info);
        }

        private void HideBalloon(object sender = null, EventArgs e = null)
        {
            if (balloonTimer == null)
            {
                return;
            }
            balloonTimer.Stop();
            balloonTimer.Dispose();
            balloonTimer = null;
        }

    }
}