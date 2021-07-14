using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CrawlReport.Models;
using PuppeteerSharp;
using System.Text.RegularExpressions;
using System.IO;

namespace CrawlReport.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            List<QuestionsModel> questions = new List<QuestionsModel>();
            Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });


            string path = @"MyFile.txt";
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.Create(path).Dispose();
            }

            using (TextWriter tw = new StreamWriter(path))
            {
                for (int i = 1; i <= 7; i++)
                {
                    string fullUrl = "https://stackoverflow.com/questions?tab=votes&page=" + i;

                    Page page = await browser.NewPageAsync();
                    await page.GoToAsync(fullUrl);

                    var hrefJs = @"Array.from($('div.question-summary  h3  a')).map(a => a.href);";
                    var hrefs = await page.EvaluateExpressionAsync<string[]>(hrefJs);

                    var titleJs = @"Array.from($('div.question-summary  h3  a')).map(a => a.text);";
                    var titles = await page.EvaluateExpressionAsync<string[]>(titleJs);

                    var descJs = @"Array.from($('div.question-summary  div.excerpt')).map(a => a.innerText);";
                    var desc = await page.EvaluateExpressionAsync<string[]>(descJs);


                    for (int j = 0; j < titles.Length; j++)
                    {
                        tw.WriteLine(titles[j]);
                        tw.WriteLine(desc[j]);
                        tw.WriteLine();
                        tw.WriteLine();
                        questions.Add(new QuestionsModel { link = hrefs[j].Replace("https://stackoverflow.com/questions/", ""), title = titles[j], description = desc[j] });
                    }
                }
            }
            return View(questions.Take(100));
        }

        [Route("answer/{id}/{link}")]
        public async Task<IActionResult> Answer(string id,string link)
        {
            List<QuestionsModel> questions = new List<QuestionsModel>();
            Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            string fullUrl = "https://stackoverflow.com/questions/"+ id + "/"+link;

            Page page = await browser.NewPageAsync();
            await page.GoToAsync(fullUrl);

            var titleJs = @"$('div#question-header  h1  a').text()";
            var title = await page.EvaluateExpressionAsync<string>(titleJs);
            ViewBag.QueTitle = title;

            var questionJs = @"document.querySelector('#question').innerHTML";
            var que = await page.EvaluateExpressionAsync<string>(questionJs);
            ViewBag.Question = que;

            var answerJs = @"document.querySelector('#answers').innerHTML";
            var answer = await page.EvaluateExpressionAsync<string>(answerJs);
            ViewBag.Answers = answer;

            return View();
        }


        [Route("report")]
        public async Task<IActionResult> Report()
        {
            string text = await System.IO.File.ReadAllTextAsync(@"MyFile.txt");
            string[] words = text.Split(new char[] {
            ' '
        }, StringSplitOptions.RemoveEmptyEntries);
            var word_query = (from string word in words orderby word select word).Distinct();
            string[] result = word_query.ToArray();

            List<WordsReportModel> repo = new List<WordsReportModel>();

            string wordsReportPath = @"WordsReport.txt";
            if (!System.IO.File.Exists(wordsReportPath))
            {
                System.IO.File.Create(wordsReportPath).Dispose();
            }

            foreach (string word in result)
            {
                int count = 0;
                int i = 0;
                while ((i = text.IndexOf(word, i)) != -1)
                {
                    i += word.Length;
                    count++;
                }
                if (word.Length > 1)
                {
                   
                    repo.Add(new WordsReportModel { Word = word, Count = count });
                }
            }

            repo = repo.OrderByDescending(x => x.Count).ToList();
            using (TextWriter tw = new StreamWriter(wordsReportPath))
            {
                foreach (var v in repo)
                {
                    tw.WriteLine(v.Word + ": " + v.Count);
                }
            }

            ViewBag.Repo = repo;
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
