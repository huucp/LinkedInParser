using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using NLog;
using MyConnect;
using MyConnect.SQLServer;
using MyUtility;
using Microsoft.Vbe.Interop;

namespace LinkedInParser
{
    public partial class Parser : Form
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public Parser()
        {



            //string content = File.ReadAllText("luu vinh.html");
            //var parserCore = new ParserCore(content);
            //parserCore.Process();
            MainProcess("http://vn.linkedin.com/pub/trung-kien-thai/66/b1/946", 10);
        }
        const string scn = "SQLConnectionName";
        MyGetData mGet = new MyGetData(scn);
        MyExecuteData mExe = new MyExecuteData(scn);
        private void MainProcess(string first, int limit)
        {

            int count = 0;
            string link = first;
            while (count < limit)
            {
                string content = GetWebContent(link);
                var parserCore = new ParserCore(content);
                var profile = parserCore.Process();
                

                if (!IsExisted(link))
                {
                    WriteToDB(profile,link);
                }
                link = profile.NextProfile;
                count++;
            }
        }

        private bool IsExisted(string link)
        {
            link = link.Replace("'", "\"");
            string temp = "select * from UserDB where Link='" + link + "'";
            var tbl = mGet.GetDataTable_Query("select * from UserDB where Link='" + link + "'");
            if (tbl.Rows.Count == 0) return false;
            return true;
        }

        private void WriteToDB(LinkedInProfile profile,string link)
        {
            string username = profile.Username.Replace("'", "\"");
            string position = profile.Position.Replace("'", "\"");
            string summary = profile.Summary.Replace("'", "\"");
            string exp = profile.Experience.Replace("'", "\"");
            string language = string.Empty;
            foreach (var l in profile.Language)
            {
                language += ";" + l.Replace("'", "\"");
            }
            string skill = string.Empty;
            foreach (var s in profile.SkillAndExpertise)
            {
                skill = ";" + s.Replace("'", "\"");
            }
            link = link.Replace("'", "\"");
            mExe.ExecQuery("INSERT INTO UserDB (Username,Position,Description,Experience,Language,Skill,Link) " +"VALUES" +" (" +
                           "'" + username + "'," +
                           "'" + position + "'," +
                           "'" + summary + "'," +
                           "'" + exp + "'," +
                           "'" + language + "'," +
                           "'" + skill + "'," +
                           "'" + link + "'" +
                           ")");
        }

        private string GetWebContent(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            if (stream != null)
            {
                var reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                reader.Close();
                response.Close();
                return content;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
