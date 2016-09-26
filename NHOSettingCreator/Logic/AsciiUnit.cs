using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace NHOSettingCreator.Logic
{
    /// <summary>
    /// # 単位のASCII文字変換
    /// </summary>
    class AsciiUnit : ICommand
    {

        const string asciiUnitMaserURL= "https://raw.githubusercontent.com/nhoHQ/SSMIX2_support_documents/master/doc/ascii_units.md";

        public void execute()
        {
            //改行ごとに配列にする
            string[] responseArray=RequestURL().Split('\n');

            //"|----|----|----|----|"の行以下が設定値なので、それだけ抽出
            var asciiUnitRows = GetSettingsRows(responseArray);

            List<string> iniSettings = new List<string>();
            foreach(var a in asciiUnitRows)
            {
                foreach (var iniSetting in a.GetIniSettings())
                {
                    iniSettings.Add(iniSetting);
                }
            }

            var l = iniSettings;
        }

        private string RequestURL()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(asciiUnitMaserURL);
            req.Method = "GET";

            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string content = sr.ReadToEnd();

            return content;
        }

        private List<AsciiUnitMasterRow> GetSettingsRows(string[] rows)
        {
            int headerIndex = rows.ToList().IndexOf("|----|----|----|----|");
            int specialReportIndex = rows.ToList().IndexOf("### 特記事項");
            int specialReportHeaderIndex = rows.Skip(specialReportIndex).ToList().IndexOf("|----|----|----|");

            List<AsciiUnitMasterRow> l = new List<AsciiUnitMasterRow>();

            //特記事項
            var specialReportRows = rows.ToList().Skip(specialReportIndex + specialReportHeaderIndex+1).Where(r=>!string.IsNullOrEmpty(r)) ;

            foreach (var r in rows.ToList().Skip(headerIndex + 1).Take(specialReportIndex - headerIndex-1).Where(r => !string.IsNullOrEmpty(r)))
            {
                l.Add(new AsciiUnitMasterRow {  AsciiUnit = r.Split('|')[1],
                                                DisplayUnitExample = r.Split('|')[2].Split(',').ToList(),
                                                ISO = r.Split('|')[3],
                                                Bikou=r.Split('|')[4],
                                                SpecialReportMemo= specialReportRows.
                                                                    Where(s => s.Split('|')[1].Equals(r.Split('|')[1])).
                                                                    FirstOrDefault()

                                            });
            }

            return l;
        }

        /// <summary>
        /// 単位のASCII文字変換行
        /// </summary>
        private class AsciiUnitMasterRow
        {
            public string AsciiUnit;
            public List<string> DisplayUnitExample;
            public string ISO;
            public string Bikou;
            public string SpecialReportMemo;

            public AsciiUnitMasterRow()
            {
                this.DisplayUnitExample = new List<string>();
            }

            //ini設定に追加する内容を返却する
            public List<string> GetIniSettings()
            {
                List<string> iniSettings = new List<string>();
                
                foreach (var u in DisplayUnitExample)
                {
                    String setting;

                    //ローカル単位,ASCII単位
                    setting=string.Concat(u.Trim(), ",", this.AsciiUnit);

                    //JLACコード指定 備考、特記事項から抽出
                    foreach (Match m in Regex.Matches(this.Bikou, @"[0-9 A-Z?]{17}"))
                    {
                        setting = string.Concat(setting, ",", m);
                    }

                    if (!string.IsNullOrEmpty(SpecialReportMemo))
                    {
                        foreach(Match m in Regex.Matches(this.SpecialReportMemo, @"[0-9 A-Z?]{17}"))
                        {
                            setting = string.Concat(setting, ",", m);
                        }
                    }

                    iniSettings.Add(setting);
                }



                return iniSettings;
            }
        }
 
    }
}
