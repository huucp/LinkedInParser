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

namespace LinkedInParser
{
    public partial class Parser : Form
    {
        public Parser()
        {
            InitializeComponent();

            //string content = File.ReadAllText("trung kien thai.html");
            string content = File.ReadAllText("luu vinh.html");
            var parserCore = new ParserCore(content);
            parserCore.Process();
        }
    }
}
