using CefSharp;
using CefSharp.WinForms;
using CefSharp.WinForms.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace MyCourseX
{
    public partial class WebForm : Form
    {

        private readonly ChromiumWebBrowser browser;

        public WebForm()
        {
            InitializeComponent();

            browser = new ChromiumWebBrowser("wb.mycourse.cn")
            {
                Dock = DockStyle.Fill,
            };

            this.Controls.Add(browser);

            browser.FrameLoadEnd += OnFrameLoadEnd;
            browser.ConsoleMessage += OnConsoleMessage;
        }

        private void ShowMsgBox(List<int> ids, int userId)
        {
            DialogResult result = MessageBox.Show("解析课程列表完成！共 " + ids.Count + " 个未完成课程，点击确定开始处理\nUserId: " + userId);
            if (result == DialogResult.OK)
            {
                //Let's do something crazy
                //LoadJavaScript("http://wkzy.mycourse.cn/js/wx.js");

                StringBuilder sb = new StringBuilder();
                sb.Append("var list = [");

                bool first = true;
                foreach(int id in ids)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    sb.Append(id);
                    first = false;
                }
                sb.AppendLine("];");
                sb.AppendLine(@"for(var i=0;i<list.length;i++){");
                sb.AppendLine("var userid = " + userId + ";");
                sb.AppendLine("var jiaoxuejihuaid = list[i];");
                sb.AppendLine("var finishData = {\"userid\": userid, \"jiaoxuejihuaid\": jiaoxuejihuaid};\r\n");
                sb.AppendLine(File.ReadAllText("a.js"));
                sb.AppendLine("}");
                sb.AppendLine(@"console.log('All Done!');");

                browser.ExecuteScriptAsync(sb.ToString());
            }
        }

        private void LoadJavaScript(string url)
        {
            string js = @"(function () {
    function getScript(url, success) {
        var script = document.createElement('script');
        script.src = url;
        var head = document.getElementsByTagName('head')[0],
            done = false;
        // Attach handlers for all browsers
        script.onload = script.onreadystatechange = function () {
            if (!done && (!this.readyState
                || this.readyState == 'loaded'
                || this.readyState == 'complete')) {
                done = true;
                success();
                script.onload = script.onreadystatechange = null;
                head.removeChild(script);
            }
        };
        head.appendChild(script);
    }
    getScript('%%url%%', function () {
        if (typeof getQueryString == 'undefined') {
            console.log('JS not loaded');
        } else {
            console.log('JS loaded');
        }
    });
})();";
            js = js.Replace("%%url%%", url);
            browser.ExecuteScriptAsync(js);
        }

        private void OnConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            if (args.Source.Contains("studyAndTest"))
            {
                string msg = args.Message;
                if (msg.StartsWith("{\"result") && msg.EndsWith("}")) //should be the result json
                {
                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        statusStrip.Items["loginStatus"].Text = "解析课程列表";
                    });

                    JObject.LoadAsync(new JsonTextReader(new StringReader(msg))).ContinueWith(task =>
                    {
                        JObject jObj = task.Result;
                        JArray result = jObj["result"] as JArray;

                        List<int> ids = new List<int>();
                        int userId = -1;

                        foreach (JObject o in result)
                        {
                            JArray list = o["list"] as JArray;

                            foreach (JObject course in list)
                            {
                                if (!course["isComplete"].Value<bool>())
                                {
                                    int id = course["jiaoxuejihuaId"].Value<int>();
                                    ids.Add(id);

                                    if (userId == -1)
                                    {
                                        string courseUrl = course["courseUrl"].Value<string>();
                                        courseUrl = HttpUtility.UrlDecode(courseUrl);
                                        NameValueCollection collection = HttpUtility.ParseQueryString(courseUrl);
                                        userId = int.Parse(collection["userId"]);
                                    }
                                }
                            }
                        }

                        if (ids.Count > 0 && userId != -1)
                        {
                            ShowMsgBox(ids, userId);
                        }

                    });
                }
            }
            else if (args.Message.Equals("All Done!"))
            {
                MessageBox.Show("全部完成！你可以关闭本程序了！");
                browser.Load("http://wb.mycourse.cn/svnweiban/student/myself_totalCourseList.action");
            }
        }

        private void OnFrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            Console.WriteLine(args.Url);

            if (args.Url.Equals("http://wb.mycourse.cn/svnweiban/"))
            {
                browser.GetMainFrame().EvaluateScriptAsync(
                    @"
                    var input = document.getElementById('gover_search_key');
                    input.value = '西南交通大学';
                    input.focus();"
                    );
                return;
            }

            if (args.Url.Contains("home_index"))
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    statusStrip.Items["loginStatus"].Text = "登陆成功";
                    browser.Enabled = false;
                });

                browser.Load("http://wb.mycourse.cn/svnweiban/student/study_studyAndTest.action");
                return;
            }

            if (args.Url.Contains("studyAndTest"))
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    statusStrip.Items["loginStatus"].Text = "读取课程列表";
                });
            }

        }

    }
}
