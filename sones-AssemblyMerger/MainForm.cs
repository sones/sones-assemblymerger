/*
* sones GraphDB - Community Edition - http://www.sones.com
* Copyright (C) 2007-2011 sones GmbH
*
* This file is part of sones GraphDB Community Edition.
*
* sones GraphDB is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published by
* the Free Software Foundation, version 3 of the License.
* 
* sones GraphDB is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU Affero General Public License for more details.
*
* You should have received a copy of the GNU Affero General Public License
* along with sones GraphDB. If not, see <http://www.gnu.org/licenses/>.
* 
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace sones.AssemblyMerger
{
    public partial class MainForm : Form
    {
        #region Data
        String PathToCommunityEdition { get; set; }
       
        String PathToILMerge { get; set; }

        Boolean ILMERGE = true;

        Dictionary<String, String> Assemblies;
        #endregion
        
        #region MainFormInit
        public MainForm()
        {
            InitializeComponent();

            TvAssemblies.CheckBoxes = true;
            TvAssemblies.AfterCheck += new TreeViewEventHandler(TvAssemblies_AfterCheck);
            TbAssemblyName.LostFocus += new EventHandler(TbAssemblyName_LostFocus);


            if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Microsoft\ILMerge\ILMerge.exe"))
            {
                MessageBox.Show("Could not find ILMerge at the default installation directory! Please install ILMerge at: \r\n   " +
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Microsoft\ILMerge\ILMerge.exe" +
                    "\r\nor set the install directory " +
                    "in the MainForm!\r\n" +
                "Download from:\r\nhttp://www.microsoft.com/download/en/details.aspx?displaylang=en&id=17630", "Couldn't find ILMerge.exe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ILMERGE = false;
            }
            else
            {
                TbPathILMerge.Text = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Microsoft\ILMerge\ILMerge.exe";
                TbPathILMerge.Enabled = false;
            }

            PathToCommunityEdition = TbxPathSolution.Text;


        }
        #endregion

        #region Private Events
        private void TbAssemblyName_LostFocus(object sender, EventArgs e)
        {
            if (!TbAssemblyName.Text.EndsWith(".dll"))
            {
                MessageBox.Show("The merged assembly name has to end with '.dll'");
                TbAssemblyName.Text = "sones.dll";
            }
        }

        private void TvAssemblies_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.GetNodeCount(false) != 0)
                {


                    TreeNode parent = e.Node;

                    foreach (TreeNode child in parent.Nodes)
                    {
                        child.Checked = parent.Checked;
                    }

                }

                if (!IsAnyCheckboxChecked())
                {
                    BtnMerge.Enabled = false;
                }
                else
                {
                    BtnMerge.Enabled = true;
                }

            }


            
            
        }
        
        private void BtnChooseFolder_Click(object sender, EventArgs e)
        {
            ChooseFolder();
        }

        private void BtnChooseILMerge_Click(object sender, EventArgs e)
        {
            ChooseILMerge();
        }

        private void BtnGetAssemblies_Click(object sender, EventArgs e)
        {
            if (!TbxPathSolution.Text.Equals(""))
            {
                GetAssemblies();
                progressBar.Value = 100;
            }
            else
            {
                MessageBox.Show("There was no path selected!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private void BtnExpandAll_Click(object sender, EventArgs e)
        {
            TvAssemblies.ExpandAll();
        }

        private void BtnLogfile_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TbLogfile.Text = saveFileDialog.FileName;
            }
        }

        private void BtnMerge_Click(object sender, EventArgs e)
        {
            BtnMerge.Enabled = false;
            BtnMerge.Text = "Merging...";
            if (CbLogfile.Checked && System.IO.Directory.Exists(TbLogfile.Text.Substring(TbLogfile.Text.LastIndexOf("\\"))))
            {
                MessageBox.Show("A Logfile can not be created in this directory!", "Logfile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CbLogfile.Checked = false;
            }
            StartMerge();
            BtnMerge.Text = "Merge!";
            BtnMerge.Enabled = true;

        }

        private void BtnCollapse_Click(object sender, EventArgs e)
        {
            TvAssemblies.CollapseAll();
        }

        private void BtnAddAssembly_Click(object sender, EventArgs e)
        {
            AddAssemblyToList();
        }

        private void BtnOutputPath_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.Description = "Select the path, where you want to save the merged assembly.";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TbOutputPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void CbLogfile_CheckedChanged(object sender, EventArgs e)
        {
            if (CbLogfile.Checked && TbLogfile.Text.Equals(""))
            {
                TbLogfile.Text = TbOutputPath.Text + "\\MergeLog.log";
            }
        }
        #endregion

        #region Private Utilities
        
        /// <summary>
        /// Adds single or multiple Assemblies to the list of assemblies
        /// </summary>
        private void AddAssemblyToList()
        {
            List<String> inlist = new List<string>();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var files = openFileDialog1.FileNames;
                if (Assemblies == null)
                {
                    Assemblies = new Dictionary<string, string>();
                }
                TreeNode node = new TreeNode("Additional Assemblies");
                node.Name = "Additional Assemblies";
                node.Checked = true;
                foreach (var File in files)
                {
                    if (File.EndsWith(".dll"))
                    {


                        if (!TvAssemblies.Nodes.ContainsKey("Additional Assemblies"))
                        {
                            TvAssemblies.Nodes.Add(node);

                        }
                        else
                        {
                            node = TvAssemblies.Nodes[TvAssemblies.Nodes.IndexOfKey("Additional Assemblies")];
                        }

                        var k = File.LastIndexOf("\\") + 1;
                        var myFile = File.Substring(k, File.Length - k);
                        if (!Assemblies.Keys.Contains(myFile))
                        {
                            Assemblies.Add(myFile, File);
                            TreeNode child = new TreeNode(myFile);
                            child.Name = myFile;
                            child.Checked = true;
                            node.Nodes.Add(child);
                        }
                        else
                        {
                            inlist.Add(File);
                        }




                    }

                }
                if (inlist.Count() != 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var file in inlist)
                    {
                        sb.AppendLine(file.Substring(file.LastIndexOf("\\") + 1));
                    }
                    MessageBox.Show("The following assemblies are already in the list:\r\n\r\n" + sb.ToString(), "Assembly already in the list!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                TvAssemblies_AfterCheck(new object(), new TreeViewEventArgs(node, TreeViewAction.ByKeyboard));
            }
        }
        
        /// <summary>
        /// Checks wether any CheckBox in the TreeView is checked
        /// </summary>
        /// <returns></returns>
        private bool IsAnyCheckboxChecked()
        {
            foreach (TreeNode node in TvAssemblies.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Opens a dialg, to select a directory which ends with "CommunityEdition"
        /// </summary>
        private void ChooseFolder()
        {
            folderBrowserDialog.ShowNewFolderButton = false;
            folderBrowserDialog.Description = "Select the path to the CommunityEdition.";
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (!folderBrowserDialog.SelectedPath.EndsWith("CommunityEdition"))
                {
                    MessageBox.Show("This path seems not point to a valide CommunityEdition Solution.\r\n" +
                        "Please make sure, that your directory ends with 'CommunityEdition'!", "CommunityEdition", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                TbxPathSolution.Text = folderBrowserDialog.SelectedPath;
                PathToCommunityEdition = folderBrowserDialog.SelectedPath;
                TbOutputPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Opens a dialog, to select ILMerge.exe
        /// </summary>
        private void ChooseILMerge()
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                TbPathILMerge.Text = openFileDialog.FileName;
                TbPathILMerge.Enabled = false;
                PathToILMerge = openFileDialog.FileName;
                ILMERGE = true;
            }
        }

        /// <summary>
        /// Collects all Assemblies shipped with the current CommunityEdition of the GraphDB
        /// </summary>
        /// <param name="myRoot">the root path </param>
        private void CollectAssemblies(string myRoot)
        {
            
            try
            {
                string[] Files = System.IO.Directory.GetFiles(myRoot);
                string[] Folders = System.IO.Directory.GetDirectories(myRoot);


                if (myRoot != PathToCommunityEdition)
                {
                    if (Files.Length != 0)
                    {
                        for (int i = 0; i < Files.Length; i++)
                        {
                            string File = Files[i];
                            if (File.EndsWith(".dll"))
                            {
                                File = File.Substring(File.LastIndexOf("\\") + 1);

                                if (!Files[i].Contains("Application"))
                                {
                                    if (!Assemblies.Keys.Contains(File))
                                    {
                                        Assemblies.Add(File, Files[i]);
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                

                if (Folders.Length != 0)
                {
                    for (int i = 0; i < Folders.Length; i++)
                    {
                        CollectAssemblies(Folders[i]);
                    }
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        /// <summary>
        /// Fetch and visualize all shipped Assemblies in TreeView
        /// </summary>
        private void GetAssemblies()
        {
            if (TvAssemblies.Nodes.Count == 0 || TvAssemblies.Nodes.ContainsKey("Additional Assemblies"))
            {
                progressBar.PerformStep();
                BtnMerge.Enabled = true;
                string newPath = System.IO.Path.Combine(PathToCommunityEdition, "temp");
                if (Assemblies == null)
                {
                    Assemblies = new Dictionary<String, String>();
                }
                CollectAssemblies(PathToCommunityEdition);
                TvAssemblies.CheckBoxes = true;

                foreach (var Assembly in Assemblies)
                {
                    progressBar.PerformStep();
                    progressBar.Update();
                    if (Assembly.Value.IndexOf("CommunityEdition") != -1)
                    {


                        var nodename = Assembly.Value.Substring(Assembly.Value.IndexOf("CommunityEdition") + 17);
                        nodename = nodename.Substring(0, nodename.IndexOf("\\"));

                        if (!TvAssemblies.Nodes.ContainsKey(nodename))
                        {
                            progressBar.Update();
                            progressBar.PerformStep();
                            var treenode = TvAssemblies.Nodes.Add(nodename);
                            treenode.Name = nodename;
                            treenode.Checked = true;
                            var child = treenode.Nodes.Add(Assembly.Key);
                            child.Name = Assembly.Key;
                            child.Checked = true;
                        }
                        else
                        {
                            TreeNode treenode = TvAssemblies.Nodes[TvAssemblies.Nodes.IndexOfKey(nodename)];
                            var child = treenode.Nodes.Add(Assembly.Key);
                            child.Name = Assembly.Key;
                            child.Checked = true;
                        }
                    }
                }
            }
            else
            {
                progressBar.Value = 0;
                TvAssemblies.Nodes.Clear();
                GetAssemblies();
            }
        }

        /// <summary>
        /// Start the merge process by calling ILMerge.exe with the created arguments
        /// </summary>
        private void StartMerge()

        {
            if (!PathToCommunityEdition.Equals("") && ILMERGE)
            {


                if (!System.IO.File.Exists(TbOutputPath.Text + "\\" + TbAssemblyName.Text))
                {

                    string newPath = System.IO.Path.Combine(PathToCommunityEdition, "temp");
                    progressBar.Value = 0;

                    System.IO.Directory.CreateDirectory(newPath);


                    string arguments = BuildArgumentsAndCopyToTemp(newPath);
                    progressBar.PerformStep();
                    if (!System.IO.File.Exists(newPath + "\\ILMerge.exe"))
                    {
                        if (System.IO.File.Exists(TbPathILMerge.Text))
                        {
                            System.IO.File.Copy(TbPathILMerge.Text, newPath + "\\ILMerge.exe");
                        }
                        else
                        {
                            MessageBox.Show("Couldn't find ILMerge at:\r\n " +
                                    TbPathILMerge.Text + "\r\nPlease make sure a correct path to ILMerge.exe!", "Couldn'f find ILMerge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    progressBar.PerformStep();

                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo(newPath + "\\" + "ILMerge.exe");
                        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
                        startInfo.Arguments = arguments;
                        startInfo.WorkingDirectory = newPath;
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;

                        Process.Start(startInfo);

                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show(ex.Message + "\r\n" + (ex.InnerException != null ? ex.InnerException.Message : ""), "Unkown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        progressBar.Value = 0;
                        System.IO.File.Delete(newPath + "\\ILMerge.exe");
                        return;
                    }



                    bool Readable = false;
                    int tries = 0;
                    while (!Readable)
                    {
                        try
                        {
                            progressBar.Update();
                            
                            System.IO.File.Copy(newPath + "\\" + TbAssemblyName.Text, TbOutputPath.Text + "\\" + TbAssemblyName.Text);
                            if (CbDebug.Checked)
                            {
                                System.IO.File.Copy(newPath + "\\" + (TbAssemblyName.Text.Replace(".dll", ".pdb")), TbOutputPath.Text + "\\" + (TbAssemblyName.Text.Replace(".dll", ".pdb")));
                            }
                                
                            Readable = true;
                            
                            
                            System.Threading.Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            progressBar.PerformStep();
                            Readable = false;
                            System.Threading.Thread.Sleep(500);
                            if (!System.IO.File.Exists(newPath + "\\" + TbAssemblyName.Text))
                            {
                                tries++;
                                if (tries == 20)
                                {
                                    MessageBox.Show("There must been an error with ILMerge.exe! May there is a problem with your assemblies or " +
                                        "your 'Program Debug Databases' are out of date.\r\nPlease take a glance at the Logfile generated by ILMerge.\r\n" +
                                        "If you don't set a path for the Logfile output, try again with a set one!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    System.IO.Directory.Delete(newPath, true);
                                    return;
                                }
                            }
                        }

                    }
                    Readable = false;
                    while (!Readable)
                    {
                        try
                        {
                            System.IO.Directory.Delete(newPath, true);
                            Readable = true;
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                    
                    
                    progressBar.Value = 100;
                    MessageBox.Show("All selected files successful merged to:\r\n" +
                        "  "+ TbOutputPath.Text + "\\" + TbAssemblyName.Text +  "\r\n" + (CbDebug.Checked ? "A 'Program Debug Database' was also created." : ""),
                        "Merge Succeeded!",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);

                    


                }
                else
                {
                    if (CbOverwrite.Checked)
                    {
                        System.IO.File.Delete(TbOutputPath.Text + "\\" + TbAssemblyName.Text);
                        if (System.IO.File.Exists(TbOutputPath.Text + "\\" + TbAssemblyName.Text.Replace(".dll", ".pdb")))
                        {
                            System.IO.File.Delete(TbOutputPath.Text + "\\" + TbAssemblyName.Text.Replace(".dll", ".pdb"));
                        }
                        StartMerge();
                    }
                    else
                    {


                        if (MessageBox.Show(TbAssemblyName.Text + " already exists!\r\nOverwrite the existing one?", "File exists!",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            System.IO.File.Delete(TbOutputPath.Text + "\\" + TbAssemblyName.Text);
                            if (System.IO.File.Exists(TbOutputPath.Text + "\\" + TbAssemblyName.Text.Replace(".dll", ".pdb")))
                            {
                                System.IO.File.Delete(TbOutputPath.Text + "\\" + TbAssemblyName.Text.Replace(".dll", ".pdb"));
                            }
                                
                            StartMerge();
                        }
                        else
                        {
                            progressBar.Value = 100;
                        }
                    }
                }

            }
            else
            {
                MessageBox.Show("Either there was no ILMerge found, or the path to the CommunityEdition was cleared!\r\n"+
                    "Please ensure that there is an ILMerge available.","Couldn't merge!",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            
        }

        /// <summary>
        /// Creates the argument list and copies the assemblies to the temp folder, including ILMerge
        /// </summary>
        /// <param name="myDestination">the temp directory</param>
        /// <returns></returns>
        private string BuildArgumentsAndCopyToTemp(String myDestination)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append("/allowDup /targetplatform:v4 /target:library /copyattrs /allowMultiple /xmldocs ");
                if(CbLogfile.Checked)
                    sb.Append("/log:" + TbLogfile.Text + " ");

                if (!CbDebug.Checked)
                    sb.Append("/ndebug ");
                sb.Append("/out:" + TbAssemblyName.Text + " ");

                foreach (var Assembly in Assemblies)
                {
                    foreach (TreeNode Node in TvAssemblies.Nodes)
                    {
                        foreach (TreeNode child in Node.Nodes)
                        {
                            if (child.Name == Assembly.Key)
                            {
                                if (child.Checked)
                                {
                                    sb.Append(Assembly.Key + " ");
                                    if (!System.IO.File.Exists(myDestination + "\\" + Assembly.Key))
                                    {
                                        System.IO.File.Copy(Assembly.Value, myDestination + "\\" + Assembly.Key);
                                        if (!System.IO.File.Exists(myDestination + "\\" + Assembly.Key.Replace(".dll", ".pdb")) && System.IO.File.Exists(Assembly.Value.Replace(".dll",".pdb")) && CbDebug.Checked)
                                        {
                                            System.IO.File.Copy(Assembly.Value.Replace(".dll", ".pdb"), myDestination + "\\" + Assembly.Key.Replace(".dll", ".pdb"));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                
            }
            


            return sb.ToString();
        }

        #endregion

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.BtnChooseFolder = new System.Windows.Forms.Button();
            this.TbxPathSolution = new System.Windows.Forms.TextBox();
            this.TbPathILMerge = new System.Windows.Forms.TextBox();
            this.BtnChooseILMerge = new System.Windows.Forms.Button();
            this.GbILMerge = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.BtnGetAssemblies = new System.Windows.Forms.Button();
            this.BtnMerge = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.CbOverwrite = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.CbLogfile = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.BtnOutputPath = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.TbOutputPath = new System.Windows.Forms.TextBox();
            this.CbDebug = new System.Windows.Forms.CheckBox();
            this.BtnLogfile = new System.Windows.Forms.Button();
            this.TbLogfile = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.TbAssemblyName = new System.Windows.Forms.TextBox();
            this.BtnExpandAll = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.BtnCollapse = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.BtnAddAssembly = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label8 = new System.Windows.Forms.Label();
            this.TvAssemblies = new MyTreeView();
            this.GbILMerge.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.SelectedPath = "C:\\";
            this.folderBrowserDialog.ShowNewFolderButton = false;
            // 
            // BtnChooseFolder
            // 
            this.BtnChooseFolder.Image = ((System.Drawing.Image)(resources.GetObject("BtnChooseFolder.Image")));
            this.BtnChooseFolder.Location = new System.Drawing.Point(313, 32);
            this.BtnChooseFolder.Name = "BtnChooseFolder";
            this.BtnChooseFolder.Size = new System.Drawing.Size(35, 32);
            this.BtnChooseFolder.TabIndex = 0;
            this.BtnChooseFolder.UseVisualStyleBackColor = true;
            this.BtnChooseFolder.Click += new System.EventHandler(this.BtnChooseFolder_Click);
            // 
            // TbxPathSolution
            // 
            this.TbxPathSolution.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(231)))), ((int)(((byte)(194)))));
            this.TbxPathSolution.Location = new System.Drawing.Point(6, 39);
            this.TbxPathSolution.Name = "TbxPathSolution";
            this.TbxPathSolution.Size = new System.Drawing.Size(301, 20);
            this.TbxPathSolution.TabIndex = 1;
            // 
            // TbPathILMerge
            // 
            this.TbPathILMerge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(231)))), ((int)(((byte)(194)))));
            this.TbPathILMerge.Location = new System.Drawing.Point(6, 39);
            this.TbPathILMerge.Name = "TbPathILMerge";
            this.TbPathILMerge.Size = new System.Drawing.Size(327, 20);
            this.TbPathILMerge.TabIndex = 2;
            // 
            // BtnChooseILMerge
            // 
            this.BtnChooseILMerge.ForeColor = System.Drawing.Color.Black;
            this.BtnChooseILMerge.Location = new System.Drawing.Point(220, 65);
            this.BtnChooseILMerge.Name = "BtnChooseILMerge";
            this.BtnChooseILMerge.Size = new System.Drawing.Size(113, 32);
            this.BtnChooseILMerge.TabIndex = 3;
            this.BtnChooseILMerge.Text = "Find ILMerge";
            this.BtnChooseILMerge.UseVisualStyleBackColor = true;
            this.BtnChooseILMerge.Click += new System.EventHandler(this.BtnChooseILMerge_Click);
            // 
            // GbILMerge
            // 
            this.GbILMerge.BackColor = System.Drawing.Color.Transparent;
            this.GbILMerge.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.GbILMerge.Controls.Add(this.label1);
            this.GbILMerge.Controls.Add(this.BtnChooseILMerge);
            this.GbILMerge.Controls.Add(this.TbPathILMerge);
            this.GbILMerge.ForeColor = System.Drawing.Color.Black;
            this.GbILMerge.Location = new System.Drawing.Point(542, 12);
            this.GbILMerge.Name = "GbILMerge";
            this.GbILMerge.Size = new System.Drawing.Size(339, 102);
            this.GbILMerge.TabIndex = 4;
            this.GbILMerge.TabStop = false;
            this.GbILMerge.Text = "ILMerge";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(3, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "ILMerge.exe is located at:";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "ILMerge.exe";
            this.openFileDialog.Filter = "ILMerge|ILMerge.exe";
            this.openFileDialog.InitialDirectory = "C:\\Program Files (x86)\\Microsoft\\ILMerge";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.TbxPathSolution);
            this.groupBox1.Controls.Add(this.BtnChooseFolder);
            this.groupBox1.Controls.Add(this.BtnGetAssemblies);
            this.groupBox1.ForeColor = System.Drawing.Color.Black;
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(354, 102);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Assemblies CommunityEdition";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(6, 23);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(103, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Path to the Solution:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(7, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(175, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "The CommunityEdition is located at:";
            // 
            // BtnGetAssemblies
            // 
            this.BtnGetAssemblies.Location = new System.Drawing.Point(240, 65);
            this.BtnGetAssemblies.Name = "BtnGetAssemblies";
            this.BtnGetAssemblies.Size = new System.Drawing.Size(108, 29);
            this.BtnGetAssemblies.TabIndex = 7;
            this.BtnGetAssemblies.Text = "Get Assemblies";
            this.BtnGetAssemblies.UseVisualStyleBackColor = true;
            this.BtnGetAssemblies.Click += new System.EventHandler(this.BtnGetAssemblies_Click);
            // 
            // BtnMerge
            // 
            this.BtnMerge.Enabled = false;
            this.BtnMerge.Location = new System.Drawing.Point(784, 428);
            this.BtnMerge.Name = "BtnMerge";
            this.BtnMerge.Size = new System.Drawing.Size(91, 49);
            this.BtnMerge.TabIndex = 6;
            this.BtnMerge.Text = "Merge!";
            this.BtnMerge.UseVisualStyleBackColor = true;
            this.BtnMerge.Click += new System.EventHandler(this.BtnMerge_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.Transparent;
            this.groupBox2.Controls.Add(this.CbOverwrite);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.CbLogfile);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.BtnOutputPath);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.TbOutputPath);
            this.groupBox2.Controls.Add(this.CbDebug);
            this.groupBox2.Controls.Add(this.BtnLogfile);
            this.groupBox2.Controls.Add(this.TbLogfile);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.TbAssemblyName);
            this.groupBox2.ForeColor = System.Drawing.Color.Black;
            this.groupBox2.Location = new System.Drawing.Point(542, 120);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(339, 170);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Settings";
            // 
            // CbOverwrite
            // 
            this.CbOverwrite.AutoSize = true;
            this.CbOverwrite.Location = new System.Drawing.Point(251, 27);
            this.CbOverwrite.Name = "CbOverwrite";
            this.CbOverwrite.Size = new System.Drawing.Size(71, 17);
            this.CbOverwrite.TabIndex = 9;
            this.CbOverwrite.Text = "Overwrite";
            this.CbOverwrite.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(3, 50);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Output:";
            // 
            // CbLogfile
            // 
            this.CbLogfile.AutoSize = true;
            this.CbLogfile.Location = new System.Drawing.Point(6, 140);
            this.CbLogfile.Name = "CbLogfile";
            this.CbLogfile.Size = new System.Drawing.Size(57, 17);
            this.CbLogfile.TabIndex = 7;
            this.CbLogfile.Text = "Logfile";
            this.CbLogfile.UseVisualStyleBackColor = true;
            this.CbLogfile.CheckedChanged += new System.EventHandler(this.CbLogfile_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(3, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Logfile:";
            // 
            // BtnOutputPath
            // 
            this.BtnOutputPath.Image = ((System.Drawing.Image)(resources.GetObject("BtnOutputPath.Image")));
            this.BtnOutputPath.Location = new System.Drawing.Point(295, 59);
            this.BtnOutputPath.Name = "BtnOutputPath";
            this.BtnOutputPath.Size = new System.Drawing.Size(35, 32);
            this.BtnOutputPath.TabIndex = 3;
            this.BtnOutputPath.UseVisualStyleBackColor = true;
            this.BtnOutputPath.Click += new System.EventHandler(this.BtnOutputPath_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(3, 45);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "The output directory:";
            // 
            // TbOutputPath
            // 
            this.TbOutputPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(231)))), ((int)(((byte)(194)))));
            this.TbOutputPath.Location = new System.Drawing.Point(6, 66);
            this.TbOutputPath.Name = "TbOutputPath";
            this.TbOutputPath.Size = new System.Drawing.Size(283, 20);
            this.TbOutputPath.TabIndex = 5;
            // 
            // CbDebug
            // 
            this.CbDebug.AutoSize = true;
            this.CbDebug.Checked = true;
            this.CbDebug.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CbDebug.Location = new System.Drawing.Point(69, 140);
            this.CbDebug.Name = "CbDebug";
            this.CbDebug.Size = new System.Drawing.Size(100, 17);
            this.CbDebug.TabIndex = 4;
            this.CbDebug.Text = "Debug Symbols";
            this.CbDebug.UseVisualStyleBackColor = true;
            // 
            // BtnLogfile
            // 
            this.BtnLogfile.ForeColor = System.Drawing.Color.Black;
            this.BtnLogfile.Location = new System.Drawing.Point(251, 111);
            this.BtnLogfile.Name = "BtnLogfile";
            this.BtnLogfile.Size = new System.Drawing.Size(82, 23);
            this.BtnLogfile.TabIndex = 3;
            this.BtnLogfile.Text = "Save Logfile";
            this.BtnLogfile.UseVisualStyleBackColor = true;
            this.BtnLogfile.Click += new System.EventHandler(this.BtnLogfile_Click);
            // 
            // TbLogfile
            // 
            this.TbLogfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(231)))), ((int)(((byte)(194)))));
            this.TbLogfile.Location = new System.Drawing.Point(6, 113);
            this.TbLogfile.Name = "TbLogfile";
            this.TbLogfile.Size = new System.Drawing.Size(239, 20);
            this.TbLogfile.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 27);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(135, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Name of merged Assembly:";
            // 
            // TbAssemblyName
            // 
            this.TbAssemblyName.Location = new System.Drawing.Point(144, 24);
            this.TbAssemblyName.Name = "TbAssemblyName";
            this.TbAssemblyName.Size = new System.Drawing.Size(100, 20);
            this.TbAssemblyName.TabIndex = 0;
            this.TbAssemblyName.Text = "sones.dll";
            // 
            // BtnExpandAll
            // 
            this.BtnExpandAll.Location = new System.Drawing.Point(258, 417);
            this.BtnExpandAll.Name = "BtnExpandAll";
            this.BtnExpandAll.Size = new System.Drawing.Size(75, 23);
            this.BtnExpandAll.TabIndex = 14;
            this.BtnExpandAll.Text = "Expand All";
            this.BtnExpandAll.UseVisualStyleBackColor = true;
            this.BtnExpandAll.Click += new System.EventHandler(this.BtnExpandAll_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.FileName = "MergeLog.log";
            this.saveFileDialog.Filter = "Logfile|*.log";
            // 
            // BtnCollapse
            // 
            this.BtnCollapse.Location = new System.Drawing.Point(258, 388);
            this.BtnCollapse.Name = "BtnCollapse";
            this.BtnCollapse.Size = new System.Drawing.Size(75, 23);
            this.BtnCollapse.TabIndex = 15;
            this.BtnCollapse.Text = "Collapse All";
            this.BtnCollapse.UseVisualStyleBackColor = true;
            this.BtnCollapse.Click += new System.EventHandler(this.BtnCollapse_Click);
            // 
            // progressBar
            // 
            this.progressBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(174)))), ((int)(((byte)(212)))), ((int)(((byte)(124)))));
            this.progressBar.Location = new System.Drawing.Point(439, 449);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(339, 28);
            this.progressBar.Step = 5;
            this.progressBar.TabIndex = 16;
            // 
            // BtnAddAssembly
            // 
            this.BtnAddAssembly.Location = new System.Drawing.Point(258, 446);
            this.BtnAddAssembly.Name = "BtnAddAssembly";
            this.BtnAddAssembly.Size = new System.Drawing.Size(108, 31);
            this.BtnAddAssembly.TabIndex = 17;
            this.BtnAddAssembly.Text = "Add Assembly";
            this.BtnAddAssembly.UseVisualStyleBackColor = true;
            this.BtnAddAssembly.Click += new System.EventHandler(this.BtnAddAssembly_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Assembly|*.dll";
            this.openFileDialog1.Multiselect = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(9, 117);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(143, 13);
            this.label8.TabIndex = 10;
            this.label8.Text = "Current Assemblies to merge:";
            // 
            // TvAssemblies
            // 
            this.TvAssemblies.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(231)))), ((int)(((byte)(194)))));
            this.TvAssemblies.Location = new System.Drawing.Point(12, 133);
            this.TvAssemblies.Name = "TvAssemblies";
            this.TvAssemblies.Size = new System.Drawing.Size(240, 344);
            this.TvAssemblies.TabIndex = 13;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(887, 486);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.BtnAddAssembly);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.BtnCollapse);
            this.Controls.Add(this.BtnExpandAll);
            this.Controls.Add(this.TvAssemblies);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.BtnMerge);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.GbILMerge);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "sones-AssemblyMerger - powerd by ILMerge";
            this.GbILMerge.ResumeLayout(false);
            this.GbILMerge.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
   
    }

        
}

