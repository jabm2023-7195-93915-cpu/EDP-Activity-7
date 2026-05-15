using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BrewTrack
{
    public partial class PurchaseOrderForm : Form
    {
        static readonly Color Purple      = Color.FromArgb(88,  66, 190);
        static readonly Color PurpleDark  = Color.FromArgb(58,  42, 130);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White       = Color.White;
        static readonly Color GrayText    = Color.FromArgb(110, 105, 140);
        static readonly Color Green       = Color.FromArgb(30,  150,  90);
        static readonly Color Red         = Color.FromArgb(180,  40,  40);

        ComboBox cboItem;
        TextBox  txtSupplier, txtQuantity, txtUnitCost;
        Label    lblTotal, lblMsg;
        DataGridView grid;
        string _loggedInUser;

        public PurchaseOrderForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI();
            LoadGrid();
            LoadItems();
        }

        void BuildUI()
        {
            Text            = "Purchase Orders";
            Size            = new Size(950, 640);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(245, 243, 255);
            Font            = new Font("Segoe UI", 9f);

            // ── Form panel ────────────────────────────────────────
            var pnlForm = new Panel { Size = new Size(280, 560), Location = new Point(10, 10), BackColor = White };
            RoundCorners(pnlForm, 12); Controls.Add(pnlForm);

            AddLbl(pnlForm, "🛒 New Purchase Order", 13, true, Purple, new Point(15, 14));
            Divider(pnlForm, 45);

            AddLbl(pnlForm, "Item",     9, false, GrayText, new Point(15, 55));
            cboItem = new ComboBox { Location = new Point(15, 73), Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(249, 247, 255) };
            cboItem.SelectedIndexChanged += (s, e) => UpdateUnitCost();
            pnlForm.Controls.Add(cboItem);

            AddLbl(pnlForm, "Supplier", 9, false, GrayText, new Point(15, 110));
            txtSupplier = AddTxt(pnlForm, new Point(15, 128), new Size(250, 30));

            AddLbl(pnlForm, "Quantity", 9, false, GrayText, new Point(15, 168));
            txtQuantity = AddTxt(pnlForm, new Point(15, 186), new Size(250, 30));
            txtQuantity.TextChanged += (s, e) => RecalcTotal();

            AddLbl(pnlForm, "Unit Cost (₱)", 9, false, GrayText, new Point(15, 226));
            txtUnitCost = AddTxt(pnlForm, new Point(15, 244), new Size(250, 30));
            txtUnitCost.TextChanged += (s, e) => RecalcTotal();

            AddLbl(pnlForm, "Total Cost:", 9, true, Purple, new Point(15, 284));
            lblTotal = new Label { Text = "₱ 0.00", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Purple, AutoSize = true, Location = new Point(15, 300) };
            pnlForm.Controls.Add(lblTotal);

            lblMsg = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = Green,
                AutoSize = true, Location = new Point(15, 340) };
            pnlForm.Controls.Add(lblMsg);

            var btnSave = MakeBtn(pnlForm, "💾  Save Order", Purple, new Point(15, 362));
            btnSave.Click += BtnSave_Click;
            var btnClear = MakeBtn(pnlForm, "🗑️  Clear", Color.FromArgb(100, 100, 120), new Point(15, 410));
            btnClear.Click += (s, e) => ClearForm();

            // ── Grid panel ────────────────────────────────────────
            var pnlGrid = new Panel { Size = new Size(635, 560), Location = new Point(305, 10), BackColor = White };
            RoundCorners(pnlGrid, 12); Controls.Add(pnlGrid);

            AddLbl(pnlGrid, "Purchase Order History", 11, true, Purple, new Point(15, 14));
            Divider(pnlGrid, 40);

            grid = MakeGrid(new Point(15, 50), new Size(605, 490));
            grid.Columns.Add("POID",      "PO #");
            grid.Columns.Add("ItemName",  "Item");
            grid.Columns.Add("Supplier",  "Supplier");
            grid.Columns.Add("Qty",       "Qty");
            grid.Columns.Add("UnitCost",  "Unit Cost");
            grid.Columns.Add("Total",     "Total");
            grid.Columns.Add("Date",      "Date");
            grid.Columns.Add("By",        "Recorded By");
            pnlGrid.Controls.Add(grid);
        }

        void LoadItems()
        {
            cboItem.Items.Clear();
            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand("SELECT ItemID, ItemName, Price FROM inventory ORDER BY ItemName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        cboItem.Items.Add(new ItemEntry(r["ItemID"].ToString(), r["ItemName"].ToString(), Convert.ToDecimal(r["Price"])));
            }
            catch { }
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
        }

        void UpdateUnitCost()
        {
            if (cboItem.SelectedItem is ItemEntry item)
                txtUnitCost.Text = item.Price.ToString("F2");
        }

        void RecalcTotal()
        {
            if (int.TryParse(txtQuantity.Text, out int qty) &&
                decimal.TryParse(txtUnitCost.Text, out decimal cost))
                lblTotal.Text = "₱ " + (qty * cost).ToString("N2");
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
                    @"SELECT p.POID, i.ItemName, p.Supplier, p.Quantity,
                             p.UnitCost, p.TotalCost, p.OrderDate, p.RecordedBy
                      FROM purchase_orders p JOIN inventory i ON i.ItemID=p.ItemID
                      ORDER BY p.OrderDate DESC LIMIT 100", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        grid.Rows.Add(r["POID"], r["ItemName"], r["Supplier"], r["Quantity"],
                            "₱"+Convert.ToDecimal(r["UnitCost"]).ToString("N2"),
                            "₱"+Convert.ToDecimal(r["TotalCost"]).ToString("N2"),
                            Convert.ToDateTime(r["OrderDate"]).ToString("yyyy-MM-dd HH:mm"),
                            r["RecordedBy"]);
            }
            catch (Exception ex) { ShowMsg("Load error: " + ex.Message, Red); }
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is ItemEntry item)) { ShowMsg("Select an item.", Red); return; }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0) { ShowMsg("Enter a valid quantity.", Red); return; }
            if (!decimal.TryParse(txtUnitCost.Text, out decimal cost) || cost <= 0) { ShowMsg("Enter a valid unit cost.", Red); return; }
            if (string.IsNullOrWhiteSpace(txtSupplier.Text)) { ShowMsg("Enter supplier name.", Red); return; }

            try
            {
                using (var conn = DBConnection.GetConnection())
                using (var cmd = new MySqlCommand(
                    "INSERT INTO purchase_orders (ItemID,Supplier,Quantity,UnitCost,RecordedBy) VALUES (@i,@s,@q,@c,@r)", conn))
                {
                    cmd.Parameters.AddWithValue("@i", item.ID);
                    cmd.Parameters.AddWithValue("@s", txtSupplier.Text.Trim());
                    cmd.Parameters.AddWithValue("@q", qty);
                    cmd.Parameters.AddWithValue("@c", cost);
                    cmd.Parameters.AddWithValue("@r", _loggedInUser);
                    cmd.ExecuteNonQuery();
                }
                ShowMsg("Purchase order saved!", Green);
                ClearForm();
                LoadGrid();
            }
            catch (Exception ex) { ShowMsg("Error: " + ex.Message, Red); }
        }

        void ClearForm()
        {
            if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
            txtSupplier.Text = txtQuantity.Text = txtUnitCost.Text = "";
            lblTotal.Text = "₱ 0.00"; lblMsg.Text = "";
        }

        void ShowMsg(string t, Color c) { lblMsg.ForeColor = c; lblMsg.Text = t; }

        // ── Helpers ───────────────────────────────────────────────
        void AddLbl(Panel p, string t, float sz, bool bold, Color c, Point loc) =>
            p.Controls.Add(new Label { Text=t, Font=new Font("Segoe UI",sz,bold?FontStyle.Bold:FontStyle.Regular),
                ForeColor=c, AutoSize=true, Location=loc });

        void Divider(Panel p, int y) =>
            p.Controls.Add(new Panel { Size=new Size(p.Width-30,1), Location=new Point(15,y), BackColor=PurpleLight });

        TextBox AddTxt(Panel p, Point loc, Size sz)
        {
            var tb = new TextBox { Location=loc, Size=sz, BorderStyle=BorderStyle.FixedSingle,
                Font=new Font("Segoe UI",9.5f), BackColor=Color.FromArgb(249,247,255) };
            p.Controls.Add(tb); return tb;
        }

        Button MakeBtn(Panel p, string t, Color bg, Point loc)
        {
            var b = new Button { Text=t, Size=new Size(250,38), Location=loc, BackColor=bg,
                ForeColor=White, FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",9f,FontStyle.Bold), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize=0; RoundCorners(b,8); p.Controls.Add(b); return b;
        }

        DataGridView MakeGrid(Point loc, Size sz)
        {
            var g = new DataGridView { Location=loc, Size=sz, BackgroundColor=White, BorderStyle=BorderStyle.None,
                RowHeadersVisible=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly=true, AllowUserToAddRows=false, Font=new Font("Segoe UI",9f),
                GridColor=Color.FromArgb(225,220,240), SelectionMode=DataGridViewSelectionMode.FullRowSelect };
            g.ColumnHeadersDefaultCellStyle.BackColor=Purple; g.ColumnHeadersDefaultCellStyle.ForeColor=White;
            g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            g.DefaultCellStyle.SelectionBackColor=PurpleLight; g.DefaultCellStyle.SelectionForeColor=Color.FromArgb(40,30,80);
            g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(249,247,255);
            g.EnableHeadersVisualStyles=false; return g;
        }

        void RoundCorners(Control ctrl, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0,0,r*2,r*2,180,90); path.AddArc(ctrl.Width-r*2,0,r*2,r*2,270,90);
            path.AddArc(ctrl.Width-r*2,ctrl.Height-r*2,r*2,r*2,0,90); path.AddArc(0,ctrl.Height-r*2,r*2,r*2,90,90);
            path.CloseAllFigures(); ctrl.Region=new Region(path);
        }
    }

    // Helper class for ComboBox items
    public class ItemEntry
    {
        public string ID, Name; public decimal Price;
        public ItemEntry(string id, string name, decimal price) { ID=id; Name=name; Price=price; }
        public override string ToString() => Name;
    }
}
