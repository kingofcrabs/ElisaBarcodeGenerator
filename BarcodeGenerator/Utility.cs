using BarcodeGenerator.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace BarcodeGenerator
{
    class Utility
    {
        static string sSaveFolder = "";

        static public List<string> ParseDigital(string digitalName, List<string> allTests)
        {
            List<string> strs = new List<string>();
            int val = int.Parse(digitalName);
            int cnt = allTests.Count;
            for (int i = 0; i < cnt; i++)
            {
                int mask = (int)Math.Pow(2.0, i);
                if ((mask & val) != 0)
                {
                    strs.Add(allTests[i]);
                }
                if (mask > val)
                    break;
            }
            return strs;
        }

        static public bool IsDigital(string s)
        {
            return s.All(x => Char.IsDigit(x));
        }

        static public int GetSampleCountPerPlate()
        {
            int smpCountPerPlate = int.Parse(ConfigurationManager.AppSettings[stringRes.sampleCountPerPlate]);
            return smpCountPerPlate;
        }


        static public string GetOutputFolder()
        {
            if (sSaveFolder != "")
                return sSaveFolder;

            sSaveFolder = GetExeFolder() + "\\Output\\";
            sSaveFolder += "\\" + DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(sSaveFolder))
            {
                Directory.CreateDirectory(sSaveFolder);
            }

            sSaveFolder += "\\" + DateTime.Now.ToString("HHmmss");
            if (!Directory.Exists(sSaveFolder))
            {
                Directory.CreateDirectory(sSaveFolder);
            }
            sSaveFolder += "\\";
            return sSaveFolder;
        }

        static public string GetBarcodesFolder()
        {
            string s = GetOutputFolder();
            s += "\\Barcodes\\";
            if (!Directory.Exists(s))
                Directory.CreateDirectory(s);
            return s;
        }
        internal static string GetPredefinedBarcodesFolder()
        {
            string s = GetConfigFolder();
            s += "\\PredefinedBarcodes\\";
            if (!Directory.Exists(s))
                Directory.CreateDirectory(s);
            return s;
        }
        static public string GetLayoutFolder()
        {
            string s = GetOutputFolder();
            s += "\\Layout\\";
            if (!Directory.Exists(s))
                Directory.CreateDirectory(s);
            return s;
        }
        static public string GetInfoFolder()
        {
            return GetConfigFolder();
        }

        static public string GetConfigFolder()
        {
            string s = GetExeFolder() + stringRes.configFolder;
            return s;
        }

        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s;
        }
 

        static public string GetExcelPath()
        {
            //throw new NotImplementedException();
            string s = GetOutputFolder();
            s += "\\input.xls";
            return s;
        }

        public static bool IsFileInUse(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            bool inUse = true;
            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,
                FileShare.None);
                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }


        private static List<string> GetAllBarcodes(Dictionary<string, List<string>> dictionary)
        {
            List<string> allBarcodes = new List<string>();
            foreach (var barcodes in dictionary.Values)
                allBarcodes = allBarcodes.Union(barcodes).ToList();
            return allBarcodes;
        }

        private static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);

                string dest = Path.Combine(destFolder, name);

                File.Copy(file, dest, true);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);

                string dest = Path.Combine(destFolder, name);

                CopyFolder(folder, dest);
            }
        }
   

        public static void Move2WorkingFolder()
        {
            string sWorkingFolder = GetExeFolder() + stringRes.workingFolder;
            if (!Directory.Exists(sWorkingFolder))
                Directory.CreateDirectory(sWorkingFolder);
            else
            {
                try
                {
                    DeleteDir(sWorkingFolder, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("无法删除working文件夹,这是由于某个文件正被打开所致\r\n,"
                    + "请关闭working文件夹下的所有文件！\r\n");
                    //throw new Exception("Failed to delete working folder.");
                }
            }
            CopyFolder(GetOutputFolder(), sWorkingFolder);
        }

     

       
     

        private static bool CheckDeleteResult(string sWorkingFolder)
        {
            if (sWorkingFolder[sWorkingFolder.Length - 1] != Path.DirectorySeparatorChar)
                sWorkingFolder += Path.DirectorySeparatorChar;
            string[] fileList = Directory.GetFileSystemEntries(sWorkingFolder);
            return fileList.Length == 0;
        }


        public static void DeleteDir(string aimPath, bool bKeepCurrent)
        {
            if (!bKeepCurrent)
            {
                Directory.Delete(aimPath, true);
                return;
            }

            // 检查目标目录是否以目录分割字符结束如果不是则添加之
            if (aimPath[aimPath.Length - 1] != Path.DirectorySeparatorChar)
                aimPath += Path.DirectorySeparatorChar;
            // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
            // 如果你指向Delete目标文件下面的文件而不包含目录请使用下面的方法
            // string[] fileList = Directory.GetFiles(aimPath);
            string[] fileList = Directory.GetFileSystemEntries(aimPath);
            // 遍历所有的文件和目录
            foreach (string file in fileList)
            {
                // 先当作目录处理如果存在这个目录就递归Delete该目录下面的文件
                if (Directory.Exists(file))
                {
                    DeleteDir(aimPath + Path.GetFileName(file), false);
                }
                // 否则直接Delete文件
                else
                {
                    if (Path.GetFileName(file).IndexOf("barcodes.txt") != -1) //don't delete barcodes, it is needed by EVOware
                        continue;
                    File.Delete(aimPath + Path.GetFileName(file));
                }
            }
        }
    
        static public void WriteExecuteResult(bool bok)
        {
            string sWorkingFolder = GetExeFolder() + stringRes.workingFolder;
            File.WriteAllText(sWorkingFolder + "result.txt", bok.ToString());
        }
        public static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Encoding = new UnicodeEncoding(false, false);
            settings.Indent = true;
            //settings.OmitXmlDeclaration = false;

            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReaderSettings settings = new XmlReaderSettings();

            using (StringReader textReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }
        static public void SaveSetting(DataGridView dataGridView, int sampleCount)
        {

            StreamWriter sw = new StreamWriter(Utility.GetExcelPath(), false, 
                System.Text.Encoding.GetEncoding(-0));
            string strLine = "";

            char comma = ',';
            //Write in the headers of the columns.  
            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                if (i > 0)
                    strLine += comma;
                strLine += dataGridView.Columns[i].HeaderText;
            }
            strLine.Remove(strLine.Length - 1);
            sw.WriteLine(strLine);
            strLine = "";
            //Write in the content of the columns.  
            int lastColumnRows = sampleCount % 16;
            if (lastColumnRows == 0)
                lastColumnRows = 16;
            for (int j = 0; j < dataGridView.Rows.Count; j++)
            {
                strLine = "";
                for (int k = 0; k < dataGridView.Columns.Count; k++)
                {

                    if (k == dataGridView.Columns.Count - 1 && j >= lastColumnRows)
                        break;

                    if (k > 0)
                        strLine += comma;
                    if (dataGridView.Rows[j].Cells[k].Value == null)
                        strLine += "";
                    else
                    {
                        string m = dataGridView.Rows[j].Cells[k].Value.ToString().Trim();
                        strLine += m;
                    }
                }
                if (strLine != string.Empty)
                {
                    strLine.Remove(strLine.Length - 1);
                    sw.WriteLine(strLine);
                }
            }
            sw.Close();
        }

        internal static int GetMaxSampleCount()
        {
            int sampleGrids = int.Parse(ConfigurationManager.AppSettings[stringRes.sampleGrids]);
            return sampleGrids * 16;
        }
    }
}
