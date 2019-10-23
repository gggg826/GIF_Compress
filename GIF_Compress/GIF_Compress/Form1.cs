using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Encoder = System.Drawing.Imaging.Encoder;

namespace GIF_Compress
{
    public partial class Form1 : Form
    {
        private List<string> mAllgif;
        private int mTargetW, mTargetH;
        private float mTargetFrameDelay;
        private string mSaveDir;

        public Form1()
        {
            InitializeComponent();
            InitListView();
            mTargetW = int.Parse(textBox1.Text);
            mAllgif = new List<string>();
            mSaveDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"/temp/";
            if (!Directory.Exists(mSaveDir))
            {
                Directory.CreateDirectory(mSaveDir);
            }

        }

        private void InitListView()
        {
            listView1.Columns.Add("文件名", -2, HorizontalAlignment.Left);
            listView1.Columns[0].Width = listView1.Width;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(mAllgif == null)
            {
                return;
            }

            MessageBox.Show(GetDelay(mAllgif[0]).ToString());
            return;

            for (int i = 0, c = mAllgif.Count; i < c; i++)
            {
                CompressGIF(mAllgif[i]);
            }

            MessageBox.Show("压缩完成!", "");
        }
        private void button2_Click(object sender, EventArgs e)
        {
            mAllgif.Clear();
            listView1.Clear();
            InitListView();
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] gifs = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            string fileName = string.Empty;
            for (int i = 0,c = gifs.Length; i < c; i++)
            {
                fileName = gifs[i];
                if(mAllgif.Contains(fileName))
                {
                    continue;
                }
                if (Path.GetExtension(fileName).ToLower().Equals(".gif"))
                {
                    mAllgif.Add(fileName);
                    listView1.Items.Add(fileName);
                }
            }
        }
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;  //重要代码：表明是所有类型的数据，比如文件路径
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if(!int.TryParse(textBox1.Text, out mTargetW) || mTargetW <= 0)
            {
                MessageBox.Show("请输入正整数!", "错误");
            }
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox1.Text, out mTargetH) || mTargetH <= 0)
            {
                MessageBox.Show("请输入正整数!", "错误");
            }
        }

        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            mTargetFrameDelay = 1 / (float)trackBar1.Value;
            label4.Text = trackBar1.Value.ToString();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
        }






        private void CompressGIF(string gifInPath)
        {
            //原图路径
            //string imgPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\2.gif";

            //做一层保护，判断当前输出文件夹存在与否
            if (!Directory.Exists(mSaveDir))
            {
                Directory.CreateDirectory(mSaveDir);
            }
            string savePath = mSaveDir + Path.GetFileName(gifInPath);
            if(File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            //原图
            Image img = Image.FromFile(gifInPath);
            //宽不够指定值的不缩放
            if (img.Width > mTargetW)
            {
                mTargetH = (int)((float)img.Height / img.Width * mTargetW);
                //新图第一帧
                Image new_img = new Bitmap(mTargetW, mTargetH);
                //新图其他帧
                Image new_imgs = new Bitmap(mTargetW, mTargetH);
                //新图第一帧GDI+绘图对象
                Graphics g_new_img = Graphics.FromImage(new_img);
                //新图其他帧GDI+绘图对象
                Graphics g_new_imgs = Graphics.FromImage(new_imgs);
                //配置新图第一帧GDI+绘图对象
                g_new_img.CompositingMode = CompositingMode.SourceCopy;//overdraw 覆盖、混合
                g_new_img.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g_new_img.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g_new_img.SmoothingMode = SmoothingMode.HighQuality;
                g_new_img.Clear(Color.FromKnownColor(KnownColor.Transparent));
                //配置其他帧GDI+绘图对象
                g_new_imgs.CompositingMode = CompositingMode.SourceCopy;
                g_new_imgs.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g_new_imgs.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g_new_imgs.SmoothingMode = SmoothingMode.HighQuality;
                g_new_imgs.Clear(Color.FromKnownColor(KnownColor.Transparent));
                //遍历维数
                foreach (Guid gid in img.FrameDimensionsList)
                {
                    //因为是缩小GIF文件所以这里要设置为Time
                    //如果是TIFF这里要设置为PAGE
                    FrameDimension f = FrameDimension.Time;
                    //获取总帧数
                    int count = img.GetFrameCount(f);
                    //保存标示参数
                    Encoder encoder = Encoder.SaveFlag;
                    //
                    EncoderParameters ep = null;
                    //图片编码、解码器
                    ImageCodecInfo ici = null;
                    //图片编码、解码器集合
                    ImageCodecInfo[] icis = ImageCodecInfo.GetImageDecoders();
                    //为 图片编码、解码器 对象 赋值
                    foreach (ImageCodecInfo ic in icis)
                    {
                        if (ic.FormatID == ImageFormat.Gif.Guid)
                        {
                            ici = ic;
                            break;
                        }
                    }
                    //每一帧
                    for (int c = 0; c < count; c++)
                    {
                        //选择由维度和索引指定的帧
                        img.SelectActiveFrame(f, c);
                        //第一帧
                        if (c == 0)
                        {
                            //将原图第一帧画给新图第一帧
                            g_new_img.DrawImage(img, new Rectangle(0, 0, mTargetW, mTargetH), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                            //把振频和透明背景调色板等设置复制给新图第一帧
                            for (int i = 0; i < img.PropertyItems.Length; i++)
                            {
                                new_img.SetPropertyItem(img.PropertyItems[i]);
                            }
                            ep = new EncoderParameters(1);
                            //第一帧需要设置为MultiFrame
                            ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.MultiFrame);
                            //保存第一帧
                            new_img.Save(savePath, ici, ep);
                        }
                        //其他帧
                        else
                        {
                            //把原图的其他帧画给新图的其他帧
                            g_new_imgs.DrawImage(img, new Rectangle(0, 0, mTargetW, mTargetH), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                            //把振频和透明背景调色板等设置复制给新图第一帧
                            for (int i = 0; i < img.PropertyItems.Length; i++)
                            {
                                new_imgs.SetPropertyItem(img.PropertyItems[i]);
                            }
                            ep = new EncoderParameters(1);
                            //如果是GIF这里设置为FrameDimensionTime
                            //如果为TIFF则设置为FrameDimensionPage
                            ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.FrameDimensionTime);
                            //向新图添加一帧
                            new_img.SaveAdd(new_imgs, ep);
                        }
                    }
                    ep = new EncoderParameters(1);
                    //关闭多帧文件流
                    ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.Flush);
                    new_img.SaveAdd(ep);
                }

                //释放文件
                img.Dispose();
                new_img.Dispose();
                new_imgs.Dispose();
                g_new_img.Dispose();
                g_new_imgs.Dispose();
            }
            else
            {
                File.Copy(gifInPath, savePath);
            }
        }


        private int GetDelay(string sfile)
        {
            Image img = Image.FromFile(sfile);//加载Gif图片
            FrameDimension dim = new FrameDimension(img.FrameDimensionsList[0]);
            int framcount = img.GetFrameCount(dim);
            if (framcount <= 1)
                return 0;
            else
            {
                int delay = 0;
                bool stop = false;
                for (int i = 0; i < framcount; i++)//遍历图像帧
                {
                    if (stop == true)
                        break;
                    img.SelectActiveFrame(dim, i);//激活当前帧
                    for (int j = 0; j < img.PropertyIdList.Length; j++)//遍历帧属性
                    {
                        if ((int)img.PropertyIdList.GetValue(j) == 0x5100)//如果是延迟时间
                        {
                            PropertyItem pItem = (PropertyItem)img.PropertyItems.GetValue(j);//获取延迟时间属性
                            byte[] delayByte = new byte[4];//延迟时间，以1/100秒为单位
                            delayByte[0] = pItem.Value[i * 4];
                            delayByte[1] = pItem.Value[1 + i * 4];
                            delayByte[2] = pItem.Value[2 + i * 4];
                            delayByte[3] = pItem.Value[3 + i * 4];
                            delay = BitConverter.ToInt32(delayByte, 0) * 10; //乘以10，获取到毫秒
                            stop = true;
                            break;
                        }
                    }


                }
                return delay;
            }
        }
    }
}
