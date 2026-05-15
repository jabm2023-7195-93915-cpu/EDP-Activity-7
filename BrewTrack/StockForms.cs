using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    // ================================================================
    //  Stock In Form
    // ================================================================
    public partial class StockInForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White       = Color.White;
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);
        static readonly Color Green       = Color.FromArgb(30,  150,  90);
        static readonly Color Red         = Color.FromArgb(180,  40,  40);

        ComboBox cboItem;
        TextBox  txtQuantity, txtSupplier, txtNotes;
        Label    lblMsg;
        DataGridView grid;
        string _loggedInUser;

        public StockInForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI(); LoadGrid(); LoadItems();
        }

        void BuildUI()
        {
            Text = "Stock In"; Size = new Size(950, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = Color.FromArgb(245, 243, 255);
            Font = new Font("Segoe UI", 9f);

            var pnlForm = new Panel { Size = new Size(280, 560), Location = new Point(10, 10), BackColor = White };
            RoundCorners(pnlForm, 12); Controls.Add(pnlForm);

            AddLbl(pnlForm, "📥 Stock In", 13, true, Purple, new Point(15, 14));
            Div(pnlForm, 45);

            AddLbl(pnlForm, "Item", 9, false, GrayText, new Point(15, 55));
            cboItem = new ComboBox { Location = new Point(15, 73), Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(249, 247, 255) };
            pnlForm.Controls.Add(cboItem);

            AddLbl(pnlForm, "Quantity", 9, false, GrayText, new Point(15, 112));
            txtQuantity = AddTxt(pnlForm, new Point(15, 130), new Size(250, 30));

            AddLbl(pnlForm, "Supplier (optional)", 9, false, GrayText, new Point(15, 170));
            txtSupplier = AddTxt(pnlForm, new Point(15, 188), new Size(250, 30));

            AddLbl(pnlForm, "Notes (optional)", 9, false, GrayText, new Point(15, 228));
            txtNotes = AddTxt(pnlForm, new Point(15, 246), new Size(250, 55));
            txtNotes.Multiline = true;

            lblMsg = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = Green,
                AutoSize = true, Location = new Point(15, 312) };
            pnlForm.Controls.Add(lblMsg);

            var btnSave = MakeBtn(pnlForm, "💾  Record Stock In", Purple, new Point(15, 334));
            btnSave.Click += BtnSave_Click;
            var btnClear = MakeBtn(pnlForm, "🗑️  Clear", Color.FromArgb(100, 100, 120), new Point(15, 382));
            btnClear.Click += (s, e) => ClearForm();

            var pnlGrid = new Panel { Size = new Size(635, 560), Location = new Point(305, 10), BackColor = White };
            RoundCorners(pnlGrid, 12); Controls.Add(pnlGrid);
            AddLbl(pnlGrid, "Stock In History", 11, true, Purple, new Point(15, 14));
            Div(pnlGrid, 40);

            grid = MakeGrid(new Point(15, 50), new Size(605, 490));
            grid.Columns.Add("ID",       "#");
            grid.Columns.Add("Item",     "Item");
            grid.Columns.Add("Qty",      "Qty");
            grid.Columns.Add("Supplier", "Supplier");
            grid.Columns.Add("Notes",    "Notes");
            grid.Columns.Add("Date",     "Date");
            grid.Columns.Add("By",       "Recorded By");
            pnlGrid.Controls.Add(grid);
        }

        void LoadItems()
        {
            cboItem.Items.Clear();
            try {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand("SELECT ItemID, ItemName FROM inventory ORDER BY ItemName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        cboItem.Items.Add(new ItemEntry(r["ItemID"].ToString(), r["ItemName"].ToString(), 0));
            } catch { }
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
        }

        void LoadGrid()
        {
            grid.Rows.Clear();
            try {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    @"SELECT s.StockInID, i.ItemName, s.Quantity, s.Supplier,
                             s.Notes, s.DateIn, s.RecordedBy
                      FROM stock_in s JOIN inventory i ON i.ItemID=s.ItemID
                      ORDER BY s.DateIn DESC LIMIT 100", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        grid.Rows.Add(r["StockInID"], r["ItemName"], r["Quantity"],
                            r["Supplier"], r["Notes"],
                            Convert.ToDateTime(r["DateIn"]).ToString("yyyy-MM-dd HH:mm"),
                            r["RecordedBy"]);
            } catch (Exception ex) { ShowMsg("Load error: " + ex.Message, Red); }
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is ItemEntry item)) { ShowMsg("Select an item.", Red); return; }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0) { ShowMsg("Enter a valid quantity.", Red); return; }

            try {
                using (var conn = DBConnection.GetConnection())
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO stock_in (ItemID,Quantity,Supplier,Notes,RecordedBy) VALUES (@i,@q,@s,@n,@r)", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", item.ID);
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@s", txtSupplier.Text.Trim());
                        cmd.Parameters.AddWithValue("@n", txtNotes.Text.Trim());
                        cmd.Parameters.AddWithValue("@r", _loggedInUser);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new MySqlCommand(
                        "UPDATE inventory SET Quantity = Quantity + @q WHERE ItemID = @i", conn))
                    {
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@i", item.ID);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowMsg("Stock In recorded!", Green);
                ClearForm(); LoadGrid();
            } catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void ClearForm() {
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
            txtQuantity.Text = txtSupplier.Text = txtNotes.Text = "";
            lblMsg.Text = "";
        }
        void ShowMsg(string t, Color c) { lblMsg.ForeColor = c; lblMsg.Text = t; }
        void AddLbl(Panel p, string t, float sz, bool bold, Color c, Point loc) =>
            p.Controls.Add(new Label { Text=t, Font=new Font("Segoe UI",sz,bold?FontStyle.Bold:FontStyle.Regular), ForeColor=c, AutoSize=true, Location=loc });
        void Div(Panel p, int y) =>
            p.Controls.Add(new Panel { Size=new Size(p.Width-30,1), Location=new Point(15,y), BackColor=PurpleLight });
        TextBox AddTxt(Panel p, Point loc, Size sz) {
            var tb = new TextBox { Location=loc, Size=sz, BorderStyle=BorderStyle.FixedSingle,
                Font=new Font("Segoe UI",9.5f), BackColor=Color.FromArgb(249,247,255) };
            p.Controls.Add(tb); return tb; }
        Button MakeBtn(Panel p, string t, Color bg, Point loc) {
            var b = new Button { Text=t, Size=new Size(250,38), Location=loc, BackColor=bg,
                ForeColor=White, FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",9f,FontStyle.Bold), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize=0; RoundCorners(b,8); p.Controls.Add(b); return b; }
        DataGridView MakeGrid(Point loc, Size sz) {
            var g = new DataGridView { Location=loc, Size=sz, BackgroundColor=White, BorderStyle=BorderStyle.None,
                RowHeadersVisible=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly=true, AllowUserToAddRows=false, Font=new Font("Segoe UI",9f),
                GridColor=Color.FromArgb(225,220,240), SelectionMode=DataGridViewSelectionMode.FullRowSelect };
            g.ColumnHeadersDefaultCellStyle.BackColor=Purple; g.ColumnHeadersDefaultCellStyle.ForeColor=White;
            g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            g.DefaultCellStyle.SelectionBackColor=PurpleLight; g.DefaultCellStyle.SelectionForeColor=Color.FromArgb(40,30,80);
            g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(249,247,255);
            g.EnableHeadersVisualStyles=false; return g; }
        void RoundCorners(Control ctrl, int r) {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0,0,r*2,r*2,180,90); path.AddArc(ctrl.Width-r*2,0,r*2,r*2,270,90);
            path.AddArc(ctrl.Width-r*2,ctrl.Height-r*2,r*2,r*2,0,90); path.AddArc(0,ctrl.Height-r*2,r*2,r*2,90,90);
            path.CloseAllFigures(); ctrl.Region=new Region(path); }
    }

    // ================================================================
    //  Stock Out Form
    // ================================================================
    public partial class StockOutForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White       = Color.White;
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);
        static readonly Color Green       = Color.FromArgb(30,  150,  90);
        static readonly Color Red         = Color.FromArgb(180,  40,  40);

        ComboBox cboItem;
        TextBox  txtQuantity, txtReason;
        Label    lblMsg, lblStock;
        DataGridView grid;
        string _loggedInUser;

        public StockOutForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI(); LoadGrid(); LoadItems();
        }

        void BuildUI()
        {
            Text = "Stock Out"; Size = new Size(950, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = Color.FromArgb(245, 243, 255);
            Font = new Font("Segoe UI", 9f);

            var pnlForm = new Panel { Size = new Size(280, 560), Location = new Point(10, 10), BackColor = White };
            RoundCorners(pnlForm, 12); Controls.Add(pnlForm);

            AddLbl(pnlForm, "📤 Stock Out", 13, true, Purple, new Point(15, 14));
            Div(pnlForm, 45);

            AddLbl(pnlForm, "Item", 9, false, GrayText, new Point(15, 55));
            cboItem = new ComboBox { Location = new Point(15, 73), Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(249, 247, 255) };
            cboItem.SelectedIndexChanged += (s, e) => OnItemSelected();
            pnlForm.Controls.Add(cboItem);

            lblStock = new Label { Text = "Stock: —", Font = new Font("Segoe UI", 8f),
                ForeColor = GrayText, AutoSize = true, Location = new Point(15, 104) };
            pnlForm.Controls.Add(lblStock);

            AddLbl(pnlForm, "Quantity", 9, false, GrayText, new Point(15, 122));
            txtQuantity = AddTxt(pnlForm, new Point(15, 140), new Size(250, 30));

            AddLbl(pnlForm, "Reason", 9, false, GrayText, new Point(15, 180));
            txtReason = AddTxt(pnlForm, new Point(15, 198), new Size(250, 55));
            txtReason.Multiline = true;

            lblMsg = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = Green,
                AutoSize = true, Location = new Point(15, 264) };
            pnlForm.Controls.Add(lblMsg);

            var btnSave = MakeBtn(pnlForm, "💾  Record Stock Out", Purple, new Point(15, 286));
            btnSave.Click += BtnSave_Click;
            var btnClear = MakeBtn(pnlForm, "🗑️  Clear", Color.FromArgb(100, 100, 120), new Point(15, 334));
            btnClear.Click += (s, e) => ClearForm();

            var pnlGrid = new Panel { Size = new Size(635, 560), Location = new Point(305, 10), BackColor = White };
            RoundCorners(pnlGrid, 12); Controls.Add(pnlGrid);
            AddLbl(pnlGrid, "Stock Out History", 11, true, Purple, new Point(15, 14));
            Div(pnlGrid, 40);

            grid = MakeGrid(new Point(15, 50), new Size(605, 490));
            grid.Columns.Add("ID",     "#");
            grid.Columns.Add("Item",   "Item");
            grid.Columns.Add("Qty",    "Qty");
            grid.Columns.Add("Reason", "Reason");
            grid.Columns.Add("Date",   "Date");
            grid.Columns.Add("By",     "Recorded By");
            pnlGrid.Controls.Add(grid);
        }

        void LoadItems()
        {
            cboItem.Items.Clear();
            try {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand("SELECT ItemID, ItemName, Quantity FROM inventory ORDER BY ItemName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        cboItem.Items.Add(new SaleItemEntry(r["ItemID"].ToString(), r["ItemName"].ToString(),
                            0, Convert.ToInt32(r["Quantity"])));
            } catch { }
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
        }

        void OnItemSelected()
        {
            if (cboItem.SelectedItem is SaleItemEntry item)
            {
                lblStock.Text = $"Stock: {item.Stock}{(item.Stock < 5 ? " ⚠️ Low" : "")}";
                lblStock.ForeColor = item.Stock < 5 ? Red : Green;
            }
        }

        void LoadGrid()
        {
            grid.Rows.Clear();
            try {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    @"SELECT s.StockOutID, i.ItemName, s.Quantity, s.Reason,
                             s.DateOut, s.RecordedBy
                      FROM stock_out s JOIN inventory i ON i.ItemID=s.ItemID
                      ORDER BY s.DateOut DESC LIMIT 100", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        grid.Rows.Add(r["StockOutID"], r["ItemName"], r["Quantity"],
                            r["Reason"],
                            Convert.ToDateTime(r["DateOut"]).ToString("yyyy-MM-dd HH:mm"),
                            r["RecordedBy"]);
            } catch (Exception ex) { ShowMsg("Load error: " + ex.Message, Red); }
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is SaleItemEntry item)) { ShowMsg("Select an item.", Red); return; }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0) { ShowMsg("Enter a valid quantity.", Red); return; }
            if (qty > item.Stock) { ShowMsg($"Not enough stock! Available: {item.Stock}", Red); return; }

            try {
                using (var conn = DBConnection.GetConnection())
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO stock_out (ItemID,Quantity,Reason,RecordedBy) VALUES (@i,@q,@reason,@r)", conn))
                    {
                        cmd.Parameters.AddWithValue("@i",      item.ID);
                        cmd.Parameters.AddWithValue("@q",      qty);
                        cmd.Parameters.AddWithValue("@reason", txtReason.Text.Trim());
                        cmd.Parameters.AddWithValue("@r",      _loggedInUser);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new MySqlCommand(
                        "UPDATE inventory SET Quantity = Quantity - @q WHERE ItemID = @i", conn))
                    {
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@i", item.ID);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowMsg("Stock Out recorded!", Green);
                ClearForm(); LoadGrid(); LoadItems();
            } catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void ClearForm() {
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
            txtQuantity.Text = txtReason.Text = ""; lblMsg.Text = "";
        }
        void ShowMsg(string t, Color c) { lblMsg.ForeColor = c; lblMsg.Text = t; }
        void AddLbl(Panel p, string t, float sz, bool bold, Color c, Point loc) =>
            p.Controls.Add(new Label { Text=t, Font=new Font("Segoe UI",sz,bold?FontStyle.Bold:FontStyle.Regular), ForeColor=c, AutoSize=true, Location=loc });
        void Div(Panel p, int y) =>
            p.Controls.Add(new Panel { Size=new Size(p.Width-30,1), Location=new Point(15,y), BackColor=PurpleLight });
        TextBox AddTxt(Panel p, Point loc, Size sz) {
            var tb = new TextBox { Location=loc, Size=sz, BorderStyle=BorderStyle.FixedSingle,
                Font=new Font("Segoe UI",9.5f), BackColor=Color.FromArgb(249,247,255) };
            p.Controls.Add(tb); return tb; }
        Button MakeBtn(Panel p, string t, Color bg, Point loc) {
            var b = new Button { Text=t, Size=new Size(250,38), Location=loc, BackColor=bg,
                ForeColor=White, FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",9f,FontStyle.Bold), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize=0; RoundCorners(b,8); p.Controls.Add(b); return b; }
        DataGridView MakeGrid(Point loc, Size sz) {
            var g = new DataGridView { Location=loc, Size=sz, BackgroundColor=White, BorderStyle=BorderStyle.None,
                RowHeadersVisible=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly=true, AllowUserToAddRows=false, Font=new Font("Segoe UI",9f),
                GridColor=Color.FromArgb(225,220,240), SelectionMode=DataGridViewSelectionMode.FullRowSelect };
            g.ColumnHeadersDefaultCellStyle.BackColor=Purple; g.ColumnHeadersDefaultCellStyle.ForeColor=White;
            g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            g.DefaultCellStyle.SelectionBackColor=PurpleLight; g.DefaultCellStyle.SelectionForeColor=Color.FromArgb(40,30,80);
            g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(249,247,255);
            g.EnableHeadersVisualStyles=false; return g; }
        void RoundCorners(Control ctrl, int r) {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0,0,r*2,r*2,180,90); path.AddArc(ctrl.Width-r*2,0,r*2,r*2,270,90);
            path.AddArc(ctrl.Width-r*2,ctrl.Height-r*2,r*2,r*2,0,90); path.AddArc(0,ctrl.Height-r*2,r*2,r*2,90,90);
            path.CloseAllFigures(); ctrl.Region=new Region(path); }
    }
}
