using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    public partial class PasswordRecoveryForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color PurpleDark  = Color.FromArgb(58,  42, 130);
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);

        TextBox txtUsername;
        TextBox txtNewPass, txtConfirm;
        Label   lblStatus;

        public PasswordRecoveryForm()
        {
            Text            = "Password Recovery";
            Size            = new Size(420, 440);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = PurpleLight;
            Font            = new Font("Segoe UI", 9f);

            var card = new Panel
            {
                Size      = new Size(340, 360),
                Location  = new Point(40, 30),
                BackColor = Color.White,
                Padding   = new Padding(20)
            };
            RoundCorners(card, 14);
            Controls.Add(card);

            // Title
            var lbl = new Label
            {
                Text      = "🔑  Password Recovery",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize  = true,
                Location  = new Point(20, 20)
            };
            card.Controls.Add(lbl);

            var sub = new Label
            {
                Text      = "Reset your account password",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = GrayText,
                AutoSize  = true,
                Location  = new Point(20, 50)
            };
            card.Controls.Add(sub);

            AddLabel(card, "Username", new Point(20, 80));
            txtUsername = AddTextBox(card, new Point(20, 98), new Size(300, 34));
            

            AddLabel(card, "New Password", new Point(20, 145));
            txtNewPass = AddTextBox(card, new Point(20, 163), new Size(300, 34), isPass: true);
            

            AddLabel(card, "Confirm Password", new Point(20, 210));
            txtConfirm = AddTextBox(card, new Point(20, 228), new Size(300, 34), isPass: true);
            
            lblStatus = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Crimson,
                AutoSize  = true,
                Location  = new Point(20, 272)
            };
            card.Controls.Add(lblStatus);

            var btnReset = new Button
            {
                Text      = "Reset Password",
                Size      = new Size(300, 40),
                Location  = new Point(20, 295),
                BackColor = Purple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatAppearance.MouseOverBackColor = PurpleDark;
            RoundCorners(btnReset, 8);
            btnReset.Click += BtnReset_Click;
            card.Controls.Add(btnReset);
        }

        void BtnReset_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string newp = txtNewPass.Text;
            string conf = txtConfirm.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(newp))
            {
                lblStatus.ForeColor = Color.Crimson;
                lblStatus.Text = "Please fill in all fields.";
                return;
            }
            if (newp != conf)
            {
                lblStatus.ForeColor = Color.Crimson;
                lblStatus.Text = "Passwords do not match.";
                return;
            }
            if (newp.Length < 6)
            {
                lblStatus.ForeColor = Color.Crimson;
                lblStatus.Text = "Password must be at least 6 characters.";
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    // Check user exists first
                    string checkSql = "SELECT COUNT(*) FROM users WHERE Username=@u";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count == 0)
                        {
                            lblStatus.ForeColor = Color.Crimson;
                            lblStatus.Text = "Username not found.";
                            return;
                        }
                    }

                    string sql = "UPDATE users SET Password=@p WHERE Username=@u";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", newp);
                        cmd.Parameters.AddWithValue("@u", user);
                        cmd.ExecuteNonQuery();
                    }
                }

                lblStatus.ForeColor = Color.FromArgb(30, 150, 90);
                lblStatus.Text = "Password reset successfully!";
                txtNewPass.Clear();
                txtConfirm.Clear();
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Crimson;
                lblStatus.Text = "DB Error: " + ex.Message;
            }
        }

        void AddLabel(Panel p, string text, Point loc)
        {
            p.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = GrayText,
                AutoSize  = true,
                Location  = loc
            });
        }

        TextBox AddTextBox(Panel p, Point loc, Size sz, bool isPass = false)
        {
            var tb = new TextBox
            {
                Location              = loc,
                Size                  = sz,
                BorderStyle           = BorderStyle.FixedSingle,
                Font                  = new Font("Segoe UI", 9.5f),
                BackColor             = Color.FromArgb(249, 247, 255),
                UseSystemPasswordChar = isPass
            };
            p.Controls.Add(tb);
            return tb;
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
