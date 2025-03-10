﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Calendar = Ical.Net.Calendar;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.IO;
using System.Windows;
using HtmlAgilityPack;
using Microsoft.Win32;

namespace TimetableGrabber___SIT.API
{
    internal class IN4SIT
    {
        private MainWindow mainWindow;

        IWebDriver webDriverInstance = null;

        private const string LOGIN_URL = "https://fs.singaporetech.edu.sg/adfs/ls/idpinitiatedsignon.asmx?loginToRp=https://in4sit.singaporetech.edu.sg/psp/CSSISSTD/";

        public IN4SIT(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        private async Task<bool> CreateChromeInstance()
        {
            try
            {
                await mainWindow.Log("Creating Chrome...");

                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;

                ChromeOptions chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("headless");
                chromeOptions.AddArgument("--window-size=1920,1080");
                chromeOptions.AddArgument("--disable-extensions");

                webDriverInstance = new ChromeDriver(chromeDriverService, chromeOptions);
                webDriverInstance.Manage().Window.Maximize();

                webDriverInstance.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

                await mainWindow.Log("Created Chrome...");

                return true;
            }
            catch (Exception ex)
            {
                await mainWindow.Log(ex.Message);
                return false;
            }
        }

        private async Task<bool> CloseChromeInstance()
        {
            try
            {
                await mainWindow.Log("Closing Chrome...");

                webDriverInstance.Quit();

                await mainWindow.Log("Closed Chrome...");

                return true;
            }
            catch (Exception ex)
            {
                await mainWindow.Log(ex.Message);
                return false;
            }
        }

        public async Task<bool> Start(string email, string password)
        {
            try
            {
                await CreateChromeInstance();

                #region LOGIN AND GO TO WEEKLY SCHEDULE LIST
                await mainWindow.SetStatus("Accessing IN4SIT...");

                await mainWindow.Log("Navigating to IN4SIT...");
                webDriverInstance.Navigate().GoToUrl(LOGIN_URL);
                await mainWindow.Log("Navigated to IN4SIT...");

                await mainWindow.Log("Entering username...");
                IWebElement usernameTextBox = webDriverInstance.FindElement(By.CssSelector("#userNameInput"));
                usernameTextBox.SendKeys(email);
                await mainWindow.Log("Entered username...");

                await mainWindow.Log("Entering password...");
                IWebElement passwordTextBox = webDriverInstance.FindElement(By.CssSelector("#passwordInput"));
                passwordTextBox.SendKeys(password);
                await mainWindow.Log("Entered password...");

                await mainWindow.Log("Clicking \"Sign in\"...");
                IWebElement signInButton = webDriverInstance.FindElement(By.CssSelector("#submitButton"));
                signInButton.Click();
                await mainWindow.Log("Clicked \"Sign in\"...");

                await mainWindow.Log("Waiting for \"Course Management\"...");
                await Task.Delay(5000);

                await mainWindow.Log("Clicking \"Course Management\"...");
                IWebElement courseManagement = webDriverInstance.FindElement(By.XPath("//div[contains(@onclick, 'https://in4sit.singaporetech.edu.sg/psc/CSSISSTD_newwin/EMPLOYEE/SA/c/NUI_FRAMEWORK.PT_AGSTARTPAGE_NUI.GBL?CONTEXTIDPARAMS=TEMPLATE_ID%3aPTPPNAVCOL&scname=ADMN_MODULE_MANAGEMENT&PTPPB_GROUPLET_ID=N_SR_MODULE_MATTERS&CRefName=ADMN_NAVCOLL_18&AJAXTRANSFER=Y')]"));
                courseManagement.Click();
                await mainWindow.Log("Clicked \"Course Management\"...");

                await mainWindow.Log("Waiting for \"Weekly Schedule\"...");
                await Task.Delay(5000);

                await mainWindow.Log("Clicking \"Weekly Schedule\"...");
                IWebElement weeklySchedule = webDriverInstance.FindElement(By.CssSelector(@"#PTGP_STEP_DVW_PTGP_STEP_LABEL\$1"));
                weeklySchedule.Click();
                await mainWindow.Log("Clicked \"Weekly Schedule\"...");

                await mainWindow.Log("Waiting...");
                await Task.Delay(5000);

                await mainWindow.Log("Switching frame...");
                webDriverInstance.SwitchTo().Frame("main_target_win0");
                await mainWindow.Log("Switched frame...");

                await mainWindow.Log("Clicking \"List View\"...");
                IWebElement listViewRadioButton = webDriverInstance.FindElement(By.CssSelector(@"#DERIVED_REGFRM1_SSR_SCHED_FORMAT\$258\$"));
                listViewRadioButton.Click();
                await mainWindow.Log("Clicked \"List View\"...");

                await mainWindow.Log("Waiting...");
                await Task.Delay(5000);
                #endregion

                #region TERM SELECTOR IF PROMPTED
                try
                {
                    IWebElement termSelectorText = webDriverInstance.FindElement(By.CssSelector(@"#win0divSSR_DUMMY_RECV1GP\$0"));
                    
                    string termText1 = webDriverInstance.FindElement(By.CssSelector(@"#TERM_CAR\$0")).Text;
                    string termText2 = webDriverInstance.FindElement(By.CssSelector(@"#TERM_CAR\$1")).Text;


                    await mainWindow.Log("Please select one of the option(s)...");
                    string TermIdentifier = Application.Current.Dispatcher.Invoke(new Func<String>(() => { return mainWindow.OpenSelectionPrompt(termText1, termText2); }));
                    IWebElement termViewRadioButton = webDriverInstance.FindElement(By.CssSelector(@"#SSR_DUMMY_RECV1\$sels\$" + TermIdentifier + @"\$\$0"));
                    termViewRadioButton.Click();
                    
                    IWebElement termSubmitButton = webDriverInstance.FindElement(By.CssSelector("#DERIVED_SSS_SCT_SSR_PB_GO"));
                    termSubmitButton.Click();

                    await mainWindow.Log("Waiting...");
                    await Task.Delay(5000);
                }
                catch (NoSuchElementException exc)
                {
                    await mainWindow.Log("No Term Selection found...");
                    await mainWindow.Log(exc.Message);
                }
                #endregion

                #region UNCHECKING FILTERS
                await mainWindow.Log("Unchecking \"Show Dropped Classes\"...");
                IWebElement showDroppedClassesCheckBox = webDriverInstance.FindElement(By.CssSelector("#DERIVED_REGFRM1_SA_STUDYLIST_D"));
                showDroppedClassesCheckBox.Click();
                await mainWindow.Log("Unchecked \"Show Dropped Classes\"...");

                await mainWindow.Log("Unchecking \"Show Waitlisted Classes\"...");
                IWebElement showWaitlistedClassesCheckBox = webDriverInstance.FindElement(By.CssSelector("#DERIVED_REGFRM1_SA_STUDYLIST_W"));
                showWaitlistedClassesCheckBox.Click();
                await mainWindow.Log("Unchecked \"Show Waitlisted Classes\"...");

                await mainWindow.Log("Clicking \"Filter\"...");
                IWebElement filterButton = webDriverInstance.FindElement(By.CssSelector(@"#DERIVED_REGFRM1_SA_STUDYLIST_SHOW\$14\$"));
                filterButton.Click();
                await mainWindow.Log("Clicked \"Filter\"...");

                await mainWindow.Log("Waiting...");
                await Task.Delay(5000);
                #endregion

                #region SCRAPE WEEKLY SCHEDULE LIST
                await mainWindow.SetStatus("Scraping schedule...");

                await mainWindow.Log("Scraping schedule...");

                List<Course> timetable = new List<Course>();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(webDriverInstance.PageSource);

                await CloseChromeInstance();

                var courses = htmlDocument.DocumentNode.SelectNodes("//td[@class=\"PAGROUPDIVIDER\"]");
                foreach (var course in courses)
                {
                    string name = course.InnerHtml.Split(new string[] { " - " }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
                    System.Diagnostics.Debug.WriteLine(name);

                    HtmlNode tableNode = course.ParentNode.ParentNode.SelectSingleNode("tr[2]/td/table/tbody/tr[3]/td[2]/div/table/tbody/tr/td/table/tbody");

                    string section = "";
                    string component = "";
                    string location = "";

                    foreach (HtmlNode tableRow in tableNode.SelectNodes("tr"))
                    {
                        if (tableRow.Attributes["valign"] == null)
                            continue;
                        else
                        {
                            HtmlNodeCollection tableColumns = tableRow.SelectNodes("td");

                            if (tableColumns[1].SelectSingleNode("div/span/a") != null)
                                section = tableColumns[1].SelectSingleNode("div/span/a").InnerHtml;

                            if (tableColumns[2].SelectSingleNode("div/span").InnerHtml != "&nbsp;")
                                component = tableColumns[2].SelectSingleNode("div/span").InnerHtml;

                            location = tableColumns[4].SelectSingleNode("div/span").InnerHtml;

                            string rawDate = tableColumns[6].SelectSingleNode("div/span").InnerHtml;
                            Regex dateRegex = new Regex(@"([0-9]{2}/[0-9]{2}/[0-9]{4}) - ([0-9]{2}/[0-9]{2}/[0-9]{4})", RegexOptions.Singleline);
                            Match dateRegexMatch = dateRegex.Match(rawDate);

                            string rawTime = tableColumns[3].SelectSingleNode("div/span").InnerHtml;
                            Regex timeRegex = new Regex(@".. ([0-9]+:[0-9]{2}[A-Z]*) - ([0-9]+:[0-9]{2}[A-Z]*)", RegexOptions.Singleline);
                            Match timeRegexMatch = timeRegex.Match(rawTime);

                            string formattedStartDateTime = string.Format("{0} {1}:00", dateRegexMatch.Groups[1], timeRegexMatch.Groups[1]);
                            string formattedEndDateTime = string.Format("{0} {1}:00", dateRegexMatch.Groups[2], timeRegexMatch.Groups[2]);

                            DateTime startDateTime = DateTime.Now, endDateTime = DateTime.Now;
                            string[] dateTimeFormatStrings = new string[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy hh:mm:ss" };
                            if (!DateTime.TryParseExact(formattedStartDateTime, dateTimeFormatStrings, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out startDateTime))
                            {
                                await mainWindow.Log("Please change your Window's regional format to English (Singapore)!");
                            }

                            if (!DateTime.TryParseExact(formattedEndDateTime, dateTimeFormatStrings, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out endDateTime))
                            {
                                await mainWindow.Log("Please change your Window's regional format to English (Singapore)!");
                            }

                            timetable.Add(new Course(name, section, component, startDateTime, endDateTime, location));
                        }
                    }
                }

                await mainWindow.Log("Scraped schedule...");
                #endregion

                #region EXPORT TO ICS
                await mainWindow.SetStatus("Exporting schedule...");

                await mainWindow.Log("Exporting schedule...");

                Calendar exportCalendar = new Calendar();

                foreach (Course course in timetable)
                {
                    CalendarEvent calendarEvent = new CalendarEvent
                    {
                        Summary = string.Format("{0} - {1} - {2}", course.Name, course.Section, course.Component),
                        Start = new CalDateTime(course.StartDateTime),
                        End = new CalDateTime(course.EndDateTime),
                        Location = course.Location
                    };

                    exportCalendar.Events.Add(calendarEvent);
                }

                CalendarSerializer calendarSerializer = new CalendarSerializer();
                string serializedCalendar = calendarSerializer.SerializeToString(exportCalendar);

                string fileName = string.Format("{0}_timetable.ics", DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss"));

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "TimetableGrabber - SIT",
                    Filter = "Calendar File|*.ics",
                    FileName = fileName
                };

                bool? saveFile = saveFileDialog.ShowDialog();
                if (!string.IsNullOrWhiteSpace(saveFileDialog.FileName) && (bool)saveFile)
                {
                    File.WriteAllText(saveFileDialog.FileName, serializedCalendar);

                    await mainWindow.Log(string.Format("Exported schedule to {0}...", saveFileDialog.FileName));
                }
                else
                    await mainWindow.Log("Schedule was not exported...");
                #endregion

                await mainWindow.SetStatus("Done...");

                return true;
            }
            catch (Exception ex)
            {
                #region CHECK IF "LOGIN FAILURE"
                try
                {
                    IWebElement loginError = webDriverInstance.FindElement(By.CssSelector("#errorText"));
                    await mainWindow.Log("Error Output: " + loginError.Text);
                    await CloseChromeInstance();
                    return false;
                }
                catch (Exception exc)
                {
                    if (!(exc is NoSuchElementException))
                    {
                        await mainWindow.Log(exc.Message);
                    }
                }
                #endregion

                #region CHECK IF "NOT REGISTERED FOR ANY CLASS"
                try
                {
                    IWebElement noClassRegisteredText = webDriverInstance.FindElement(By.CssSelector("#DERIVED_REGFRM1_SS_MESSAGE_LONG"));
                    String noClassContent = noClassRegisteredText.Text;
                    await CloseChromeInstance();
                    if (String.Equals(noClassContent, "You are not registered for classes in this term."))
                    {
                        await mainWindow.Log("No registered classes found...");
                        MessageBox.Show("No registered classes found!", "TimetableGrabber - SIT", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                    await mainWindow.Log("Content Output: "+noClassContent);
                    return false;
                }
                catch (Exception exc)
                {
                    if (!(exc is NoSuchElementException))
                    {
                        await mainWindow.Log(exc.Message);
                    }
                }
                #endregion

                await CloseChromeInstance();
                await mainWindow.Log(ex.Message);
                return false;
            }
        }
    }
}
