using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

// Created by Zaini Hafid

namespace SAMEngine
{
    public partial class SAMEngineForm : Form
    {
        DBEngineDataContext db = new DBEngineDataContext();

        string pathSimpan = "";

        public string PathSimpan
        {
            get { return pathSimpan; }
            set { pathSimpan = value; }
        }
        
        public SAMEngineForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
                    //MessageBox.Show("success");
                    folderBrowserDialog1.ShowDialog();

                    if (folderBrowserDialog1.SelectedPath != "")
                    {
                        DialogResult r = MessageBox.Show("Yakin akan baca seluruh Folder?", "Konfirmasi", MessageBoxButtons.YesNo);
                        if (r.ToString() == "Yes")
                        {
                            searchMainFolder(folderBrowserDialog1.SelectedPath);
                        }
                    }

            //searchMainFolder(@"C:\Users\Administrator\Desktop\Manifest\Manifest jan 13");
            //searchFile(@"C:\Users\Administrator\Desktop\Manifest\Manifest jan 13\OPR_1\D1", 1);

        }

        // cari Folder utama
        private void searchMainFolder(string path){

            //@"C:\Users\Administrator\Desktop\Manifest\Manifest jan 13\OPR_1"
            var listFolder = (from folder in Directory.GetDirectories(path) orderby folder ascending select folder);
            foreach (var w in listFolder)
            {
                // ambil nama file 
                string dirName = new DirectoryInfo(w.ToString()).Name;
                    
                // Cek file apakah sudah ada apa belum
                var cekFolder = (from folder in db.tbl_Folders select folder).Where(q => q.nama == dirName.ToString()).SingleOrDefault();
                if (cekFolder == null)
                {
                    tbl_Folder folder = new tbl_Folder();
                    folder.nama = dirName;
                    db.tbl_Folders.InsertOnSubmit(folder);
                    db.SubmitChanges();

                }
                //cek subfolder
                searchSubFolder(path + "\\"+ dirName, dirName);
                
            }
        }

        private void searchSubFolder(string path, string namaFolder)
        {
            var listFolder = (from folder in Directory.GetDirectories(path) orderby folder ascending select folder);
            foreach (var w in listFolder)
            {
                // ambil id dari folder 
                var idFolder = (from folder in db.tbl_Folders select folder).Where(q => q.nama == namaFolder.Trim()).SingleOrDefault();
                
                // ambil nama file 
                string dirName = new DirectoryInfo(w.ToString()).Name;

                // Cek folder apakah sudah ada apa belum
                var cekFolder = (from subfolder in db.tbl_subFolders select subfolder).Where(q => q.namaSubFolder == dirName.ToString()).Where(y => y.idFolder == idFolder.id).SingleOrDefault();
                if (cekFolder == null)
                {
                    tbl_subFolder subfolder = new tbl_subFolder();
                    subfolder.idFolder = idFolder.id;
                    subfolder.namaSubFolder = dirName;

                    db.tbl_subFolders.InsertOnSubmit(subfolder);
                    db.SubmitChanges();
                }

                //cek File
                searchFile(path + "\\" + dirName, Convert.ToInt32(idFolder.id));
            }
        }

        private void searchFile(string path, int idFolder)
        {
            var listFile = (from file in Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories) orderby file descending select file);

            // ambil nama subdirectory
            string subdirName = new DirectoryInfo(path).Name;


            foreach (var w in listFile.ToArray())
            {
                // ambil id dari subFolder
                var idSubFolder = (from subFolder in db.tbl_subFolders select subFolder).Where(q => q.idFolder == idFolder).Where(y => y.namaSubFolder == subdirName).SingleOrDefault();

                // ambil nama file 
                string fileName = new DirectoryInfo(w.ToString()).Name;

                // Cek file apakah sudah ada apa belum
                var cekFile = (from file in db.tbl_Files select file).Where(q => q.namaFile == fileName.ToString().Trim()).Where(y => y.idSubFolder == idSubFolder.id).SingleOrDefault();
                if (cekFile == null)
                {
                    tbl_File fileInput = new tbl_File();
                    fileInput.idSubFolder = idSubFolder.id;
                    fileInput.namaFile = fileName;

                    fileInput.path = w;
                    
                    // baca isi didalam file masukan ke database
                    fileInput.isiFile = readFile(w);

                    fileInput.noKartu = fileName.Split('-')[0];
                    fileInput.tglInput = DateTime.Now;
                    fileInput.status = false;

                    //Ambil path tanpa filename
                    string pathNoFilename = Path.GetDirectoryName(w.ToString());


                    if (fileName != "PCID.txt")
                    {
                        string PCIDtxt = readFile(pathNoFilename + "\\PCID.txt");

                        if (PCIDtxt != "")
                            fileInput.kunciSAM = CalculateProcess(PCIDtxt.Trim(), readFile(w).Trim());
                    }

                    //fileInput.kunciSAM =
                    db.tbl_Files.InsertOnSubmit(fileInput);
                    db.SubmitChanges();
                }

            }
        }

        public string CalculateProcess(string string1, string string2)
        {
            byte[] opr1ba = new byte[32];
            opr1ba = hexToByteArray(string1 + string1);

            byte[] opr2ba = new byte[32];
            opr2ba = hexToByteArray(string2);

            byte[] buff = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                buff[i] = Convert.ToByte((int)(opr1ba[i] ^ opr2ba[i]));
            }

            return convertByteArrayToHexString(buff, buff.Length);
        }

        public byte[] hexToByteArray(string hexString)
        {
            int bytesCount = (hexString.Length) / 2;
            byte[] bytes = new byte[bytesCount];
            for (int x = 0; x < bytesCount; ++x)
            {
                bytes[x] = Convert.ToByte(hexString.Substring(x * 2, 2), 16);
            }
            return bytes;
        }

        public string convertByteArrayToHexString(byte[] recBuff, int recvLen)
        {
            string tmp = String.Empty;

            for (int i = 0; i < recvLen; i++)
            {
                tmp += string.Format("{0:X2}", recBuff[i]);
            }

            return tmp;
        }

        private void path_txt_MouseClick(object sender, MouseEventArgs e)
        {
            folderBrowserDialog1.ShowDialog();

            if (folderBrowserDialog1.SelectedPath != "")
            {
                path_txt.Text = folderBrowserDialog1.SelectedPath;
            }

        }

        private void generate_btn_Click(object sender, EventArgs e)
        {
            // Generate kedalam registry
            var cariSAM = (from cari in db.tbl_Files select cari).Where(q => q.noKartu == noSAM_txt.Text.Trim()).SingleOrDefault();

            if (cariSAM != null)
            {
                // buat registri 
                if (path_txt.Text != "")
                {
                    pathSimpan = path_txt.Text;
                    createFile(pathSimpan + "\\" + cariSAM.noKartu+".reg");
                    writeFileReg(pathSimpan + cariSAM.noKartu+".reg", cariSAM.noKartu, cariSAM.kunciSAM);
                    writeFileBat(pathSimpan + cariSAM.noKartu + ".bat", cariSAM.noKartu);
                    cariSAM.status = true;
                    db.SubmitChanges();
                    statistik();
                    MessageBox.Show("Success");
                }
                else
                {
                    MessageBox.Show("Masukkan Path");
                }
            }
            else
            {
                MessageBox.Show("No SAM harus diisi");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            statistik();
        }

        private void statistik()
        {
            // totalFileSAM
            var totalFileSAM = (from total in db.tbl_Files select total).Where(q => q.namaFile != "PCID.txt").Count();
            totalFileSAM_lbl.Text = totalFileSAM.ToString();

            // jumlah PCID
            var pcidCount = (from pcid in db.tbl_Files select pcid).Where(q => q.namaFile == "PCID.txt").Count();
            pcid_lbl.Text = pcidCount.ToString();

            // Generate
            var generateCount = (from generate in db.tbl_Files select generate).Where(q => q.status == true).Count();
            generate_lbl.Text = generateCount.ToString();

            // Available
            var availableCount = (from available in db.tbl_Files select available).Where(q => q.status == false).Count();
            available_lbl.Text = availableCount.ToString();

            // datagridview
            var datagrid = (from data in db.jmlStatFolders select new{ Folder = data.nama, Jml_SAM = data.jml}).OrderBy(q => q.Folder);
            statistik_dgv.DataSource = datagrid;
        }

        public void createFile(string path)
        {
            FileStream fs = null;
            if (!File.Exists(path))
            {
                using (fs = File.Create(path))
                {

                }
            }
        }

        private string readFile(string path)
        {
            string textline = "";

            StreamReader objReader;
            objReader = new System.IO.StreamReader(path);

            do
            {
                textline = textline + objReader.ReadLine();

            } while (objReader.Peek() != -1);

            objReader.Close();

            return textline;
        }

        private void writeFileReg(string path, string noKartu, string kunciSAM)
        {
            //if (File.Exists(path))
            //{
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write("Windows Registry Editor Version 5.00\r\n[HKEY_LOCAL_MACHINE\\SOFTWARE\\4C454E\\53414D5F4944]\r\n\"56414C55455F4944\"=\""+kunciSAM+"\"");
                }
            //}
        }

        private void writeFileBat(string path, string noKartu)
        {
            //if (File.Exists(path))
            //{
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write("start " + noKartu + ".reg\r\npause\r\n del "+noKartu+".reg\r\ndel "+noKartu+".bat");
                }
            //}
        }

        private void noSAM_txt_KeyDown(object sender, KeyEventArgs e)
        {
                if (e.KeyCode == Keys.Enter)
                {
                    // Generate kedalam registry
                    var cariSAM = (from cari in db.tbl_Files select cari).Where(q => q.noKartu == noSAM_txt.Text.Trim()).SingleOrDefault();

                    if (cariSAM != null)
                    {
                        // buat registri 
                        if (path_txt.Text != "")
                        {
                            pathSimpan = path_txt.Text;
                            createFile(pathSimpan + "\\" + cariSAM.noKartu + ".reg");
                            writeFileReg(pathSimpan + cariSAM.noKartu + ".reg", cariSAM.noKartu, cariSAM.kunciSAM);
                            writeFileBat(pathSimpan + cariSAM.noKartu + ".bat", cariSAM.noKartu);
                            cariSAM.status = true;
                            db.SubmitChanges();
                            statistik();
                            DialogResult r = MessageBox.Show("Success", "Konfirmasi", MessageBoxButtons.OK);
                            if (r.ToString() == "OK")
                            {
                                noSAM_txt.Text = "";
                            }
                        }
                        else
                        {
                            MessageBox.Show("Masukkan Path");
                        }
                    }
                    else
                    {
                        MessageBox.Show("No SAM harus diisi");
                    }       
                }
        }

        
    }
}
