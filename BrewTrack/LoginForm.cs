using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    public partial class LoginForm : Form
    {
        // ── Controls ──────────────────────────────────────────────
        private Label   lblTitle, lblSub, lblUser, lblPass, lblError;
        private TextBox txtUser, txtPass;
        private Button  btnLogin, btnForgot;
        private Panel   pnlCard;
        private PictureBox picLogo;

        // ── Colors ────────────────────────────────────────────────
        static readonly Color Purple     = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleLight= Color.FromArgb(237, 234, 255);
        static readonly Color PurpleDark = Color.FromArgb(58,  42, 130);
        static readonly Color White      = Color.White;
        static readonly Color GrayText   = Color.FromArgb(110, 105, 140);

        public LoginForm()
        {
            BuildUI();
        }

        void BuildUI()
        {
            // ── Window ────────────────────────────────────────────
            Text            = "BrewTrack — Login";
            Size            = new Size(480, 600);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = PurpleLight;
            Font            = new Font("Segoe UI", 9f);

            // ── Card panel ────────────────────────────────────────
            pnlCard = new Panel
            {
                Size      = new Size(380, 460),
                Location  = new Point(50, 60),
                BackColor = White,
                Padding   = new Padding(30)
            };
            RoundCorners(pnlCard, 16);
            Controls.Add(pnlCard);

            // ── Logo circle ───────────────────────────────────────
            picLogo = new PictureBox
            {
                Size      = new Size(70, 70),
                Location  = new Point(155, 20),
                BackColor = Purple
            };
            RoundCorners(picLogo, 35);
            pnlCard.Controls.Add(picLogo);

            var lblCup = new Label
            {
                Text      = "☕",
                Font      = new Font("Segoe UI Emoji", 22f),
                ForeColor = White,
                AutoSize  = false,
                Size      = new Size(70, 70),
                Location  = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            picLogo.Controls.Add(lblCup);

           

            // (centre properly)
            lblTitle = new Label
            {
                Text      = "BrewTrack",
                Font      = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize  = true
            };
            pnlCard.Controls.Add(lblTitle);
            lblTitle.Location = new Point((pnlCard.Width - lblTitle.PreferredWidth) / 2, 90);

            lblSub = new Label
            {
                Text      = "Café Inventory System",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = GrayText,
                AutoSize  = true
            };
            pnlCard.Controls.Add(lblSub);
            lblSub.Location = new Point((pnlCard.Width - lblSub.PreferredWidth) / 2, 130);

            // ── Fields ────────────────────────────────────────────
            MakeLabel("Username", 9, false, GrayText, new Point(30, 165), null, pnlCard);
            txtUser = MakeTextBox(new Point(30, 185), new Size(320, 36), pnlCard);
            

            MakeLabel("Password", 9, false, GrayText, new Point(30, 235), null, pnlCard);
            txtPass = MakeTextBox(new Point(30, 255), new Size(320, 36), pnlCard, isPassword: true);
            

            // ── Error label ───────────────────────────────────────
            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.Crimson,
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = true,
                Location  = new Point(30, 300)
            };
            pnlCard.Controls.Add(lblError);

            // ── Login button ──────────────────────────────────────
            btnLogin = new Button
            {
                Text      = "Log In",
                Size      = new Size(320, 42),
                Location  = new Point(30, 325),
                BackColor = Purple,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize  = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = PurpleDark;
            RoundCorners(btnLogin, 8);
            btnLogin.Click += BtnLogin_Click;
            pnlCard.Controls.Add(btnLogin);

            // ── Forgot password link ──────────────────────────────
            btnForgot = new Button
            {
                Text      = "Forgot password?",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Purple,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Underline),
                AutoSize  = true,
                Cursor    = Cursors.Hand,
                Location  = new Point(0, 380)
            };
            btnForgot.FlatAppearance.BorderSize = 0;
            btnForgot.Click += (s, e) => new PasswordRecoveryForm().ShowDialog();
            pnlCard.Controls.Add(btnForgot);
            btnForgot.Location = new Point((pnlCard.Width - btnForgot.Width) / 2, 380);

            // ── About link ────────────────────────────────────────
            var btnAbout = new Button
            {
                Text      = "About BrewTrack",
                FlatStyle = FlatStyle.Flat,
                ForeColor = GrayText,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8f),
                AutoSize  = true,
                Cursor    = Cursors.Hand,
                Location  = new Point(0, 410)
            };
            btnAbout.FlatAppearance.BorderSize = 0;
            btnAbout.Click += (s, e) => new AboutForm().ShowDialog();
            pnlCard.Controls.Add(btnAbout);
            btnAbout.Location = new Point((pnlCard.Width - btnAbout.Width) / 2, 410);

            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };
        }

        void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Please enter both username and password.";
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    string sql = "SELECT Username FROM users WHERE Username=@u AND Password=@p";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user);
                        cmd.Parameters.AddWithValue("@p", pass);

                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            var dash = new DashboardForm(user);
                            dash.FormClosed += (s2, e2) => this.Close();
                            this.Hide();
                            dash.Show();
                        }
                        else
                        {
                            lblError.Text = "Invalid username or password.";
                            txtPass.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "DB Error: " + ex.Message;
            }
        }

        // ── Helpers ───────────────────────────────────────────────
        Label MakeLabel(string text, float size, bool bold, Color color,
                        Point loc, Size? sz, Panel parent)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                AutoSize  = sz == null,
                Location  = loc
            };
            if (sz.HasValue) lbl.Size = sz.Value;
            parent.Controls.Add(lbl);
            return lbl;
        }

        TextBox MakeTextBox(Point loc, Size sz, Panel parent, bool isPassword = false)
        {
            var tb = new TextBox
            {
                Location        = loc,
                Size            = sz,
                BorderStyle     = BorderStyle.FixedSingle,
                Font            = new Font("Segoe UI", 9.5f),
                BackColor       = Color.FromArgb(249, 247, 255),
                ForeColor       = Color.FromArgb(40, 30, 70),
                UseSystemPasswordChar = isPassword
            };
            parent.Controls.Add(tb);
            return tb;
        }

        void RoundCorners(Control ctrl, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(ctrl.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(ctrl.Width - radius * 2, ctrl.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, ctrl.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            ctrl.Region = new Region(path);
        }
    }
}
