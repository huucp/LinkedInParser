using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;
using NBug;
using NLog;
using MyConnect;
using MyConnect.SQLServer;
using MyUtility;
using Microsoft.Vbe.Interop;
using Application = System.Windows.Forms.Application;

namespace LinkedInParser
{
    public partial class Parser : Form
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public Parser()
        {

            InitializeComponent();
            //string content = File.ReadAllText("trung kien thai.html");
            //var parserCore = new ParserCore(content);
            //parserCore.Process();
            //MainProcess("http://vn.linkedin.com/pub/trung-kien-thai/66/b1/946", 10);
            //MainProcess("http://vn.linkedin.com/in/ducdangindochinecounsel?trk=pub-pbmap", 10);

#if DEBUG
            NBug.Settings.Destination1 = "Type=Mail;From=huupc@tosy.com;To=phamchinhhuu@gmail.com;SmtpServer=mail.tosy.com;";
            AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
            Application.ThreadException += Handler.ThreadException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += NBug.Handler.UnobservedTaskException;
#endif
        }
        const string scn = "SQLConnectionName";
        MyGetData mGet = new MyGetData(scn);
        MyExecuteData mExe = new MyExecuteData(scn);
        private string link;
        private int count = 0;
        private int limit;
        private void MainProcess(string first, int l)
        {
            limit = l;

            link = first;


            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += ParseData;
            backgroundWorker.RunWorkerAsync();



        }

        private void UpdateLog(string log)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    LogTextBox.AppendText(log);
                });
            }
            else
            {
                LogTextBox.AppendText(log);
            }
        }

        private void ParseData(object sender, DoWorkEventArgs e)
        {
            while (count < limit)
            {
                if (!ParseDataProcess()) break;
                count++;
            }

            BeginInvoke((MethodInvoker)delegate
            {
                MessageBox.Show("Parse done");
                LimitTextBox.Text = string.Empty;
                LinkTextBox.Text = string.Empty;
                ParseButton.Enabled = true;
            });

        }

        private bool ParseDataProcess()
        {
            if (link == string.Empty)
            {
                UpdateLog("Cannot get next profile. The link is empty.");
                return false;
            }
            UpdateLog("Parse link: " + link + ".\n");
            string content = GetWebContent(link);
            var parserCore = new ParserCore(content);
            var profile = parserCore.Process();



            if (!IsExisted(link))
            {
                WriteToDB(profile, link);
                UpdateLog("This profile is not exist in DB. Write to DB done!\n");
            }
            else
            {
                UpdateLog("This profile is exist in DB.\n");
            }
            int linkCount = 0;
            foreach (var profileLink in profile.NextProfile)
            {
                linkCount++;
                if (!IsExisted(profileLink))
                {
                    link = profileLink;
                    return true;
                }
                if (linkCount == profile.NextProfile.Count())
                {
                    return false;
                }
            }
            link = string.Empty;
            return true;
        }

        private bool IsExisted(string link)
        {
            link = link.Replace("'", "\"");
            string temp = "select * from UserDB where Link='" + link + "'";
            var tbl = mGet.GetDataTable_Query("select * from UserDB where Link='" + link + "'");
            if (tbl.Rows.Count == 0) return false;
            return true;
        }

        private void WriteToDB(LinkedInProfile profile, string link)
        {
            string username = profile.Username.Replace("'", "\"");
            string position = profile.Position.Replace("'", "\"");
            string summary = profile.Summary.Replace("'", "\"");
            string exp = profile.Experience.Replace("'", "\"");
            string language = profile.Language.Count > 0 ? profile.Language[0] : string.Empty;
            for (int i = 1; i < profile.Language.Count; i++)
            {
                language += ";" + profile.Language[i].Replace("'", "\"");
            }
            string skill = profile.SkillAndExpertise.Count > 0 ? profile.SkillAndExpertise[0] : string.Empty;
            for (int i = 1; i < profile.SkillAndExpertise.Count; i++)
            {
                skill += ";" + profile.SkillAndExpertise[i].Replace("'", "\"");
            }
            link = link.Replace("'", "\"");
            mExe.ExecQuery("INSERT INTO UserDB (Username,Position,Description,Experience,Language,Skill,Link) " + "VALUES" + " (" +
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
            string content = string.Empty;
            using (var client = new WebClient())
            {
                content = client.DownloadString(url);
                //File.WriteAllText("test.html", content);
            }
            return content;
            //WebRequest request = WebRequest.Create(url);
            //request.Method = "GET";
            //WebResponse response = request.GetResponse();
            //Stream stream = response.GetResponseStream();
            //if (stream != null)
            //{
            //    var reader = new StreamReader(stream);
            //    string content = reader.ReadToEnd();
            //    reader.Close();
            //    response.Close();
            //    File.WriteAllText("test.html",content);
            //    return content;
            //}
            //else
            //{
            //    return string.Empty;
            //}
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            ParseButton.Enabled = false;
            count = 0;
            link = string.Empty;
            string url = LinkTextBox.Text;
            int Limit = 1;
            if (!Int32.TryParse(LimitTextBox.Text, out Limit))
            {
                MessageBox.Show("The limit must be a number");
                return;
            }
            MainProcess(url, Limit);
        }
    }
}
