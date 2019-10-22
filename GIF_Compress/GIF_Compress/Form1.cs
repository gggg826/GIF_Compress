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
        private string[] mAllgif = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(mAllgif == null)
            {
                return;
            }

            for (int i = 0, c = mAllgif.Length; i < c; i++)
            {
                CompressGIF(mAllgif[i]);
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            mAllgif = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            for (int i = 0,c = mAllgif.Length; i < c; i++)
            {
                listView1.Items.Add(mAllgif[i]);
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





        private void CompressGIF(string gifInPath)
        {
            //原图路径
            //string imgPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\2.gif";
            //原图
            Image img = Image.FromFile(gifInPath);
            //不够100*100的不缩放
            if (img.Width > 100 && img.Height > 100)
            {
                //新图第一帧
                Image new_img = new Bitmap(100, 100);
                //新图其他帧
                Image new_imgs = new Bitmap(100, 100);
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
                            g_new_img.DrawImage(img, new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                            //把振频和透明背景调色板等设置复制给新图第一帧
                            for (int i = 0; i < img.PropertyItems.Length; i++)
                            {
                                new_img.SetPropertyItem(img.PropertyItems[i]);
                            }
                            ep = new EncoderParameters(1);
                            //第一帧需要设置为MultiFrame
                            ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.MultiFrame);
                            //保存第一帧
                            new_img.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"/temp/" + Path.GetFileName(gifInPath), ici, ep);
                        }
                        //其他帧
                        else
                        {
                            //把原图的其他帧画给新图的其他帧
                            g_new_imgs.DrawImage(img, new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
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
        }
    }
}
