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
            if (postionSplitWord.Count() > 1)
            {
                string position = ExtractPosition(postionSplitWord[1]);
                string[] positionSplit = Regex.Split(position, " at ");
                profile.Position = positionSplit[0];
                profile.Company = positionSplit.Count() > 1 ? positionSplit[1] : string.Empty;
            }
            else
            {
                profile.Position = string.Empty;
                profile.Company = string.Empty;
            }
            logger.Info(profile.Position);

            // Get summary
            logger.Info("Get summary");
            string[] summarySplitWord = Regex.Split(Content, "<p class=\" description summary\">");
            profile.Summary = summarySplitWord.Count() > 1 ? ExtractSummary(summarySplitWord[1]) : string.Empty;

            // Get experience
            logger.Info("Get experience");
            string[] expSplitWord = Regex.Split(Content, "<div class=\"postitle\">");
            profile.ExperienceCompany = string.Empty;
            profile.ExperienceCompanyLocation = string.Empty;
            profile.ExperienceCompanyType = string.Empty;
            profile.ExperienceCompanySize = string.Empty;
            profile.ExperienceCompanyBusinessSector = string.Empty;
            profile.ExperiencePeriod = string.Empty;
            profile.ExperiencePosition = string.Empty;
            if (expSplitWord.Count() > 1) ExtractExperience(expSplitWord[1], ref profile);
            //logger.Info(profile.Experience);

            // Get language
            logger.Info("Get language");
            string[] langSplitWord = Regex.Split(Content, "<li class=\"competency language\">");
            if (langSplitWord.Count() > 1)
            {
                for (int i = 1; i < langSplitWord.Count(); i++)
                {
                    string lang = ExtractLanguage(langSplitWord[i]);
                    profile.Language.Add(lang);
                    logger.Info(lang);
                }
            }

            // Get skill and expertise
            logger.Info("Get skill and expertise");
            if (Content.Contains("<li class=\"competency show-bean  \">"))
            {
                string[] skillSplitWord = Regex.Split(Content, "<li class=\"competency show-bean  \">");
                for (int i = 1; i < skillSplitWord.Count(); i++)
                {
                    var content = Regex.Split(skillSplitWord[i], "</span>")[0];
                    string skill = ExtractSkillAndExpertise(content);
                    profile.SkillAndExpertise.Add(skill);
                    logger.Info(skill);
                }
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
                for (int i = 1; i < nextSplitWord.Count(); i++)
                {
                    string profileLink = ExtractNextProfile(nextSplitWord[i]);
                    profile.NextProfile.Add(profileLink);
                    logger.Info(profileLink);
                }
            }

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
            string givenName, familyName;
            if (words.Count() > 1)
            {
                givenName = Regex.Split(words[0], "<span class=\"given-name\">")[1];
                familyName = Regex.Split(words[1], "</span></span>")[0];
                return WebUtility.HtmlDecode(string.Format("{0} {1}", givenName, familyName)).Trim();
            }
            else
            {
                words = Regex.Split(content, "</span><span class=\"given-name\">");
                familyName = Regex.Split(words[0], "<span class=\"family-name\">")[1];
                givenName = Regex.Split(words[1], "</span></span>")[0];
                return WebUtility.HtmlDecode(string.Format("{0} {1}", familyName, givenName)).Trim();
            }
        }
        private string ExtractPosition(string content)
        {
            string temp = WebUtility.HtmlDecode(Regex.Split(content, "</p>")[0]);
            string[] parts = temp.Split(new string[] { "\n" }, StringSplitOptions.None);
            int index = (parts.Count() - 1) / 2;
            return parts[index].Trim();
        }

        private string ExtractSummary(string content)
        {
            string[] description = Regex.Split(content, "</p>");
            string s = description[0].Replace("<br>", "");
            return WebUtility.HtmlDecode(s).Trim();
        }

        private void ExtractExperience(string content, ref LinkedInProfile profile)
        {
            string[] titleSplitWord = Regex.Split(content, "<span class=\"title\">");
            string title = Regex.Split(titleSplitWord[1], "</span>")[0].Trim();
            profile.ExperiencePosition = WebUtility.HtmlDecode(title);
            logger.Info(title);

            string org = string.Empty;
            if (content.Contains("<span class=\"org summary\">"))
            {
                string[] orgSplitWord = Regex.Split(content, "<span class=\"org summary\">");
                org = Regex.Split(orgSplitWord[1], "</span>")[0].Trim();
                profile.ExperienceCompany = WebUtility.HtmlDecode(org);
                logger.Info(org);
            }

            string[] orgDetailSplitWord = Regex.Split(content, "<p class=\"orgstats organization-details");
            string orgDetail = string.Empty;
            if (orgDetailSplitWord.Count() > 1)
            {
                string[] orgDetailTemp = Regex.Split(orgDetailSplitWord[1], "position\">");
                orgDetail = Regex.Split(orgDetailTemp[1], "</p>")[0].Trim();
                //profile.ExperienceCompanyDetail = WebUtility.HtmlDecode(orgDetail);
                string[] orgDetailSplit = Regex.Split(orgDetail, ";");
                switch (orgDetailSplit.Count())
                {
                    case 1:
                        profile.ExperienceCompanyBusinessSector = WebUtility.HtmlDecode(orgDetailSplit[0]).Trim();
                        break;
                    case 4:
                        profile.ExperienceCompanyType = WebUtility.HtmlDecode(orgDetailSplit[0]).Trim();
                        profile.ExperienceCompanySize = WebUtility.HtmlDecode(orgDetailSplit[1]).Trim();
                        profile.ExperienceCompanyBusinessSector = WebUtility.HtmlDecode(orgDetailSplit[3]).Trim();
                        break;
                    case 3:
                        profile.ExperienceCompanyType = WebUtility.HtmlDecode(orgDetailSplit[0]).Trim();
                        profile.ExperienceCompanySize = WebUtility.HtmlDecode(orgDetailSplit[1]).Trim();
                        profile.ExperienceCompanyBusinessSector = WebUtility.HtmlDecode(orgDetailSplit[2]).Trim();
                        break;
                }
                logger.Info(orgDetail);
            }

            string[] periodSplitWord = new string[] { };
            if (content.Contains("<p class=\"period\">"))
            {
                periodSplitWord = Regex.Split(content, "<p class=\"period\">");
            }
            if (content.Contains("<div class=\"period\">"))
            {
                periodSplitWord = Regex.Split(content, "<div class=\"period\">");
            }

            string[] timeSplitWord = Regex.Split(periodSplitWord[1], "</abbr>");
            if (timeSplitWord.Count() == 1)
            {
                return;
            }
            string firstTime = Regex.Split(timeSplitWord[0], ">")[1].Trim();
            string secondTime = Regex.Split(timeSplitWord[1], ">")[1].Trim();
            string duration = Regex.Split(periodSplitWord[1], "</span>")[1].Trim();
            profile.ExperiencePeriod = WebUtility.HtmlDecode(string.Format(firstTime + " - " + secondTime + " " + duration));
            logger.Info(profile.ExperiencePeriod);

            if (content.Contains("<span class=\"location\">"))
            {
                string[] locationSplitWord = Regex.Split(content, "<span class=\"location\">");
                profile.ExperienceCompanyLocation =
                    WebUtility.HtmlDecode(Regex.Split(locationSplitWord[1], "</span>")[0]);
                logger.Info(profile.ExperienceCompanyLocation);
            }
        }
    }

    public class LinkedInProfile
    {
        public string Username { get; set; }
        public string Position { get; set; }
        public string Company { get; set; }
        public string Summary { get; set; }
        public string ExperienceCompany { get; set; }
        public string ExperiencePosition { get; set; }
        public string ExperienceCompanyType { get; set; }
        public string ExperienceCompanySize { get; set; }
        public string ExperienceCompanyBusinessSector { get; set; }
        public string ExperiencePeriod { get; set; }
        public string ExperienceCompanyLocation { get; set; }
        public List<string> Language = new List<string>();
        public List<string> SkillAndExpertise = new List<string>();
        public List<string> NextProfile = new List<string>();
    }
}
