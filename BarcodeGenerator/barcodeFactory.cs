using BarcodeGenerator.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BarcodeGenerator
{
    interface IBarcodeGenerator
    {
        string Generate(string sPanel, int col, int row);
        void Save();
    }

    class BarcodeFactory
    {
        public static IBarcodeGenerator CreateGenerator()
        {
            string name = ConfigurationManager.AppSettings["BarcodeGenerator"];
            IBarcodeGenerator bg = null;
            switch (name)
            {
                case "sequential":
                    bg = new SequentialBarcodeGenerator();
                    break;
                case "":
                    bg = null;
                    break;
                default:
                    bg = new DefaultBarcodeGenerator();
                    break;
            }
            return bg;
        }
    }
    class PanelInfo
    {
        public string prefix;
        public int seqNo;
        public PanelInfo(string p, int no)
        {
            prefix = p;
            seqNo = no;
        }
    }
    class SequentialBarcodeGenerator : DefaultBarcodeGenerator
    {
        public SequentialBarcodeGenerator()
            : base()
        {
            Debug.WriteLine("SequentialBarcodeGenerator");
        }

        public override string Generate(string sPanel, int col, int row)
        {
            return base.IncreaseSeqNo(sPanel);
        }

    }

    class DefaultBarcodeGenerator : IBarcodeGenerator
    {
        string sTime = "";
        Dictionary<string, PanelInfo> panel_BarcodeInfo = new Dictionary<string, PanelInfo>();
        string header = "";
        string sTemplateFile = "";
        public DefaultBarcodeGenerator()
        {
            sTime = DateTime.Now.ToString("yyMMddhhmmss");
            sTemplateFile = Utility.GetInfoFolder() + stringRes.barcodeTemplate;
            ReadInfo();
        }

        private void ReadInfo()
        {
            string[] strs = File.ReadAllLines(sTemplateFile, System.Text.Encoding.Default);
            header = strs[0];
            for (int i = 1; i < strs.Length; i++)
            {
                string[] innerStrs = strs[i].Split(';');
                panel_BarcodeInfo.Add(innerStrs[0], new PanelInfo(innerStrs[1], int.Parse(innerStrs[2])));
            }
        }

        protected string IncreaseSeqNo(string sPanel)
        {
            if (sPanel == "" || sPanel == stringRes.nullPanel)
                return sPanel;
            if (!panel_BarcodeInfo.ContainsKey(sPanel))
            {
                sPanel = "misc";
            }
            PanelInfo panelInfo = panel_BarcodeInfo[sPanel];
            string sDate = DateTime.Now.ToString("MMdd");
            string sBarcode = string.Format("{0}{1}{2:D5}", panelInfo.prefix, sDate, panelInfo.seqNo++);
            return sBarcode;
        }

        virtual public string Generate(string sPanel, int col, int row)
        {
            string sBarcode = IncreaseSeqNo(sPanel);
            if (sBarcode == "" || sBarcode == stringRes.nullPanel)
                return sBarcode;
            return string.Format("{0}{1:D1}{2:D2}", sTime, col + 1, row);
        }

        public void Save()
        {
            List<string> strs = new List<string>();
            strs.Add(header);
            foreach (var pair in panel_BarcodeInfo)
            {
                string s = string.Format("{0};{1};{2}", pair.Key, pair.Value.prefix, pair.Value.seqNo);
                strs.Add(s);
            }
            File.WriteAllLines(sTemplateFile, strs, System.Text.Encoding.Unicode);
        }
    }
}
