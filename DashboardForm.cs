using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    public partial class DashboardForm : Form
    {
        static readonly Color Purple = Color.FromArgb(88, 66, 190);
        static readonly Color PurpleDark = Color.FromArgb(58, 42, 130);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color Sidebar = Color.FromArgb(58, 42, 130);
        static readonly Color White = Color.White;
        static readonly Color GrayText = Color.FromArgb(110, 105, 140);

        string _loggedInUser;
        Panel pnlSidebar, pnlContent;
        Label lblPageTitle;

        public DashboardForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI();
            ShowDashboardPage();
        }

        void BuildUI()
        {
            Text = "BrewTrack — Dashboard";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 243, 255);

            pnlSidebar = new Panel { Size = new Size(220, 700), Location = new Point(0, 0), BackColor = Sidebar };
            Controls.Add(pnlSidebar);

            pnlSidebar.Controls.Add(new Label
            {
                Text = "☕ BrewTrack",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = White,
                AutoSize = true,
                Location = new Point(20, 22)
            });
            pnlSidebar.Controls.Add(new Label
            {
                Text = "👤 " + _loggedInUser,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 190, 230),
                AutoSize = true,
                Location = new Point(20, 52)
            });
            pnlSidebar.Controls.Add(new Panel
            {
                Size = new Size(180, 1),
                Location = new Point(20, 74),
                BackColor = Color.FromArgb(100, 80, 160)
            });

            int y = 86;
            NavBtn("📊  Dashboard", ref y, () => ShowDashboardPage());
            NavBtn("📦  Inventory", ref y, () => ShowInventoryPage());

            pnlSidebar.Controls.Add(new Label
            {
                Text = "  TRANSACTIONS",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 140, 190),
                AutoSize = true,
                Location = new Point(20, y)
            });
            y += 20;

            NavBtn("🛒  Purchase Orders", ref y, () => new PurchaseOrderForm(_loggedInUser).ShowDialog());
            NavBtn("💰  Sales", ref y, () => new SalesForm(_loggedInUser).ShowDialog());
            NavBtn("📥  Stock In", ref y, () => new StockInForm(_loggedInUser).ShowDialog());
            NavBtn("📤  Stock Out", ref y, () => new StockOutForm(_loggedInUser).ShowDialog());

            pnlSidebar.Controls.Add(new Label
            {
                Text = "  MANAGEMENT",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 140, 190),
                AutoSize = true,
                Location = new Point(20, y)
            });
            y += 20;

            NavBtn("👥  User Management", ref y, () => new UserManagementForm().ShowDialog());
            NavBtn("📊  Reports", ref y, () => new ReportForm(_loggedInUser).ShowDialog());
            NavBtn("ℹ️   About", ref y, () => new AboutForm().ShowDialog());

            var btnLogout = new Button
            {
                Text = "🚪  Logout",
                Size = new Size(180, 40),
                Location = new Point(20, 618),
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => {
                if (MessageBox.Show("Logout?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { new LoginForm().Show(); Close(); }
            };
            pnlSidebar.Controls.Add(btnLogout);

            pnlContent = new Panel
            {
                Size = new Size(880, 700),
                Location = new Point(220, 0),
                BackColor = Color.FromArgb(245, 243, 255)
            };
            Controls.Add(pnlContent);

            var topBar = new Panel { Size = new Size(880, 60), Location = new Point(0, 0), BackColor = White };
            pnlContent.Controls.Add(topBar);

            lblPageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize = true,
                Location = new Point(24, 16)
            };
            topBar.Controls.Add(lblPageTitle);
        }

        void NavBtn(string text, ref int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(200, 40),
                Location = new Point(10, y),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(220, 215, 245),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 255, 255, 255);
            btn.Click += (s, e) => onClick();
            pnlSidebar.Controls.Add(btn);
            y += 44;
        }

        void ClearContent()
        {
            for (int i = pnlContent.Controls.Count - 1; i >= 0; i--)
                if (pnlContent.Controls[i].Location.Y >= 60)
                    pnlContent.Controls.RemoveAt(i);
        }

        void ShowDashboardPage()
        {
            ClearContent();
            lblPageTitle.Text = "Dashboard";

            int totalItems = 0, totalQty = 0, totalSales = 0;
            decimal totalValue = 0, totalRevenue = 0;

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*), SUM(Quantity), SUM(Quantity*Price) FROM inventory", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            totalItems = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                            totalQty = r.IsDBNull(1) ? 0 : r.GetInt32(1);
                            totalValue = r.IsDBNull(2) ? 0 : r.GetDecimal(2);
                        }
                    }
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*), IFNULL(SUM(TotalAmount),0) FROM sales", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            totalSales = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                            totalRevenue = r.IsDBNull(1) ? 0 : r.GetDecimal(1);
                        }
                    }
                }
            }
            catch { }

            StatCard("📦", "Inventory Items", totalItems.ToString(), Color.FromArgb(88, 66, 190), new Point(24, 80));
            StatCard("🔢", "Total Stock", totalQty.ToString(), Color.FromArgb(0, 130, 100), new Point(204, 80));
            StatCard("💰", "Inventory Value", "P " + totalValue.ToString("N0"), Color.FromArgb(180, 80, 20), new Point(384, 80));
            StatCard("🛒", "Total Sales", totalSales.ToString(), Color.FromArgb(30, 90, 170), new Point(564, 80));
            StatCard("💵", "Total Revenue", "P " + totalRevenue.ToString("N0"), Color.FromArgb(120, 40, 140), new Point(744, 80));

            pnlContent.Controls.Add(new Label
            {
                Text = "Inventory Snapshot",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize = true,
                Location = new Point(24, 200)
            });

            var grid = MakeGrid(new Point(24, 228), new Size(832, 430));
            grid.Columns.Add("ItemID", "Item ID");
            grid.Columns.Add("ItemName", "Item Name");
            grid.Columns.Add("Category", "Category");
            grid.Columns.Add("Quantity", "Stock");
            grid.Columns.Add("Price", "Price (P)");
            LoadInventoryGrid(grid);
            pnlContent.Controls.Add(grid);
        }

        void StatCard(string icon, string label, string value, Color accent, Point loc)
        {
            var card = new Panel { Size = new Size(162, 100), Location = loc, BackColor = White };
            RoundCorners(card, 10); pnlContent.Controls.Add(card);
            card.Controls.Add(new Panel { Size = new Size(5, 100), Location = new Point(0, 0), BackColor = accent });
            card.Controls.Add(new Label
            {
                Text = icon + "  " + label,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = GrayText,
                AutoSize = true,
                Location = new Point(12, 16)
            });
            card.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = false,
                Size = new Size(145, 32),
                Location = new Point(12, 42)
            });
        }

        void ShowInventoryPage()
        {
            ClearContent();
            lblPageTitle.Text = "Inventory";
            pnlContent.Controls.Add(new Label
            {
                Text = "All Inventory Items",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize = true,
                Location = new Point(24, 75)
            });

            var grid = MakeGrid(new Point(24, 105), new Size(832, 555));
            grid.Columns.Add("ItemID", "Item ID");
            grid.Columns.Add("ItemName", "Item Name");
            grid.Columns.Add("Category", "Category");
            grid.Columns.Add("Unit", "Unit");
            grid.Columns.Add("Quantity", "Stock");
            grid.Columns.Add("Price", "Price (P)");
            LoadInventoryGrid(grid, true);
            pnlContent.Controls.Add(grid);
        }

        void LoadInventoryGrid(DataGridView grid, bool includeUnit = false)
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand("SELECT * FROM inventory", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        if (includeUnit)
                            grid.Rows.Add(r["ItemID"], r["ItemName"], r["Category"], r["Unit"], r["Quantity"], r["Price"]);
                        else
                            grid.Rows.Add(r["ItemID"], r["ItemName"], r["Category"], r["Quantity"], r["Price"]);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        DataGridView MakeGrid(Point loc, Size sz)
        {
            var g = new DataGridView
            {
                Location = loc,
                Size = sz,
                BackgroundColor = White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(225, 220, 240),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Purple; g.ColumnHeadersDefaultCellStyle.ForeColor = White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.DefaultCellStyle.SelectionBackColor = PurpleLight; g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 30, 80);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 247, 255);
            g.EnableHeadersVisualStyles = false; return g;
        }

        void RoundCorners(Control ctrl, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, r * 2, r * 2, 180, 90); path.AddArc(ctrl.Width - r * 2, 0, r * 2, r * 2, 270, 90);
            path.AddArc(ctrl.Width - r * 2, ctrl.Height - r * 2, r * 2, r * 2, 0, 90); path.AddArc(0, ctrl.Height - r * 2, r * 2, r * 2, 90, 90);
            path.CloseAllFigures(); ctrl.Region = new Region(path);
        }
    }
}