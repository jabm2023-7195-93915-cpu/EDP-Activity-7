using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    public partial class SalesForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleDark  = Color.FromArgb(58,  42, 130);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White       = Color.White;
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);
        static readonly Color Green       = Color.FromArgb(30,  150,  90);
        static readonly Color Red         = Color.FromArgb(180,  40,  40);

        ComboBox cboItem;
        TextBox  txtQuantity, txtUnitPrice;
        Label    lblTotal, lblStock, lblMsg;
        DataGridView grid;
        string _loggedInUser;

        public SalesForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI();
            LoadGrid();
            LoadItems();
        }

        void BuildUI()
        {
            Text = "Sales Transaction"; Size = new Size(950, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = Color.FromArgb(245, 243, 255);
            Font = new Font("Segoe UI", 9f);

            var pnlForm = new Panel { Size = new Size(280, 560), Location = new Point(10, 10), BackColor = White };
            RoundCorners(pnlForm, 12); Controls.Add(pnlForm);

            AddLbl(pnlForm, "💰 New Sale", 13, true, Purple, new Point(15, 14));
            Divider(pnlForm, 45);

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
            txtQuantity.TextChanged += (s, e) => RecalcTotal();

            AddLbl(pnlForm, "Unit Price (₱)", 9, false, GrayText, new Point(15, 180));
            txtUnitPrice = AddTxt(pnlForm, new Point(15, 198), new Size(250, 30));
            txtUnitPrice.TextChanged += (s, e) => RecalcTotal();

            AddLbl(pnlForm, "Total Amount:", 9, true, Purple, new Point(15, 238));
            lblTotal = new Label { Text = "₱ 0.00", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Purple, AutoSize = true, Location = new Point(15, 254) };
            pnlForm.Controls.Add(lblTotal);

            lblMsg = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = Green,
                AutoSize = true, Location = new Point(15, 294) };
            pnlForm.Controls.Add(lblMsg);

            var btnSave = MakeBtn(pnlForm, "💾  Record Sale", Purple, new Point(15, 316));
            btnSave.Click += BtnSave_Click;
            var btnClear = MakeBtn(pnlForm, "🗑️  Clear", Color.FromArgb(100, 100, 120), new Point(15, 364));
            btnClear.Click += (s, e) => ClearForm();

            var pnlGrid = new Panel { Size = new Size(635, 560), Location = new Point(305, 10), BackColor = White };
            RoundCorners(pnlGrid, 12); Controls.Add(pnlGrid);
            AddLbl(pnlGrid, "Sales History", 11, true, Purple, new Point(15, 14));
            Divider(pnlGrid, 40);

            grid = MakeGrid(new Point(15, 50), new Size(605, 490));
            grid.Columns.Add("SaleID",   "Sale #");
            grid.Columns.Add("Item",     "Item");
            grid.Columns.Add("Qty",      "Qty");
            grid.Columns.Add("Price",    "Unit Price");
            grid.Columns.Add("Total",    "Total");
            grid.Columns.Add("Date",     "Date");
            grid.Columns.Add("By",       "Recorded By");
            pnlGrid.Controls.Add(grid);
        }

        void LoadItems()
        {
            cboItem.Items.Clear();
            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand("SELECT ItemID, ItemName, Price, Quantity FROM inventory ORDER BY ItemName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        cboItem.Items.Add(new SaleItemEntry(r["ItemID"].ToString(), r["ItemName"].ToString(),
                            Convert.ToDecimal(r["Price"]), Convert.ToInt32(r["Quantity"])));
            }
            catch { }
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
        }

        void OnItemSelected()
        {
            if (cboItem.SelectedItem is SaleItemEntry item)
            {
                txtUnitPrice.Text = item.Price.ToString("F2");
                lblStock.Text = $"Stock: {item.Stock} {(item.Stock < 5 ? "⚠️ Low" : "")}";
                lblStock.ForeColor = item.Stock < 5 ? Red : Green;
            }
            RecalcTotal();
        }

        void RecalcTotal()
        {
            if (int.TryParse(txtQuantity.Text, out int qty) &&
                decimal.TryParse(txtUnitPrice.Text, out decimal price))
                lblTotal.Text = "₱ " + (qty * price).ToString("N2");
            else
                lblTotal.Text = "₱ 0.00";
        }

        void LoadGrid()
        {
            grid.Rows.Clear();
            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    @"SELECT s.SaleID, i.ItemName, s.Quantity, s.UnitPrice,
                             s.TotalAmount, s.SaleDate, s.RecordedBy
                      FROM sales s JOIN inventory i ON i.ItemID=s.ItemID
                      ORDER BY s.SaleDate DESC LIMIT 100", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        grid.Rows.Add(r["SaleID"], r["ItemName"], r["Quantity"],
                            "₱"+Convert.ToDecimal(r["UnitPrice"]).ToString("N2"),
                            "₱"+Convert.ToDecimal(r["TotalAmount"]).ToString("N2"),
                            Convert.ToDateTime(r["SaleDate"]).ToString("yyyy-MM-dd HH:mm"),
                            r["RecordedBy"]);
            }
            catch (Exception ex) { ShowMsg("Load error: " + ex.Message, Red); }
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is SaleItemEntry item)) { ShowMsg("Select an item.", Red); return; }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0) { ShowMsg("Enter a valid quantity.", Red); return; }
            if (!decimal.TryParse(txtUnitPrice.Text, out decimal price) || price <= 0) { ShowMsg("Enter a valid price.", Red); return; }
            if (qty > item.Stock) { ShowMsg($"Not enough stock! Available: {item.Stock}", Red); return; }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    // Insert sale record
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO sales (ItemID,Quantity,UnitPrice,RecordedBy) VALUES (@i,@q,@p,@r)", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", item.ID);
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@p", price);
                        cmd.Parameters.AddWithValue("@r", _loggedInUser);
                        cmd.ExecuteNonQuery();
                    }
                    // Deduct from inventory
                    using (var cmd = new MySqlCommand(
                        "UPDATE inventory SET Quantity = Quantity - @q WHERE ItemID = @i", conn))
                    {
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.Parameters.AddWithValue("@i", item.ID);
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowMsg("Sale recorded!", Green);
                ClearForm();
                LoadGrid();
                LoadItems(); // refresh stock levels
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void ClearForm()
        {
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
            txtQuantity.Text = txtUnitPrice.Text = "";
            lblTotal.Text = "₱ 0.00"; lblMsg.Text = "";
        }

        void ShowMsg(string t, Color c) { lblMsg.ForeColor = c; lblMsg.Text = t; }

        void AddLbl(Panel p, string t, float sz, bool bold, Color c, Point loc) =>
            p.Controls.Add(new Label { Text=t, Font=new Font("Segoe UI",sz,bold?FontStyle.Bold:FontStyle.Regular), ForeColor=c, AutoSize=true, Location=loc });
        void Divider(Panel p, int y) =>
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

    public class SaleItemEntry
    {
        public string ID, Name; public decimal Price; public int Stock;
        public SaleItemEntry(string id, string name, decimal price, int stock) { ID=id; Name=name; Price=price; Stock=stock; }
        public override string ToString() => Name;
    }
}
