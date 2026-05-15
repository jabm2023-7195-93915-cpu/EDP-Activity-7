using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    
    public partial class UserManagementForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleDark  = Color.FromArgb(58,  42, 130);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White       = Color.White;
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);
        static readonly Color Green       = Color.FromArgb(30,  150,  90);
        static readonly Color Red         = Color.FromArgb(180,  40,  40);

        DataGridView grid;
        TextBox txtSearch, txtUsername, txtPassword;
        ComboBox cboStatus;
        Button   btnAdd, btnUpdate, btnToggle, btnClear;
        Label    lblMsg;
        string   _selectedUser = null;

        public UserManagementForm()
        {
            Text            = "User Management";
            Size            = new Size(860, 640);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(245, 243, 255);
            Font            = new Font("Segoe UI", 9f);

            BuildUI();
            EnsureIsActiveColumn();
            LoadUsers();
        }

        void EnsureIsActiveColumn()
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    "ALTER TABLE users ADD COLUMN IF NOT EXISTS IsActive TINYINT(1) DEFAULT 1", conn))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }

        void BuildUI()
        {
            // ── Left panel: form ──────────────────────────────────
            var pnlForm = new Panel
            {
                Size      = new Size(270, 580),
                Location  = new Point(10, 10),
                BackColor = White
            };
            RoundCorners(pnlForm, 12);
            Controls.Add(pnlForm);

            AddLabel(pnlForm, "👥 Manage User", 15, true, Purple, new Point(15, 14));
            var div = new Panel { Size = new Size(240, 1), Location = new Point(15, 45), BackColor = PurpleLight };
            pnlForm.Controls.Add(div);

            AddLabel(pnlForm, "Username",     9,  false, GrayText, new Point(15, 58));
            txtUsername = AddTextBox(pnlForm, new Point(15, 76),  new Size(240, 32));
            
            AddLabel(pnlForm, "Password",     9,  false, GrayText, new Point(15, 122));
            txtPassword = AddTextBox(pnlForm, new Point(15, 140), new Size(240, 32), isPass: true);
           
            AddLabel(pnlForm, "Status",       9,  false, GrayText, new Point(15, 186));
            cboStatus = new ComboBox
            {
                Location        = new Point(15, 204),
                Size            = new Size(240, 32),
                DropDownStyle   = ComboBoxStyle.DropDownList,
                BackColor       = Color.FromArgb(249, 247, 255),
                Font            = new Font("Segoe UI", 9.5f)
            };
            cboStatus.Items.AddRange(new object[] { "Active", "Inactive" });
            cboStatus.SelectedIndex = 0;
            pnlForm.Controls.Add(cboStatus);

            lblMsg = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Green,
                AutoSize  = true,
                Location  = new Point(15, 248)
            };
            pnlForm.Controls.Add(lblMsg);

            btnAdd = MakeButton(pnlForm, "➕  Add User",     Purple, new Point(15, 270));
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = MakeButton(pnlForm, "✏️  Update",      Color.FromArgb(0, 120, 90), new Point(15, 318));
            btnUpdate.Click += BtnUpdate_Click;
            btnUpdate.Enabled = false;

            btnToggle = MakeButton(pnlForm, "🔄  Toggle Status", Color.FromArgb(150, 80, 0), new Point(15, 366));
            btnToggle.Click += BtnToggle_Click;
            btnToggle.Enabled = false;

            btnClear = MakeButton(pnlForm, "🗑️  Clear / New",   Color.FromArgb(100, 100, 120), new Point(15, 414));
            btnClear.Click += (s, e) => ClearForm();

            // ── Right panel: list + search ────────────────────────
            var pnlList = new Panel
            {
                Size      = new Size(555, 580),
                Location  = new Point(292, 10),
                BackColor = White
            };
            RoundCorners(pnlList, 12);
            Controls.Add(pnlList);

            AddLabel(pnlList, "Account List", 11, true, Purple, new Point(15, 14));

            // Search
            txtSearch = new TextBox
            {
                Location        = new Point(15, 44),
                Size            = new Size(400, 30),
                Font            = new Font("Segoe UI", 9.5f),
                BackColor       = Color.FromArgb(249, 247, 255),
                BorderStyle     = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadUsers(txtSearch.Text);
            pnlList.Controls.Add(txtSearch);

            var btnRefresh = new Button
            {
                Text      = "⟳ Refresh",
                Location  = new Point(425, 44),
                Size      = new Size(110, 30),
                BackColor = Purple,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadUsers();
            pnlList.Controls.Add(btnRefresh);

            // Grid
            grid = new DataGridView
            {
                Location          = new Point(15, 85),
                Size              = new Size(525, 475),
                BackgroundColor   = White,
                BorderStyle       = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly          = true,
                AllowUserToAddRows = false,
                SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
                Font              = new Font("Segoe UI", 9f),
                GridColor         = Color.FromArgb(225, 220, 240)
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Purple;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = White;
            grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor     = PurpleLight;
            grid.DefaultCellStyle.SelectionForeColor     = Color.FromArgb(40, 30, 80);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 247, 255);
            grid.EnableHeadersVisualStyles = false;
            grid.CellClick += Grid_CellClick;
            pnlList.Controls.Add(grid);

            grid.Columns.Add("Username", "Username");
            grid.Columns.Add("Status",   "Status");
        }

        void LoadUsers(string search = "")
        {
            grid.Rows.Clear();
            try
            {
                string sql = string.IsNullOrEmpty(search)
                    ? "SELECT Username, IFNULL(IsActive,1) AS IsActive FROM users ORDER BY Username"
                    : "SELECT Username, IFNULL(IsActive,1) AS IsActive FROM users WHERE Username LIKE @s ORDER BY Username";

                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@s", "%" + search + "%");

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int active = r.GetInt32(1);
                            int rowIdx = grid.Rows.Add(r["Username"], active == 1 ? "Active" : "Inactive");

                            // Color code the status cell
                            grid.Rows[rowIdx].Cells["Status"].Style.ForeColor =
                                active == 1 ? Green : Red;
                            grid.Rows[rowIdx].Cells["Status"].Style.Font =
                                new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = Red;
                lblMsg.Text = "Load error: " + ex.Message;
            }
        }

        void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            _selectedUser = row.Cells["Username"].Value?.ToString();
            txtUsername.Text    = _selectedUser;
            txtPassword.Text    = "";   // never pre-fill password
            cboStatus.SelectedItem = row.Cells["Status"].Value?.ToString();
            btnUpdate.Enabled  = true;
            btnToggle.Enabled  = true;
            btnAdd.Enabled     = false;

            lblMsg.ForeColor = Purple;
            lblMsg.Text = $"Selected: {_selectedUser}";
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowMsg("Username and password are required.", Red);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    // Check duplicate
                    using (var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Username=@u", conn))
                    {
                        check.Parameters.AddWithValue("@u", user);
                        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        {
                            ShowMsg("Username already exists.", Red);
                            return;
                        }
                    }

                    int active = cboStatus.SelectedItem.ToString() == "Active" ? 1 : 0;
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO users (Username, Password, IsActive) VALUES (@u, @p, @a)", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user);
                        cmd.Parameters.AddWithValue("@p", pass);
                        cmd.Parameters.AddWithValue("@a", active);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowMsg("User added successfully!", Green);
                ClearForm();
                LoadUsers();
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedUser)) return;

            string newUser = txtUsername.Text.Trim();
            string newPass = txtPassword.Text;
            int    active  = cboStatus.SelectedItem.ToString() == "Active" ? 1 : 0;

            if (string.IsNullOrEmpty(newUser))
            {
                ShowMsg("Username cannot be empty.", Red);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    string sql = string.IsNullOrEmpty(newPass)
                        ? "UPDATE users SET Username=@newu, IsActive=@a WHERE Username=@oldu"
                        : "UPDATE users SET Username=@newu, Password=@p, IsActive=@a WHERE Username=@oldu";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@newu", newUser);
                        cmd.Parameters.AddWithValue("@oldu", _selectedUser);
                        cmd.Parameters.AddWithValue("@a",    active);
                        if (!string.IsNullOrEmpty(newPass))
                            cmd.Parameters.AddWithValue("@p", newPass);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowMsg("Updated successfully!", Green);
                ClearForm();
                LoadUsers();
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void BtnToggle_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedUser)) return;

            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    "UPDATE users SET IsActive = 1 - IFNULL(IsActive,1) WHERE Username=@u", conn))
                {
                    cmd.Parameters.AddWithValue("@u", _selectedUser);
                    cmd.ExecuteNonQuery();
                }
                ShowMsg("Status toggled!", Green);
                ClearForm();
                LoadUsers();
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void ClearForm()
        {
            _selectedUser        = null;
            txtUsername.Text     = "";
            txtPassword.Text     = "";
            cboStatus.SelectedIndex = 0;
            btnAdd.Enabled       = true;
            btnUpdate.Enabled    = false;
            btnToggle.Enabled    = false;
            lblMsg.Text          = "";
        }

        void ShowMsg(string text, Color color)
        {
            lblMsg.ForeColor = color;
            lblMsg.Text      = text;
        }

        // ── Helpers ───────────────────────────────────────────────
        void AddLabel(Panel p, string text, float size, bool bold, Color color, Point loc)
        {
            p.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
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

        Button MakeButton(Panel p, string text, Color bg, Point loc)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(240, 38),
                Location  = loc,
                BackColor = bg,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            RoundCorners(btn, 8);
            p.Controls.Add(btn);
            return btn;
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
