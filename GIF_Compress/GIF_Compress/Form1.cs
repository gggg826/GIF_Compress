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
        private int mTargetW, mTargetFrameDelay;
        private string mSaveDir;
        private string mCurrentSaveFilePath;

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
            SetTargetFrameCount();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mAllgif == null)
            {
                return;
            }

            for (int i = 0, c = mAllgif.Count; i < c; i++)
            {
                CheckSavePath(mAllgif[i]);
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
            for (int i = 0, c = gifs.Length; i < c; i++)
            {
                fileName = gifs[i];
                if (mAllgif.Contains(fileName))
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
            if (!int.TryParse(textBox1.Text, out mTargetW) || mTargetW <= 0)
            {
                MessageBox.Show("请输入正整数!", "错误");
            }
        }

        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            SetTargetFrameCount();
        }

        private void SetTargetFrameCount()
        {
            mTargetFrameDelay = 100 / trackBar1.Value;//单位为0.1毫秒，与gif记录的延迟一致
            label4.Text = trackBar1.Value.ToString();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = true;
        }




        private void CheckSavePath(string sourcePath)
        {
            //做一层保护，判断当前输出文件夹存在与否
            if (!Directory.Exists(mSaveDir))
            {
                Directory.CreateDirectory(mSaveDir);
            }
            mCurrentSaveFilePath = mSaveDir + Path.GetFileName(sourcePath);
            if (File.Exists(mCurrentSaveFilePath))
            {
                File.Delete(mCurrentSaveFilePath);
            }
        }

        private void CompressGIF(string sourcePath)
        {
            //原图
            Image img = Image.FromFile(sourcePath);
            int saveW, saveH;
            //宽不够指定值的不缩放
            if (img.Width > mTargetW)
            {
                saveW = mTargetW;
                saveH = (int)((float)img.Height / img.Width * saveW);
            }
            else
            {
                saveW = img.Width;
                saveH = img.Height;
            }

            //新图第一帧
            Image new_img = new Bitmap(saveW, saveH);
            //新图第一帧GDI+绘图对象
            Graphics g_new_img = Graphics.FromImage(new_img);
            //配置新图第一帧GDI+绘图对象
            g_new_img.CompositingMode = CompositingMode.SourceCopy;//overdraw 覆盖、混合
            g_new_img.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g_new_img.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g_new_img.SmoothingMode = SmoothingMode.HighQuality;
            g_new_img.Clear(Color.FromKnownColor(KnownColor.Transparent));

            //新图其他帧
            Image new_imgs = new Bitmap(saveW, saveH);
            //新图其他帧GDI+绘图对象
            Graphics g_new_imgs = Graphics.FromImage(new_imgs);
            //配置其他帧GDI+绘图对象
            g_new_imgs.CompositingMode = CompositingMode.SourceCopy;
            g_new_imgs.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g_new_imgs.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g_new_imgs.SmoothingMode = SmoothingMode.HighQuality;
            g_new_imgs.Clear(Color.FromKnownColor(KnownColor.Transparent));
            //遍历维数
            foreach (Guid gid in img.FrameDimensionsList)
            {
                //计算减帧数据
                PropertyItem[] propertyItems = img.PropertyItems;
                List<int> remaindFramIndexArr = FilterFramList(mTargetFrameDelay, ref propertyItems);


                //因为是缩小GIF文件所以这里要设置为Time
                //如果是TIFF这里要设置为PAGE
                FrameDimension f = FrameDimension.Time;
                //获取总帧数
                int count = img.GetFrameCount(f);
                //减帧后的可用帧index
                int nextFramIndex = 0;
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
                    if(nextFramIndex > remaindFramIndexArr.Count - 1)
                    {
                        break;
                    }

                    if(c != remaindFramIndexArr[nextFramIndex])
                    {
                        continue;
                    }

                    //选择由维度和索引指定的帧
                    img.SelectActiveFrame(f, c);
                    //第一帧
                    if (c == 0)
                    {
                        //将原图第一帧画给新图第一帧
                        g_new_img.DrawImage(img, new Rectangle(0, 0, saveW, saveH), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                        //把振频和透明背景调色板等设置复制给新图第一帧
                        for (int i = 0; i < img.PropertyItems.Length; i++)
                        {
                            new_img.SetPropertyItem(propertyItems[i]);
                        }
                        ep = new EncoderParameters(1);
                        //第一帧需要设置为MultiFrame
                        ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.MultiFrame);
                        //保存第一帧
                        new_img.Save(mCurrentSaveFilePath, ici, ep);
                    }
                    //其他帧
                    else
                    {
                        //把原图的其他帧画给新图的其他帧
                        g_new_imgs.DrawImage(img, new Rectangle(0, 0, saveW, saveH), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                        //把振频和透明背景调色板等设置复制给新图第一帧
                        //for (int i = 0; i < img.PropertyItems.Length; i++)
                        //{
                        //    new_imgs.SetPropertyItem(img.PropertyItems[i]);
                        //}
                        ep = new EncoderParameters(1);
                        //如果是GIF这里设置为FrameDimensionTime
                        //如果为TIFF则设置为FrameDimensionPage
                        ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.FrameDimensionTime);
                        //向新图添加一帧
                        new_img.SaveAdd(new_imgs, ep);
                    }
                    nextFramIndex++;
                }
                ep = new EncoderParameters(1);
                //关闭多帧文件流
                ep.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.Flush);
                new_img.SaveAdd(ep);

                //释放文件
                img.Dispose();
                new_img.Dispose();
                new_imgs.Dispose();
                g_new_img.Dispose();
                g_new_imgs.Dispose();
            }
        }

        /// <summary>
        /// 重新生成减帧后的延迟数据，并返回剩余帧的index列表
        /// </summary>
        /// <param name="targetDelayTime"></param>
        /// <param name="pItem"></param>
        /// <returns></returns>
        private List<int> FilterFramList(int targetDelayTime, ref PropertyItem[] pItem)
        {
            int i, c = pItem.Length;
            int pItemDelayIndex = 0;//pitem中用于存储帧延迟信息的数组下标
            byte[] dArray = null;//用于存储源帧延时的byte数组
            for (i = 0; i < c; i++)
            {
                if (pItem[i].Id == 0x5100)//如果是延迟时间,https://docs.microsoft.com/zh-cn/dotnet/api/system.drawing.imaging.propertyitem.id?view=netframework-4.8#System_Drawing_Imaging_PropertyItem_Id
                {
                    pItemDelayIndex = i;
                    dArray = pItem[i].Value;
                    break;
                }
            }

            List<int> fResult = new List<int>();//剔除延迟时间不够的帧后，保存符合条件的帧
            List<byte> fDelayBytes = new List<byte>();
            short lastFramDelay = 0;//当前遍例帧与前一帧之间的延迟时间
            int totalFramDelay = 0;//有删帧,重新计算延迟

            fResult.Add(0);//添加第一帧,从第二帧开始遍例
            int fIndex = 1;//记录目标帧
            for (i = 4, c = dArray.Length; i <= c; i += 4)
            {
                byte[] delayByte = new byte[4];//前一帧延迟时间，以1/100秒为单位
                delayByte[0] = dArray[i - 4];
                delayByte[1] = dArray[i - 3];
                delayByte[2] = dArray[i - 2];
                delayByte[3] = dArray[i - 1];
                lastFramDelay = BitConverter.ToInt16(delayByte, 0); //单位为0.1毫秒
                totalFramDelay += lastFramDelay;
                if (totalFramDelay < targetDelayTime)
                {
                    //处理最后一帧延迟时间
                    if(i + 4 > c)
                    {
                        fDelayBytes.AddRange(BitConverter.GetBytes(totalFramDelay));
                    }
                    fIndex++;
                    continue;
                }
                
                fResult.Add(fIndex);
                fDelayBytes.AddRange(BitConverter.GetBytes(totalFramDelay));//这里是前一帧的延迟时间

                //处理最后一帧延迟时间
                if (i + 4 > c)
                {
                    fDelayBytes.AddRange(BitConverter.GetBytes(totalFramDelay));
                }
                fIndex++;
                totalFramDelay = 0;
            }

            pItem[pItemDelayIndex].Value = fDelayBytes.ToArray();//重新设定减帧后的各帧延迟时间
            return fResult;//返回减帧后的剩余帧在原数据中的Index列表
        }
    }
}
