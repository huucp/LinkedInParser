using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NLog;

namespace LinkedInParser
{
    public class ParserCore
    {
        private string Content { get; set; }
        public ParserCore(string htmlContent)
        {
            Content = htmlContent;
        }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        // <span class="full-name">: tag for user full name
        // <p class="headline-title title" style="display:block">: tag for position
        // <p class=" description summary">: tag for description
        // <div class="postitle">: tag for begin experience
        // <li class="competency language">: tag for language
        // <li class="competency show-bean  ">: tag for skill
        // <li class="with-photo">: tag for link profile
        public LinkedInProfile Process()
        {
            var profile = new LinkedInProfile();
            logger.Info("Start parse HTML");

            // Get username       
            logger.Info("Get username");
            string[] usernameSplitWord = Regex.Split(Content, "<span class=\"full-name\">");
            profile.Username = ExtractUsername(usernameSplitWord[1]);
            logger.Info(profile.Username);

            // Get position 
            logger.Info("Get position");
            string[] postionSplitWord = Regex.Split(Content, "<p class=\"headline-title title\" style=\"display:block\">");
            profile.Position = ExtractPosition(postionSplitWord[1]);
            logger.Info(profile.Position);

            // Get summary
            logger.Info("Get summary");
            string[] summarySplitWord = Regex.Split(Content, "<p class=\" description summary\">");
            if (summarySplitWord.Count() > 1)
            {
                profile.Summary = ExtractSummary(summarySplitWord[1]);
            }
            else
            {
                profile.Summary = string.Empty;
            }

            // Get experience
            logger.Info("Get experience");
            string[] expSplitWord = Regex.Split(Content, "<div class=\"postitle\">");
            profile.Experience = ExtractExperience(expSplitWord[1]);
            logger.Info(profile.Experience);

            // Get language
            logger.Info("Get language");
            string[] langSplitWord = Regex.Split(Content, "<li class=\"competency language\">");
            for (int i = 1; i < langSplitWord.Count(); i++)
            {
                string lang = ExtractLanguage(langSplitWord[i]);
                profile.Language.Add(lang);
                logger.Info(lang);
            }

            // Get skill and expertise
            logger.Info("Get skill and expertise");
            string[] skillSplitWord = Regex.Split(Content, "<li class=\"competency show-bean  \">");
            for (int i = 1; i < skillSplitWord.Count(); i++)
            {
                var content = Regex.Split(skillSplitWord[i], "</span>")[0];
                string skill = ExtractSkillAndExpertise(content);
                profile.SkillAndExpertise.Add(skill);
                logger.Info(skill);
            }
            if (Content.Contains("<li class=\"competency show-bean  extra-skill\">"))
            {
                string[] extraSkillSplitWord = Regex.Split(Content, "<li class=\"competency show-bean  extra-skill\">");
                for (int i = 1; i < extraSkillSplitWord.Count(); i++)
                {
                    var content = Regex.Split(extraSkillSplitWord[i], "</span>")[0];
                    string skill = ExtractSkillAndExpertise(content);
                    profile.SkillAndExpertise.Add(skill);
                    logger.Info(skill);
                }
            }


            // Get next profile link
            logger.Info("Get next profile");
            string[] nextSplitWord = Regex.Split(Content, "<li class=\"with-photo\">");
            if (nextSplitWord.Count() > 1)
            {
                profile.NextProfile = ExtractNextProfile(nextSplitWord[1]);
            }
            else
            {
                profile.NextProfile = string.Empty;
            }
            logger.Info(profile.NextProfile);

            logger.Info("End parse HTML");
            return profile;
        }

        private string ExtractNextProfile(string content)
        {
            string[] s1 = Regex.Split(content, "<a href=\"");
            string url = Regex.Split(s1[1], "\">")[0].Trim();
            return url;
        }

        private string ExtractSkillAndExpertise(string content)
        {
            string skill = string.Empty;
            string[] s1;
            if (content.Contains("a href"))
            {
                s1 = Regex.Split(content, "</a>");
                skill = Regex.Split(s1[0], ">")[2];
            }
            else
            {
                s1 = Regex.Split(content, ">");
                skill = Regex.Split(s1[1], "<")[0];
            }

            return WebUtility.HtmlDecode(skill).Trim();
        }

        private string ExtractLanguage(string content)
        {
            var languageSplit = Regex.Split(content, "<h3>");
            var lang = Regex.Split(languageSplit[1], "</h3>")[0].Trim();
            return WebUtility.HtmlDecode(lang).Trim();
        }

        private string ExtractUsername(string content)
        {
            string[] words = Regex.Split(content, "</span> <span class=\"family-name\">");
            string[] givenName = Regex.Split(words[0], "<span class=\"given-name\">");
            string[] familyName = Regex.Split(words[1], "</span></span>");
            return WebUtility.HtmlDecode(string.Format("{0} {1}", givenName[1], familyName[0])).Trim();
        }
        private string ExtractPosition(string content)
        {
            string temp = WebUtility.HtmlDecode(Regex.Split(content, "</p>")[0]);
            string[] parts = temp.Split(new string[] { "\n" }, StringSplitOptions.None);
            return parts[3].Trim();
        }

        private string ExtractSummary(string content)
        {
            string[] description = Regex.Split(content, "</p>");
            string s = description[0].Replace("<br>", "");
            return WebUtility.HtmlDecode(s).Trim();
        }

        private string ExtractExperience(string content)
        {
            string[] titleSplitWord = Regex.Split(content, "<span class=\"title\">");
            string title = Regex.Split(titleSplitWord[1], "</span>")[0].Trim();

            string[] orgSplitWord = Regex.Split(content, "<span class=\"org summary\">");
            string org = Regex.Split(orgSplitWord[1], "</span>")[0].Trim();

            string[] orgDetailSplitWord = Regex.Split(content, "<p class=\"orgstats organization-details current-position\">");
            string orgDetail = string.Empty;
            if (orgDetailSplitWord.Count() > 1)
            {
                orgDetail = Regex.Split(orgDetailSplitWord[1], "</p>")[0].Trim();
            }

            string[] periodSplitWord = Regex.Split(content, "<p class=\"period\">");
            string[] timeSplitWord = Regex.Split(periodSplitWord[1], "</abbr>");
            string firstTime = Regex.Split(timeSplitWord[0], ">")[1].Trim();
            string secondTime = Regex.Split(timeSplitWord[1], ">")[1].Trim();
            string duration = Regex.Split(periodSplitWord[1], "</span>")[1].Trim();

            return WebUtility.HtmlDecode(title + " " + org + " " + orgDetail + " " + firstTime + "-" + secondTime + " " + duration);
        }
    }

    public class LinkedInProfile
    {
        public string Username { get; set; }
        public string Position { get; set; }
        public string Summary { get; set; }
        public string Experience { get; set; }
        public List<string> Language = new List<string>();
        public List<string> SkillAndExpertise = new List<string>();
        public string NextProfile { get; set; }
    }
}
