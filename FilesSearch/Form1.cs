using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Xml;

namespace FilesSearch
{
    public partial class Form1 : Form
    {
        DirectoryInfo path;
        TreeNode tn;
        bool working;

        public Form1()
        {
            InitializeComponent();
            LoadConfig();
        }

        void SaveConfig()
        {
            XmlTextWriter writer = null;
            writer = new XmlTextWriter("config.xml", System.Text.Encoding.Unicode);
            writer.WriteStartDocument();
            writer.WriteStartElement("Настройки");
            writer.WriteAttributeString("Директория", startFolderTxt.Text);
            writer.WriteAttributeString("ИмяФайла", fileNameTxt.Text);
            writer.WriteAttributeString("Текст", textTxt.Text);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        void LoadConfig()
        {
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader("config.xml");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "Настройки")
                        {
                            startFolderTxt.Text = reader.GetAttribute("Директория");
                            fileNameTxt.Text = reader.GetAttribute("ИмяФайла");
                            textTxt.Text = reader.GetAttribute("Текст");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                SaveConfig();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog Folder = new FolderBrowserDialog();
            if (Folder.ShowDialog() == DialogResult.OK)
                startFolderTxt.Text = Folder.SelectedPath;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Thread th;
            th = new Thread(StartSearch);
            if (button2.Text == "Искать")
            {
                treeView1.Nodes.Clear();
                processedFilesLbl.Text = "0";
                timerLbl.Text = "0";
                currentFileNameLbl.Text = "()";
                path = new DirectoryInfo(startFolderTxt.Text);
                tn = treeView1.Nodes.Add(startFolderTxt.Text);
                working = true;
                th.Start();
                button2.Text = "Остановить";
            }
            else
            {
                working = false;
                if (th != null)
                    th.Abort();
                button2.Text = "Искать";
            }
        }

        void StartSearch()
        {
            new Thread(Timer).Start();
            FindFiles(path, fileNameTxt.Text, textTxt.Text, tn);
            working = false;
            Invoke(new MethodInvoker(delegate
            {
                button2.Text = "Искать";
            }));
        }

        void Timer()
        {
            while (working)
            {
                Invoke(new MethodInvoker(delegate
                {
                    timerLbl.Text = (Convert.ToInt32(timerLbl.Text) + 1).ToString();
                }));
                Thread.Sleep(1000);
            }
        }

        void FindFiles(DirectoryInfo path, string filename, string filetext, TreeNode treenode)
        {
            try
            {
                foreach (DirectoryInfo dir in path.GetDirectories())
                {
                    if (!working)
                        return;
                    UpdateCurrentFileName(dir.Name);
                    FindFiles(dir, filename, filetext, AddNode(treenode, dir.Name));
                }
                foreach (FileInfo f in path.GetFiles(filename))
                {
                    if (!working)
                        return;
                    UpdateCurrentFileName(f.Name);
                    UpdateFilesCount();
                    if (ReadFile(f.FullName, filetext))
                        AddNode(treenode, f.Name);
                }
                if (treenode.Nodes.Count == 0)
                    RemoveNode(treenode);
            }
            catch (UnauthorizedAccessException)
            {
                RemoveNode(treenode);
            }
        }

        TreeNode AddNode(TreeNode treenode, string treenodename)
        {
            TreeNode tn = new TreeNode();
            Invoke(new MethodInvoker(delegate
            {
                tn = treenode.Nodes.Add(treenodename);
            }));
            return tn;
        }

        void RemoveNode(TreeNode treenode)
        {
            Invoke(new MethodInvoker(delegate
            {
                treenode.Remove();
            }));
        }

        void UpdateFilesCount()
        {
            processedFilesLbl.Text = (Convert.ToInt32(processedFilesLbl.Text) + 1).ToString();
        }

        void UpdateCurrentFileName(string filename)
        {
            currentFileNameLbl.Text = string.Format("({0})", filename);
        }

        bool ReadFile(string filename, string filetext)
        {
            string line = "";
            try
            {
                FileStream f = File.OpenRead(filename);
                StreamReader fs = new StreamReader(f, System.Text.Encoding.GetEncoding(1251));
                while ((line = fs.ReadLine()) != null)
                {
                    //MessageBox.Show(line);
                    if (line.Contains(filetext))
                    {
                        f.Close();
                        return true;
                    }
                }
                f.Close();
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }
    }
}
