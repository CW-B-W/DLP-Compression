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

        private void SaveBitplanes(bool[,,,] bitplanes)
        {
            int n_depth = bitplanes.GetLength(0);
            int n_color = bitplanes.GetLength(1);
            int width = bitplanes.GetLength(2);
            int height = bitplanes.GetLength(3);
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowDialog();
                if (fbd.SelectedPath == "")
                    return;
                for (int t = 0; t < n_depth; ++t)
                {
                    for (int c = 0; c < n_color; ++c)
                    {
                        string color_name = null;
                        Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        Color[,] colors = new Color[bmp.Width, bmp.Height];
                        if (c == 0)
                        {
                            color_name = "R";
                            for (int i = 0; i < width; ++i)
                            {
                                for (int j = 0; j < height; ++j)
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
                            for (int i = 0; i < width; ++i)
                            {
                                for (int j = 0; j < height; ++j)
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
                            for (int i = 0; i < width; ++i)
                            {
                                for (int j = 0; j < height; ++j)
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
            }
        }

        private List<byte>[,,] EncodeBitPlane(bool[,,,] bitplanes)
        {
            // encoded_planes[depth, color, y_coordiate]
            int n_depth = bitplanes.GetLength(0);
            int n_color = bitplanes.GetLength(1);
            int width = bitplanes.GetLength(2);
            int height = bitplanes.GetLength(3);
            List<byte>[,,] encoded_planes = new List<byte>[n_depth, n_color, height];
            for (int t = 0; t < 8; ++t)
            {
                for (int c = 0; c < 3; ++c)
                {
                    byte[] raw_data = new byte[(width+7)/8];

                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; x += 8)
                        {
                            byte b = 0;
                            for (int ofs = 0; ofs < 8; ++ofs)
                            {
                                b <<= 1;
                                if (x < width)
                                    b |= Convert.ToByte(bitplanes[t, c, x+ofs, y]);
                            }
                            raw_data[x/8] = b;
                        }
                        List<byte> encoded = TQ.CompressionAlgorithms.RLE_Encode_TI(raw_data);
                        encoded_planes[t, c, y] = encoded;
                        List<byte> decoded = TQ.CompressionAlgorithms.RLE_Decode_TI(encoded_planes[t, c, y].ToArray());
                        for (int i = 0; i < raw_data.Length; ++i)
                        {
                            if (raw_data[i] != decoded[i])
                                throw new Exception("Wront here");
                        }
                    }
                }
            }
            return encoded_planes;
        }

        private bool[,,,] DecodeBitPlane(List<byte>[,,] encoded_planes)
        {
            bool[,,,] bitplanes = null;
            int n_depth = encoded_planes.GetLength(0);
            int n_color = encoded_planes.GetLength(1);
            int height = encoded_planes.GetLength(2);
            int width = 0;
            for (int t = 0; t < n_depth; ++t)
            {
                for (int c = 0; c < n_color; ++c)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        List<bool> row = new List<bool>();
                        List<byte> decoded = TQ.CompressionAlgorithms.RLE_Decode_TI(encoded_planes[t, c, y].ToArray());
                        for (int i = 0; i < decoded.Count; ++i)
                        {
                            byte b = decoded[i];
                            for (int ofs = 7; ofs >= 0; --ofs)
                            {
                                int x = i * 8 + ofs;
                                bool bit = Convert.ToBoolean((b >> ofs) & 1);
                                row.Add(bit);
                            }
                        }
                        width = row.Count;
                        if (bitplanes == null)
                        {
                            bitplanes = new bool[n_depth, n_color, width, height];
                        }
                        else if (row.Count != width)
                        {
                            throw new Exception("Error: Unmatched bitplane width");
                        }

                        for (int x = 0; x < row.Count; ++x)
                        {
                            bitplanes[t, c, x, y] = row[x];
                        }
                    }
                }
            }

            return bitplanes;
        }

        private void processToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool[,,,] bitplanes = ConvertToBitplanes(gBitmap);
            SaveBitplanes(bitplanes);
            List<byte>[,,] encoded_planes = EncodeBitPlane(bitplanes);
            bool[,,,] decoded_planes = DecodeBitPlane(encoded_planes);

            int n_depth = bitplanes.GetLength(0);
            int n_color = bitplanes.GetLength(1);
            int width = bitplanes.GetLength(2);
            int height = bitplanes.GetLength(3);

            int raw_data_length = n_depth * n_color * width * height / 8;
            int encoded_length = 0;
            for (int i = 0; i < encoded_planes.GetLength(0); ++i)
            {
                for (int j = 0; j < encoded_planes.GetLength(1); ++j)
                {
                    for (int k = 0; k < encoded_planes.GetLength(2); ++k)
                    {
                        encoded_length += encoded_planes[i, j, k].Count;
                    }
                }
            }

            textBox1.Text = "";
            textBox1.Text += $"raw_data_length = {raw_data_length} bytes\r\n";
            textBox1.Text += $"encoded_length = {encoded_length} bytes\r\n";
            textBox1.Text += $"compression_ratio = {1.0 * raw_data_length / encoded_length}\r\n";

            // Verify Encode/Decode
            //for (int t = 0; t < n_depth; ++t)
            //{
            //    for (int c = 0; c < n_color; ++c)
            //    {
            //        for (int x = 0; x < width; ++x)
            //        {
            //            for (int y = 0; y < height; ++y)
            //            {
            //                if (decoded_planes[t, c, x, y] != bitplanes[t, c, x, y])
            //                {
            //                    throw new Exception("Wrong answer");
            //                }
            //            }
            //        }
            //    }
            //}
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            byte[] raw_data = new byte[76];
            for (int i = 0; i < raw_data.Length; ++i)
                raw_data[i] = 0xFF;
            List<byte> encoded = TQ.CompressionAlgorithms.RLE_Encode_TI(raw_data);
            List<byte> decoded = TQ.CompressionAlgorithms.RLE_Decode_TI(encoded.ToArray());
            Console.WriteLine(raw_data.Length);

            Console.WriteLine(encoded.Count);
            for (int i = 0; i < encoded.Count; ++i)
                Console.Write(encoded[i].ToString("X") + " ");
            Console.WriteLine("");

            Console.WriteLine(decoded.Count);
            for (int i = 0; i < decoded.Count; ++i)
                Console.Write(decoded[i].ToString("X") + " ");
            Console.WriteLine("");

            Console.WriteLine(1.0 * decoded.Count / encoded.Count);
        }
    }
}
