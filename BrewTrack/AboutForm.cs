using System;
using System.Drawing;
using System.Windows.Forms;

namespace BrewTrack
{
    public partial class AboutForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);

        public AboutForm()
        {
            Text            = "About BrewTrack";
            Size            = new Size(420, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = PurpleLight;
            Font            = new Font("Segoe UI", 9f);

            var card = new Panel
            {
                Size      = new Size(360, 430),
                Location  = new Point(30, 30),
                BackColor = Color.White,
                Padding   = new Padding(20)
            };
            RoundCorners(card, 14);
            Controls.Add(card);

            // Logo circle
            var logo = new Panel
            {
                Size      = new Size(70, 70),
                Location  = new Point(145, 20),
                BackColor = Purple
            };
            RoundCorners(logo, 35);
            card.Controls.Add(logo);
            logo.Controls.Add(new Label
            {
                Text      = "☕",
                Font      = new Font("Segoe UI Emoji", 24f),
                ForeColor = Color.White,
                AutoSize  = false,
                Size      = new Size(70, 70),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            card.Controls.Add(MakeLbl("BrewTrack",            16, true,  Purple,   new Point(0, 100)));
            var titleLbl = card.Controls[card.Controls.Count - 1] as Label;
            titleLbl.Location = new Point((card.Width - titleLbl.PreferredWidth) / 2, 100);

            card.Controls.Add(MakeLbl("Café Inventory System", 9, false, GrayText, new Point(0, 128)));
            var subLbl = card.Controls[card.Controls.Count - 1] as Label;
            subLbl.Location = new Point((card.Width - subLbl.PreferredWidth) / 2, 128);

            var div = new Panel { Size = new Size(300, 1), Location = new Point(30, 155), BackColor = PurpleLight };
            card.Controls.Add(div);

            card.Controls.Add(MakeLbl("Version",     8.5f, false, GrayText,                    new Point(30, 168)));
            card.Controls.Add(MakeLbl("1.0.0",       9.5f, true,  Purple,                      new Point(30, 185)));

            card.Controls.Add(MakeLbl("Database",    8.5f, false, GrayText,                    new Point(30, 215)));
            card.Controls.Add(MakeLbl("brewtrackdb (MariaDB 10.4)", 9f, false, Color.FromArgb(50,40,90), new Point(30, 232)));

            card.Controls.Add(MakeLbl("Technology",  8.5f, false, GrayText,                    new Point(30, 262)));
            card.Controls.Add(MakeLbl("C# · .NET Framework · Windows Forms", 9f, false, Color.FromArgb(50,40,90), new Point(30, 279)));

            card.Controls.Add(MakeLbl("Built for",   8.5f, false, GrayText,                    new Point(30, 309)));
            card.Controls.Add(MakeLbl("IT 120 Event Driven Programming", 9f, false, Color.FromArgb(50,40,90), new Point(30, 326)));

            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(300, 38),
                Location  = new Point(30, 375),
                BackColor = Purple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            RoundCorners(btnClose, 8);
            btnClose.Click += (s, e) => Close();
            card.Controls.Add(btnClose);
        }

        Label MakeLbl(string t, float sz, bool bold, Color c, Point loc)
        {
            return new Label
            {
                Text      = t,
                Font      = new Font("Segoe UI", sz, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = c,
                AutoSize  = true,
                Location  = loc
            };
        }

        void RoundCorners(Control ctrl, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, r * 2, r * 2, 180, 90);
            path.AddArc(ctrl.Width - r * 2, 0, r * 2, r * 2, 270, 90);
            path.AddArc(ctrl.Width - r * 2, ctrl.Height - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(0, ctrl.Height - r * 2, r * 2, r * 2, 90, 90);
            path.CloseAllFigures();
            ctrl.Region = new Region(path);
        }
    }
}
