using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace 大风的笔趣阁爬虫
{
    public partial class Form1 : Form
    {
        /***********************
         &nbsp;     ->      空格
         <br>       ->      换行
        ***********************/

        public Form1()
        {
            InitializeComponent();
        }

        Thread _ThreadMain;
        bool _IsRunning = false;

        /// <summary>
        /// 根网址
        /// </summary>
        const string _strRootUrl = "https://www.biquge.com.cn";
        /// <summary>
        /// 小说ID
        /// </summary>
        string _strBookID = "";

        private void button1_Click(object sender, EventArgs e)
        {
            if(!_IsRunning)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        private void btnBookInfo_Click(object sender, EventArgs e)
        {
            CheckBookID(textBox1.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// 检查小说ID是否正确
        /// </summary>
        /// <param name="id">小说ID</param>
        /// <returns></returns>
        private bool CheckBookID(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                MessageBox.Show("ID不能为空！");
                return false;
            }

            var web = new HtmlAgilityPack.HtmlWeb();
            var doc_book = web.Load($"{_strRootUrl}/book/{id}/");
            HtmlAgilityPack.HtmlNode node_book_name = doc_book.DocumentNode.SelectSingleNode("//div[@id='info']/h1");
            HtmlAgilityPack.HtmlNode node_book_cover = doc_book.DocumentNode.SelectSingleNode("//div[@id='fmimg']/img");
            HtmlAgilityPack.HtmlNodeCollection node_book_properties = doc_book.DocumentNode.SelectNodes("//div[@id='info']/p");
            if (node_book_name == null)
            {
                MessageBox.Show($"找不到ID：{id}！");
                return false;
            }
            if (node_book_cover != null)
            {
                var url = node_book_cover.Attributes["src"].Value;
                System.Net.WebRequest request = System.Net.WebRequest.Create(url);
                System.Net.WebResponse response = request.GetResponse();
                System.IO.Stream responseStream = response.GetResponseStream();
                Bitmap bmp = new Bitmap(responseStream);
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Image = bmp;
            }
            if (node_book_properties != null && node_book_properties.Count > 0)
            {
                textBox3.Text = "";
                for (int i = 0; i < node_book_properties.Count; i++)
                {
                    string property = node_book_properties[i].InnerText;
                    if (property.Contains("&nbsp;"))
                    {
                        property = property.Replace("&nbsp;", " ");
                    }
                    property += "\r\n";
                    textBox3.AppendText(property);
                }
            }

            return true;
        }

        /// <summary>
        /// 线程开始
        /// </summary>
        private void Start()
        {
            _strBookID = textBox1.Text;
            if (!CheckBookID(_strBookID)) return;

            _IsRunning = true;
            button1.Text = "结束";
            textBox1.Enabled = false;
            checkBox1.Enabled = false;
            _ThreadMain = new Thread(Run);
            _ThreadMain.Start();
        }

        /// <summary>
        /// 线程停止
        /// </summary>
        private void Stop()
        {
            if (_ThreadMain != null &&
                _ThreadMain.ThreadState == ThreadState.Running)
            {
                _IsRunning = false;
                textBox1.Enabled = true;
                checkBox1.Enabled = true;
                button1.Text = "开始";
                progressBar1.Value = 0;
                label2.Text = "0/0";
            }
        }

        /// <summary>
        /// 爬取线程
        /// </summary>
        private void Run()
        {
            var url = $"{_strRootUrl}/book/{_strBookID}/";
            var web = new HtmlAgilityPack.HtmlWeb();
            var doc_book = web.Load(url);

            // 书名
            HtmlAgilityPack.HtmlNode node_book_name = doc_book.DocumentNode.SelectSingleNode("//div[@id='info']/h1");
            var book_name = node_book_name.InnerText;
            var file = book_name + ".txt";
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            // 章节数
            HtmlAgilityPack.HtmlNodeCollection listNode = doc_book.DocumentNode.SelectNodes("//div[@id='list']/dl[1]/dd");
            var count_chapter = listNode.Count;

            // 初始化进度条
            progressBar1.Invoke(new Action(() => {
                textBox2.Text = "";
                progressBar1.Maximum = count_chapter;
                progressBar1.Value = 0;
            }));

            #region 章节循环
            using (FileStream fs = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
            {
                for (int i = 0; i < count_chapter; i++)
                {
                    var tmp = listNode[i].FirstChild.Attributes["href"].Value;
                    var site_chapter = _strRootUrl + tmp;
                    var doc_chapter = web.Load(site_chapter);
                    HtmlAgilityPack.HtmlNode node_chapter_title = doc_chapter.DocumentNode.SelectSingleNode("//div[@class='bookname']/h1");

                    // 章节名
                    var title_chapter = "";
                    var isAppendFront = false;
                    this.Invoke(new Action(() => {
                        isAppendFront = checkBox1.Checked;
                    }));
                    if (isAppendFront)
                        title_chapter = "第" + (i + 1).ToString() + "章 " + node_chapter_title.InnerText;
                    else
                        title_chapter = node_chapter_title.InnerText;

                    // 章节内容
                    HtmlAgilityPack.HtmlNode node_chapter_content = doc_chapter.DocumentNode.SelectSingleNode("//div[@id='content']");
                    var content_chapter = node_chapter_content.InnerHtml;
                    content_chapter = content_chapter.Replace("<br>", "\r\n");
                    content_chapter = content_chapter.Replace("&nbsp;", " ");

                    // 写入TXT
                    sw.WriteLine(title_chapter);
                    sw.WriteLine(content_chapter);

                    // 更新进度
                    this.Invoke(new Action(() => {
                        progressBar1.Value = i + 1;
                        progressBar1.Update();
                        label2.Text = $"{(i + 1).ToString()}/{count_chapter.ToString()}";
                        textBox2.AppendText($"{title_chapter}\r\n");
                    }));

                    Thread.Sleep(1);

                    // 检查停止
                    if (!_IsRunning)
                    {
                        this.Invoke(new Action(() => {
                            progressBar1.Value = 0;
                            progressBar1.Update();
                            label2.Text = "0/0";
                        }));
                        return;
                    }
                }
            }

            #endregion

            MessageBox.Show("小说爬取完毕！");
            this.Invoke(new Action(() => {
                _IsRunning = false;
                textBox1.Enabled = true;
                checkBox1.Enabled = true;
                button1.Text = "开始";
                progressBar1.Value = 0;
                label2.Text = "0/0";
            }));
        }
    }
}
