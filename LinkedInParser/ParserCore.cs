using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace LinkedInParser
{
    public class ParserCore
    {
        private string Content { get; set; }
        public ParserCore(string htmlContent)
        {
            Content = htmlContent;
        }

        // <span class="full-name">: tag for user full name
        // <p class="headline-title title" style="display:block">: tag for position
        // <p class=" description summary">: tag for description
        // <div class="postitle">: tag for begin experience
        // <li class="competency language">: tag for language
        // <li class="competency show-bean  ">: tag for skill
        public LinkedInProfile Process()
        {
            var profile = new LinkedInProfile();
            
            // Get username            
            string[] usernameSplitWord = Regex.Split(Content, "<span class=\"full-name\">");
            profile.Username = ExtractUsername(usernameSplitWord[1]);

            // Get position            
            string[] postionSplitWord = Regex.Split(Content, "<p class=\"headline-title title\" style=\"display:block\">");
            profile.Position = ExtractPosition(postionSplitWord[1]);
            
            return profile;
        }
        private string ExtractUsername(string content)
        {
            string[] words = Regex.Split(content, "</span> <span class=\"family-name\">");
            string[] givenName = Regex.Split(words[0], "<span class=\"given-name\">");
            string[] familyName = Regex.Split(words[1], "</span></span>");
            return WebUtility.HtmlDecode(string.Format("{0} {1}", givenName[1], familyName[0]));
        }
        private string ExtractPosition(string content)
        {
            string temp = WebUtility.HtmlDecode(Regex.Split(content, "</p>")[0]);
            string[] parts = temp.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            return parts[3].Trim();
        }
        
    }

    public class LinkedInProfile
    {
        public string Username { get; set; }
        public string Position { get; set; }
        public string Summary { get; set; }
        public string Experience { get; set; }
        public List<string> Language { get; set; }
        public List<string> SkillAndExpertise { get; set; }
        public string NextProfile { get; set; }
    }
}
