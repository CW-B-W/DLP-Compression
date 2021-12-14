using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace DLP_Compression
{
    public partial class Form1 : Form
    {
        Bitmap gBitmap = null;
        public Form1()
        {
            InitializeComponent();
        }

        private bool[,,,] ConvertToBitplanes(Bitmap bmp)
        {
            Color[,] colors = TQ.BitmapFasterIO.LoadFromBitmap(bmp);
            bool[,,,] bitplanes = new bool[8, 3, bmp.Width, bmp.Height];
            for (int t = 0; t < 8; ++t)
            {
                for (int x = 0; x < bmp.Width; ++x)
                {
                    for (int y = 0; y < bmp.Height; ++y)
                    {
                        byte R_val = colors[x, y].R;
                        byte G_val = colors[x, y].G;
                        byte B_val = colors[x, y].B;

                        int threshold = 1 << t;
                        bitplanes[t, 0, x, y] = (int)R_val > threshold;
                        bitplanes[t, 1, x, y] = (int)G_val > threshold;
                        bitplanes[t, 2, x, y] = (int)B_val > threshold;
                    }
                }
            }

            return bitplanes;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gBitmap != null)
            {
                gBitmap.Dispose();
            }
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.ShowDialog();
                gBitmap = (Bitmap)Image.FromFile(ofd.FileName);

                pictureBox1.Image = gBitmap;

                //Console.WriteLine(gBitmap.PixelFormat);
                //Color[,] colors = TQ.BitmapFasterIO.LoadFromBitmap((Bitmap)gBitmap);
                //for (int i = 0; i < colors.GetLength(0); ++i)
                //{
                //    for (int j = 0; j < 100; ++j)
                //        colors[i, j] = Color.Black;
                //}
                //Bitmap bmp_save = new Bitmap(gBitmap.Width, gBitmap.Height, gBitmap.PixelFormat);
                //TQ.BitmapFasterIO.SaveToBitmap(bmp_save, colors);
                //bmp_save.Save(ofd.FileName + "save.bmp");
            }
        }

        private void toBitplansToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool[,,,] bitplanes = ConvertToBitplanes(gBitmap);
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowDialog();
                for (int t = 0; t < 8; ++t)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        string color_name = null;
                        Bitmap bmp = new Bitmap(bitplanes.GetLength(2), bitplanes.GetLength(3), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        Color[,] colors = new Color[bmp.Width, bmp.Height];
                        if (c == 0)
                        {
                            color_name = "R";
                            for (int i = 0; i < colors.GetLength(0); ++i)
                            {
                                for (int j = 0; j < colors.GetLength(1); ++j)
                                {
                                    if (bitplanes[t, c, i, j])
                                        colors[i, j] = Color.Red;
                                    else
                                        colors[i, j] = Color.White;
                                }
                            }
                        }
                        else if (c == 1)
                        {
                            color_name = "G";
                            for (int i = 0; i < colors.GetLength(0); ++i)
                            {
                                for (int j = 0; j < colors.GetLength(1); ++j)
                                {
                                    if (bitplanes[t, c, i, j])
                                        colors[i, j] = Color.Green;
                                    else
                                        colors[i, j] = Color.White;
                                }
                            }
                        }
                        else if (c == 2)
                        {
                            color_name = "B";
                            for (int i = 0; i < colors.GetLength(0); ++i)
                            {
                                for (int j = 0; j < colors.GetLength(1); ++j)
                                {
                                    if (bitplanes[t, c, i, j])
                                        colors[i, j] = Color.Blue;
                                    else
                                        colors[i, j] = Color.White;
                                }
                            }
                        }
                        TQ.BitmapFasterIO.SaveToBitmap(bmp, colors);
                        string savepath = fbd.SelectedPath + @"\bitplane_" + color_name + "_" + t.ToString() + ".bmp";
                        bmp.Save(savepath);
                    }
                }
            }
        }
    }
}
