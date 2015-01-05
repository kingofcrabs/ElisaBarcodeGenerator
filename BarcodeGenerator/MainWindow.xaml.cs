using BarcodeGenerator.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace BarcodeGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<CellPosition, string> cellPos_barcodeDict = new Dictionary<CellPosition, string>();
        bool bok = false;
        int sampleCount = 16;
        DataTable tbl;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int gridStartPos = int.Parse(ConfigurationManager.AppSettings["StartGrid"]);
            
            //flash
            this.InvalidateVisual();
            System.Threading.Thread.Sleep(200);

            //check grid start Pos
            if (gridStartPos < 1 || gridStartPos > 30)
            {
                SetInfo("起始Grid必须在1-30之间，请重新设置！", Colors.Red);
                this.IsEnabled = false;
                return;
            }
            UpdateDataGridView();
            InitListView();
            dgvSampleBarcode.EditMode = DataGridViewEditMode.EditProgrammatically;
            dgvSampleBarcode.Height = dgvSampleBarcode.Parent.Height;
            dgvSampleBarcode.Width = dgvSampleBarcode.Parent.Width;
        }

        #region plate barcodes
        private void InitListView()
        {
            string[] columnNames = { "PlateID","AssayName","Barcode" };
            int shelfColumns = int.Parse(ConfigurationManager.AppSettings[stringRes.shelfColumns]);
            tbl = new DataTable("PlateBarcode");
            foreach(string columnName in columnNames)
            {
                tbl.Columns.Add(columnName, typeof(string));
            }

            List<string> reagentPlates = ReadReagentsLayout();
            for (int row = 0; row < reagentPlates.Count; row++ )
            {
                List<string> cellContents = new List<string>();
                cellContents.Add(Convert.ToString(row+1));
                cellContents.Add(reagentPlates[row]);
                cellContents.Add("");
                tbl.Rows.Add(cellContents.ToArray());
            }
            lstViewPlateBarcode.ItemsSource = tbl.DefaultView;
        }

        private Dictionary<string,string> GetPlate_BarcodeDict()
        {
            Dictionary<string, string> plate_barcodeDict = new Dictionary<string, string>();
            foreach(var row in tbl.Rows)
            {
                
            }
            return null;
        }

        private List<string> ReadReagentsLayout()
        {
            List<string> strs = new List<string>();
            string sLayoutFile = Utility.GetConfigFolder() + stringRes.layoutConfigFile;
            var reagents = File.ReadAllLines(sLayoutFile);
            return reagents.Where(x => x != "").ToList();
        }
        #endregion

        #region confirm setting
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bok = CheckSettings();
                if (!bok)
                    return;
                else
                {
                    SetInfo("", Colors.Green);
                }
                WriteBarcodes4PosID();
                Utility.SaveSetting(dgvSampleBarcode, sampleCount);
                Utility.Move2WorkingFolder();
            }
            catch(Exception ex)
            {
                bok = false;
                SetInfo(ex.Message, true);
                return;
            }
            this.Close();
            Utility.WriteExecuteResult(bok);
        }
        #region merge with predefined barcodes
        private void WriteBarcodes4PosID()
        {
            //string sFileBarcode = Utility.GetBarcodesFolder() + "sampleBarcodes.txt";
            string sFileCnt = Utility.GetBarcodesFolder() + "sampleCnt.txt";
            List<string> sampleBarcodes = GetSampleBarcodes();
            Dictionary<string, string> predefinedPanelName_PathDict = GetAllPredefinedPlateNames();
            string sDate = DateTime.Now.ToString("yyMMddhhmmss");
            foreach(var pair in predefinedPanelName_PathDict)
            {
                WriteBarcodeAfterMerge(sampleBarcodes,pair,sDate);
            }
            File.WriteAllText(sFileCnt, sampleCount.ToString());
        }

        private void WriteBarcodeAfterMerge(List<string> allSamplebarcodes,
            KeyValuePair<string, string> name_path,
            string sDate)
        {
            List<string> allBarcodes = new List<string>(allSamplebarcodes);
            string[] predefinedBarcodes = GetPredefinedBarcodesAppendWithDateInfo(name_path.Value, sDate);
            allBarcodes.AddRange(predefinedBarcodes);
            string sFile = Utility.GetBarcodesFolder() + name_path.Key + ".txt";
            File.WriteAllLines(sFile, allBarcodes);
        }
        private static string[] GetPredefinedBarcodesAppendWithDateInfo(string sFile, string sDate)
        {
            string[] allPredefinedLines = new string[1];
            if (File.Exists(sFile))
            {
                allPredefinedLines = File.ReadAllLines(sFile);
                AddDateInfo(allPredefinedLines, sDate);
            }
            return allPredefinedLines;
        }

        private Dictionary<string, string> GetAllPredefinedPlateNames()
        {
            string sFolder = Utility.GetPredefinedBarcodesFolder();
            string[] allFilePaths = Directory.GetFiles(sFolder, "*.txt");
            Dictionary<string, string> panelNames = new Dictionary<string, string>();
            foreach (string sFilePath in allFilePaths)
            {
                FileInfo fileInfo = new FileInfo(sFilePath);
                panelNames.Add(fileInfo.Name.Replace(".txt","").Trim(), sFilePath);
            }
            return panelNames;
        }

        private static void AddDateInfo(string[] allLines, string sDate)
        {
            for (int i = 0; i < allLines.Length; i++)
            {
                char last = allLines[i].Last();
                if (last == '$' || last == ']')
                    continue;
                allLines[i] += sDate;
            }
        }
        #endregion

        
        private List<string> GetSampleBarcodes()
        {
            List<string> sampleBarcodesInfo = new List<string>();
            int gridStartPos = int.Parse(ConfigurationManager.AppSettings["StartGrid"]);
            int sampleGrids = int.Parse(ConfigurationManager.AppSettings[stringRes.sampleGrids]);
            for (int c = 0; c < sampleGrids; c++)
            {
                int col = c + gridStartPos;
                string sGridDesc = string.Format("[Grid{0}]", col);
                sampleBarcodesInfo.Add(sGridDesc);

                for (int r = 0; r < 16; r++)
                {
                    //swBarcodes.WriteLine(string.Format("{0}  =  {1}", r + 1, sBarcode));
                    string sBarcode = "$$$";
                    CellPosition cellPos = new CellPosition(c, r);
                    if (cellPos_barcodeDict.ContainsKey(cellPos))
                        sBarcode = cellPos_barcodeDict[cellPos];
                    string sLineContent = string.Format("{0} = {1}", r + 1, sBarcode);
                    sampleBarcodesInfo.Add(sLineContent);
                }
            }
            return sampleBarcodesInfo;
        }
        private bool CheckSettings()
        {
            int colNum = (sampleCount + 16 - 1) / 16;
            List<int> rowCounts = new List<int>();
            for (int i = 0; i < colNum - 1; i++)
                rowCounts.Add(16);
            if (sampleCount % 16 == 0)
                rowCounts.Add(16);
            else
                rowCounts.Add(sampleCount % 16);

            string errMsg;
            for (int c = 0; c < colNum; c++)
            {
                for (int r = 0; r < rowCounts[c]; r++)
                {
                    CellPosition cellPos = new CellPosition(c, r);
                    string barcode = "";
                    if (cellPos_barcodeDict.ContainsKey(cellPos))
                        barcode = cellPos_barcodeDict[cellPos];
                    if (!IsValidBarcode(barcode, out errMsg, r, c))
                    {
                        SetInfo(errMsg, Colors.Red);
                        dgvSampleBarcode.ClearSelection();
                        dgvSampleBarcode.Rows[r].Cells[c].Style.BackColor = System.Drawing.Color.Red;
                        return false;
                    }
                }
            }
            return true;

        }

        private bool IsValidBarcode(string sBarcode, out string errMsg, int curRow, int curCol)
        {
            errMsg = "";
            if (sBarcode == "" || sBarcode == null)
            {

                string sErrRow = dgvSampleBarcode.Rows[curRow].HeaderCell.Value.ToString();
                string sErrCol = dgvSampleBarcode.Columns[curCol].HeaderCell.Value.ToString();
                errMsg = string.Format("位于：{0}{1}的条形码为空！", sErrCol, sErrRow);
                return false;
            }
            for (int row = 0; row < dgvSampleBarcode.Rows.Count; row++)
            {
                for (int col = 0; col < dgvSampleBarcode.Rows[row].Cells.Count; col++)
                {
                    //只测试当前样品前面的样品
                    if (col * 16 + row >= curCol * 16 + curRow)
                        continue;
                    if (dgvSampleBarcode.Rows[row].Cells[col].Value.ToString() == sBarcode)
                    {
                        string sCurRowHeader = dgvSampleBarcode.Rows[row].HeaderCell.Value.ToString();
                        string sCurColHeader = dgvSampleBarcode.Columns[col].HeaderCell.Value.ToString();
                        errMsg = string.Format("条形码已经存在于：{0}，{1}", sCurColHeader, sCurRowHeader);
                        return false;
                    }
                }
            }
            errMsg = "";
            return true;
        }


        #endregion 

        
        #region utility
        private bool AlreadyHasInfo()
        {
            bool bHasInfo = false;
            for (int r = 0; r < dgvSampleBarcode.Rows.Count; r++)
            {
                for (int c = 0; c < dgvSampleBarcode.Rows[r].Cells.Count; c++)
                {
                    string s = dgvSampleBarcode.Rows[r].Cells[c].Value.ToString();
                    if (s != "")
                    {
                        bHasInfo = true;
                        break;
                    }
                }
            }
            return bHasInfo;
        }
        void SetInfo(string s, bool error = false)
        {
            var color = error ? Colors.Red : Colors.Black;
            SetInfo(s, color);
        }

        void SetInfo(string s, Color color)
        {
            if (txtInfo == null)
                return;

            txtInfo.Background = new SolidColorBrush(Colors.White);
            txtInfo.Text = s;
            txtInfo.Foreground = new SolidColorBrush(color);
        }
        #endregion

        #region set barcodes
        private List<string> GetSetting()
        {
            int count = 0;
            int start = 0;
            bool bStartEndMode = (bool)rdbStartEnd.IsChecked;
            bool bStartCountMode = (bool)rdbStartCount.IsChecked;
            if (bStartEndMode)
            {
                if (txtStartBarcodeApproach2.Text == "")
                    throw new Exception("开始编号不得为空！");
                if (txtEndBarcode.Text == "")
                    throw new Exception("结束编号不得为空！");

                start = int.Parse(txtStartBarcodeApproach2.Text);
                int end = int.Parse(txtEndBarcode.Text);
                if (end <= start)
                {
                    throw new Exception("结束编号必须大于起始编号！");
                }
                count = end - start + 1;
            }
            else if (bStartCountMode)
            {
                if (txtStartBarcodeApproach1.Text == "")
                    throw new Exception("开始编号不得为空！");
                if (txtCount.Text == "")
                    throw new Exception("数量不得为空！");
                start = int.Parse(txtStartBarcodeApproach1.Text);
                count = int.Parse(txtCount.Text);
                if (count <= 0)
                    throw new Exception("设置数量必须大于0!");
            }
            else
            {
                return new List<string>() { txtCurBarcode.Text };
            }

            List<string> barcodes = new List<string>();
            for (int i = 0; i < count; i++)
            {
                barcodes.Add(Convert.ToString(i + start));
            }
            return barcodes;
        }

        private void btnSetBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetInfo("");
                if (!HasSelectFirstCell())
                {
                    SetInfo("请选中首单元格！", System.Windows.Media.Colors.Red);
                    return;
                }
                var barcodes = GetSetting();
                if (WorkingonSampleBarcode())
                {
                    List<DataGridViewCell> cells = new List<DataGridViewCell>();
                    foreach (DataGridViewCell cell in dgvSampleBarcode.SelectedCells)
                        cells.Add(cell);
                    var firstCell = cells.OrderBy(x => x.ColumnIndex * 16 + x.RowIndex).First();
                    FillSampleBarcodes(dgvSampleBarcode, firstCell, barcodes);
                    if((bool)rdbMannual.IsChecked)
                    {
                        ClearText();
                        Move2NextCell();
                    }
                }
                else
                {
                    FillPlateBarcodes(barcodes);
                }
            }
            catch (Exception ex)
            {
                SetInfo(ex.Message, System.Windows.Media.Colors.Red);
                return;
            }
        }

     
        #region move to next cell
        private void ClearText()
        {
            txtCurBarcode.Text = "";
        }

        private void Move2NextCell()
        {
            int row, col;
            DataGridViewCell curCell = dgvSampleBarcode.CurrentCell;
            row = curCell.RowIndex;
            col = curCell.ColumnIndex;

         
            int wellID = row + col * 16 + 1;
            int sampleCount = int.Parse(txtSampleCount.Text);
            if (wellID >= sampleCount)
            {
                SetInfo("已经到达最后一个样品！",Colors.Orange);
                return;
            }
            if (row < 15)
                row++;
            else
            {
                row = 0;
                col++;
            }
            dgvSampleBarcode.CurrentCell = dgvSampleBarcode.Rows[row].Cells[col];
        }
        #endregion

        private void FillPlateBarcodes(List<string> barcodes)
        {
            int startRowIndex = lstViewPlateBarcode.SelectedIndex;
            int fillCnt = Math.Min(barcodes.Count, tbl.Rows.Count - startRowIndex);
            for(int i = 0; i< fillCnt; i++)
            {
                int rowIndex = startRowIndex + i;
                tbl.Rows[rowIndex][2] = barcodes[i];
            }
        }

        private bool WorkingonSampleBarcode()
        {
            return tabbarcodeDest.SelectedIndex == 0;
        }

        private bool HasSelectFirstCell()
        {
            if (WorkingonSampleBarcode())
                return dgvSampleBarcode.SelectedCells.Count > 0;
            else
                return lstViewPlateBarcode.SelectedIndex != -1;
        }

        public void FillSampleBarcodes(DataGridView dgvSampleBarcode, DataGridViewCell startCell,
            List<string> sBarcodes)
        {
            int num2Complete = sBarcodes.Count;
            int colStart = startCell.ColumnIndex;
            int rowStart = startCell.RowIndex;
            int startCellID = colStart * 16 + rowStart + 1;
            int curAutoCellIndex = 0;

            for (int col = colStart; col < dgvSampleBarcode.Columns.Count; col++)
            {
                for (int row = 0; row < dgvSampleBarcode.Rows.Count; row++)
                {
                    int curCellID = row + col * 16 + 1; 
                    if (curCellID < startCellID)
                        continue;
                    if( curCellID > sampleCount )
                        return;
                    CellPosition cellPos = new CellPosition(col, row);
                    DataGridViewCell cell = dgvSampleBarcode.Rows[row].Cells[col];
                    string sTmpBarcode = sBarcodes[curAutoCellIndex++];
                    cell.Value = sTmpBarcode;
                    cell.Style.BackColor = System.Drawing.Color.White;
                    cellPos_barcodeDict[cellPos] = sTmpBarcode;
                    if (curAutoCellIndex == num2Complete)
                    {
                        return;
                    }
                }
            }
        }
        #endregion

    

     


        private void btnSetSampleCount_Click(object sender, RoutedEventArgs e)
        {
            SetInfo("", Colors.Black);
            int maxSampleCount = Utility.GetMaxSampleCount();
            int i;
            bok = int.TryParse(txtSampleCount.Text, out i);
            if (!bok)
                SetInfo("样品数量必须为数字！", Colors.Red);
            if (i <= 0 || i > maxSampleCount)
            {
                SetInfo(string.Format("样品数量必须介于1和{0}之间", maxSampleCount), Colors.Red);
                bok = false;
            }

            if (!bok)
            {
                txtSampleCount.Text = sampleCount.ToString();
                return;
            }
            if (AlreadyHasInfo())
            {
                var result = System.Windows.Forms.MessageBox.Show
                    ("已经设置过一些样品,您确定要继续？\r\n点击‘Yes’将会清空设置信息！", "警告",
                    MessageBoxButtons.YesNo);
                if (result != System.Windows.Forms.DialogResult.Yes)
                    return;
                cellPos_barcodeDict.Clear();
            }
            sampleCount = i;
            UpdateDataGridView();
        }

        private void UpdateDataGridView()
        {
            //dataGridView.Columns.Add(new System.Windows.Forms.DataGridViewColumn());
            dgvSampleBarcode.AllowUserToAddRows = false;
            dgvSampleBarcode.Columns.Clear();
            List<string> strs = new List<string>();

            int colNum = (int)Math.Ceiling(sampleCount / 16.0);
            for (int j = 0; j < colNum; j++)
                strs.Add("");
            int gridStartPos = int.Parse(ConfigurationManager.AppSettings["StartGrid"]);
            for (int i = 0; i < colNum; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.HeaderText = string.Format("条{0}", gridStartPos + i);
                dgvSampleBarcode.Columns.Add(column);
                dgvSampleBarcode.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            dgvSampleBarcode.RowHeadersWidth = 80;
            for (int i = 0; i < 16; i++)
            {
                dgvSampleBarcode.Rows.Add(strs.ToArray());
                dgvSampleBarcode.Rows[i].HeaderCell.Value = string.Format("行{0}", i + 1);
            }
        }


        #region command
        private void CommandHelp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            HelpForm helpForm = new HelpForm();
            helpForm.ShowDialog();
        }

        private void CommandHelp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        #endregion

    }

    public struct CellPosition
    {
        public int col;
        public int row;
        public CellPosition(int ix, int iy)
        {
            col = ix;
            row = iy;
        }
    }
}
