using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Drawing.Chart;
using System.IO;

namespace BrewTrack
{
    public partial class ReportForm : Form
    {
        static readonly Color Purple = Color.FromArgb(88, 66, 190);
        static readonly Color PurpleDark = Color.FromArgb(58, 42, 130);
        static readonly Color PurpleLight = Color.FromArgb(237, 234, 255);
        static readonly Color White = Color.White;
        static readonly Color GrayText = Color.FromArgb(110, 105, 140);
        static readonly Color Green = Color.FromArgb(30, 150, 90);
        static readonly Color Red = Color.FromArgb(180, 40, 40);

        ComboBox cboReport;
        DataGridView grid;
        Label lblInfo;
        Button btnRun, btnExport;
        DataTable _currentData;
        string _loggedInUser;

        readonly (string Label, string SQL, string ChartTitle, string XCol, string YCol)[] Reports =
        {
            (
                "Sales Summary by Item",
                @"SELECT i.ItemName AS Item,
                         SUM(s.Quantity) AS TotalQtySold,
                         SUM(s.TotalAmount) AS TotalRevenue
                  FROM sales s JOIN inventory i ON i.ItemID=s.ItemID
                  GROUP BY i.ItemName ORDER BY TotalRevenue DESC",
                "Revenue by Item", "Item", "TotalRevenue"
            ),
            (
                "Stock In History",
                @"SELECT i.ItemName AS Item, SUM(si.Quantity) AS TotalReceived,
                         MAX(si.DateIn) AS LastReceived
                  FROM stock_in si JOIN inventory i ON i.ItemID=si.ItemID
                  GROUP BY i.ItemName ORDER BY TotalReceived DESC",
                "Stock Received by Item", "Item", "TotalReceived"
            ),
            (
                "Stock Out History",
                @"SELECT i.ItemName AS Item, SUM(so.Quantity) AS TotalConsumed,
                         MAX(so.DateOut) AS LastConsumed
                  FROM stock_out so JOIN inventory i ON i.ItemID=so.ItemID
                  GROUP BY i.ItemName ORDER BY TotalConsumed DESC",
                "Stock Consumed by Item", "Item", "TotalConsumed"
            ),
            (
                "Purchase Orders Summary",
                @"SELECT i.ItemName AS Item,
                         SUM(p.Quantity) AS TotalOrdered,
                         SUM(p.TotalCost) AS TotalCost
                  FROM purchase_orders p JOIN inventory i ON i.ItemID=p.ItemID
                  GROUP BY i.ItemName ORDER BY TotalCost DESC",
                "Purchase Cost by Item", "Item", "TotalCost"
            ),
            (
                "Current Inventory Status",
                @"SELECT ItemName AS Item, Category, Quantity AS CurrentStock,
                         Price AS UnitPrice,
                         (Quantity * Price) AS InventoryValue
                  FROM inventory ORDER BY Category, ItemName",
                "Inventory Value by Item", "Item", "InventoryValue"
            ),
        };

        public ReportForm(string username)
        {
            _loggedInUser = username;
            InitializeComponent();
            BuildUI();
        }

        void BuildUI()
        {
            Text = "Report Generator"; Size = new Size(1000, 660);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = Color.FromArgb(245, 243, 255);
            Font = new Font("Segoe UI", 9f);

            // Header bar
            var header = new Panel { Size = new Size(1000, 60), Location = new Point(0, 0), BackColor = White };
            Controls.Add(header);
            header.Controls.Add(new Label
            {
                Text = "📊 Report Generator",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize = true,
                Location = new Point(20, 14)
            });

            // Toolbar
            var toolbar = new Panel { Size = new Size(1000, 52), Location = new Point(0, 60), BackColor = PurpleLight };
            Controls.Add(toolbar);

            toolbar.Controls.Add(new Label
            {
                Text = "Report:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Purple,
                AutoSize = true,
                Location = new Point(16, 16)
            });

            cboReport = new ComboBox
            {
                Location = new Point(74, 12),
                Size = new Size(380, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = White
            };
            foreach (var r in Reports) cboReport.Items.Add(r.Label);
            cboReport.SelectedIndex = 0;
            toolbar.Controls.Add(cboReport);

            btnRun = ToolBtn("▶  Run", Purple, new Point(466, 11));
            btnRun.Click += BtnRun_Click;
            toolbar.Controls.Add(btnRun);

            btnExport = ToolBtn("💾  Export to Excel", Green, new Point(566, 11));
            btnExport.Size = new Size(160, 30);
            btnExport.Click += BtnExport_Click;
            btnExport.Enabled = false;
            toolbar.Controls.Add(btnExport);

            // Info label
            lblInfo = new Label
            {
                Text = "Select a report and click Run.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = GrayText,
                Size = new Size(960, 22),
                Location = new Point(20, 116),
                BackColor = Color.Transparent
            };
            Controls.Add(lblInfo);

            // Grid
            var pnlGrid = new Panel { Size = new Size(960, 465), Location = new Point(20, 142), BackColor = White };
            RoundCorners(pnlGrid, 10); Controls.Add(pnlGrid);

            grid = new DataGridView
            {
                Location = new Point(0, 0),
                Size = new Size(960, 465),
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
            grid.ColumnHeadersDefaultCellStyle.BackColor = Purple;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = PurpleLight;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 247, 255);
            grid.EnableHeadersVisualStyles = false;
            pnlGrid.Controls.Add(grid);
        }

        void BtnRun_Click(object sender, EventArgs e)
        {
            int idx = cboReport.SelectedIndex;
            if (idx < 0) return;

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    var da = new MySqlDataAdapter(Reports[idx].SQL, conn);
                    _currentData = new DataTable();
                    da.Fill(_currentData);
                    grid.DataSource = _currentData;

                    // Alternate row colors
                    foreach (DataGridViewRow row in grid.Rows)
                        row.DefaultCellStyle.BackColor = row.Index % 2 == 0 ? White : Color.FromArgb(249, 247, 255);

                    lblInfo.ForeColor = Green;
                    lblInfo.Text = $"✓ {Reports[idx].Label} — {_currentData.Rows.Count} record(s) · {DateTime.Now:yyyy-MM-dd HH:mm}";
                    btnExport.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblInfo.ForeColor = Red;
                lblInfo.Text = "Error: " + ex.Message;
            }
        }

        void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentData == null || _currentData.Rows.Count == 0)
            {
                MessageBox.Show("No data to export. Please run a report first.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idx = cboReport.SelectedIndex;
            var report = Reports[idx];

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dlg.FileName = report.Label.Replace(" ", "_") + "_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("Julie Ana Merle");

                    using (var pkg = new ExcelPackage())
                    {
                        // ── Sheet 1: Report Data ──────────────────────────────
                        var ws = pkg.Workbook.Worksheets.Add("Report");

                        // Company header (logo placeholder)
                        ws.Cells["A1"].Value = "☕ BrewTrack Café";
                        ws.Cells["A1"].Style.Font.Size = 18;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(88, 66, 190));
                        ws.Cells[1, 1, 1, _currentData.Columns.Count].Merge = true;

                        // Subtitle
                        ws.Cells["A2"].Value = "Inventory Management System";
                        ws.Cells["A2"].Style.Font.Italic = true;
                        ws.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(110, 105, 140));
                        ws.Cells[2, 1, 2, _currentData.Columns.Count].Merge = true;

                        // Logo placeholder note
                        ws.Cells["A3"].Value = "[Insert company logo here]";
                        ws.Cells["A3"].Style.Font.Size = 8;
                        ws.Cells["A3"].Style.Font.Color.SetColor(Color.LightGray);

                        // Report title
                        ws.Cells["A5"].Value = report.Label;
                        ws.Cells["A5"].Style.Font.Size = 13;
                        ws.Cells["A5"].Style.Font.Bold = true;
                        ws.Cells[5, 1, 5, _currentData.Columns.Count].Merge = true;

                        ws.Cells["A6"].Value = $"Generated: {DateTime.Now:MMMM dd, yyyy  HH:mm}";
                        ws.Cells["A6"].Style.Font.Size = 9;
                        ws.Cells["A6"].Style.Font.Color.SetColor(Color.FromArgb(110, 105, 140));
                        ws.Cells[6, 1, 6, _currentData.Columns.Count].Merge = true;

                        ws.Cells["A7"].Value = $"Generated by: {_loggedInUser}";
                        ws.Cells["A7"].Style.Font.Size = 9;
                        ws.Cells[7, 1, 7, _currentData.Columns.Count].Merge = true;

                        // Column headers (row 9)
                        int headerRow = 9;
                        for (int c = 0; c < _currentData.Columns.Count; c++)
                        {
                            var cell = ws.Cells[headerRow, c + 1];
                            cell.Value = _currentData.Columns[c].ColumnName;
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.Color.SetColor(Color.White);
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(88, 66, 190));
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        }

                        // Data rows
                        for (int row = 0; row < _currentData.Rows.Count; row++)
                        {
                            bool alt = row % 2 == 1;
                            for (int col = 0; col < _currentData.Columns.Count; col++)
                            {
                                var cell = ws.Cells[headerRow + 1 + row, col + 1];
                                cell.Value = _currentData.Rows[row][col];
                                if (alt)
                                {
                                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(249, 247, 255));
                                }
                                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                                cell.Style.Border.Bottom.Color.SetColor(Color.FromArgb(225, 220, 240));
                            }
                        }

                        // Auto-fit columns
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

                        // ── Signature section ─────────────────────────────────
                        int sigRow = headerRow + _currentData.Rows.Count + 3;
                        ws.Cells[sigRow, 1].Value = "Prepared by:";
                        ws.Cells[sigRow, 1].Style.Font.Bold = true;

                        int sigLineRow = sigRow + 3;
                        ws.Cells[sigLineRow, 1].Value = "______________________________";
                        ws.Cells[sigLineRow + 1, 1].Value = _loggedInUser;
                        ws.Cells[sigLineRow + 1, 1].Style.Font.Bold = true;
                        ws.Cells[sigLineRow + 2, 1].Value = "BrewTrack Staff";
                        ws.Cells[sigLineRow + 2, 1].Style.Font.Italic = true;
                        ws.Cells[sigLineRow + 3, 1].Value = $"Date: {DateTime.Now:MMMM dd, yyyy}";

                        // Second signature column (for approval)
                        int sigCol2 = _currentData.Columns.Count > 2 ? _currentData.Columns.Count - 1 : 3;
                        ws.Cells[sigRow, sigCol2].Value = "Approved by:";
                        ws.Cells[sigRow, sigCol2].Style.Font.Bold = true;
                        ws.Cells[sigLineRow, sigCol2].Value = "______________________________";
                        ws.Cells[sigLineRow + 1, sigCol2].Value = "Manager / Supervisor";
                        ws.Cells[sigLineRow + 1, sigCol2].Style.Font.Bold = true;
                        ws.Cells[sigLineRow + 2, sigCol2].Value = "BrewTrack Management";
                        ws.Cells[sigLineRow + 2, sigCol2].Style.Font.Italic = true;
                        ws.Cells[sigLineRow + 3, sigCol2].Value = "Date: ____________________";

                        // ── Sheet 2: Chart ────────────────────────────────────
                        var wsChart = pkg.Workbook.Worksheets.Add("Chart");

                        // Find X and Y column indices
                        int xColIdx = -1, yColIdx = -1;
                        for (int c = 0; c < _currentData.Columns.Count; c++)
                        {
                            if (_currentData.Columns[c].ColumnName == report.XCol) xColIdx = c;
                            if (_currentData.Columns[c].ColumnName == report.YCol) yColIdx = c;
                        }
                        // Fallback to first two columns
                        if (xColIdx < 0) xColIdx = 0;
                        if (yColIdx < 0) yColIdx = 1;

                        // Write chart data to Sheet 2
                        wsChart.Cells["A1"].Value = report.XCol;
                        wsChart.Cells["B1"].Value = report.YCol;
                        wsChart.Cells["A1"].Style.Font.Bold = true;
                        wsChart.Cells["B1"].Style.Font.Bold = true;

                        for (int row = 0; row < _currentData.Rows.Count; row++)
                        {
                            wsChart.Cells[row + 2, 1].Value = _currentData.Rows[row][xColIdx]?.ToString();
                            object val = _currentData.Rows[row][yColIdx];
                            wsChart.Cells[row + 2, 2].Value = val is DBNull ? 0 : Convert.ToDouble(val);
                        }

                        // Create bar chart
                        var chart = wsChart.Drawings.AddChart("DataChart", eChartType.ColumnClustered);
                        chart.Title.Text = report.ChartTitle;
                        chart.SetPosition(2, 0, 3, 0);
                        chart.SetSize(600, 350);
                        chart.Style = eChartStyle.Style10;

                        var series = chart.Series.Add(
                            wsChart.Cells[2, 2, _currentData.Rows.Count + 1, 2],
                            wsChart.Cells[2, 1, _currentData.Rows.Count + 1, 1]);
                        series.Header = report.YCol;

                        // Chart sheet header
                        wsChart.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        wsChart.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 234, 255));
                        wsChart.Cells["B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        wsChart.Cells["B1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 234, 255));
                        wsChart.Cells[1, 1, 1, 2].AutoFitColumns();

                        // ── Save ──────────────────────────────────────────────
                        pkg.SaveAs(new FileInfo(dlg.FileName));
                    }

                    MessageBox.Show("Report exported successfully!\n" + dlg.FileName,
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export error:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        Button ToolBtn(string t, Color bg, Point loc)
        {
            var b = new Button
            {
                Text = t,
                Size = new Size(90, 30),
                Location = loc,
                BackColor = bg,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0; return b;
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