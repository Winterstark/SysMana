using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SysMana
{
    public class Meter
    {
        public string data, dataSubsource, vis;
        public int leftMargin, topMargin, min, max, zoom, currDataValue;
        public string clickAction, dragFileAction, mWheelAction;

        DateTime prevDraw;
        string imgsDir;

        public string prefix, postfix;
        public bool onlyValue;
        
        public string spinner;
        Image spinnerImg;
        public int minSpin, maxSpin;
        double spin;

        public string background, foreground, vector;
        Image backgroundImg, foregroundImg;

        public string imgSeqDir;
        Image[] imgSeq;

        public int graphW, graphH, graphStepW, graphLineW, graphInterval;
        public Color graphLineColor;
        public bool graphBorder, graphTexFront;
        public string graphTex;
        List<float> graphValues;
        Pen graphPen;
        Image graphTexImg;
        DateTime prevGraphTick;

        public int left, h;
        Func<string, Image> LoadImg;
        Action<Image> DisposeImg;


        public Meter(string data, string vis, string imgsDir, Func<string, Image> LoadImg, Action<Image> DisposeImg)
        {
            this.data = data;
            this.vis = vis;
            this.imgsDir = imgsDir;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            prefix = "CPU usage: ";
            postfix = " %";
            leftMargin = 0;
            topMargin = 0;
            min = 0;
            max = 100;
            zoom = 100;
            vector = "Left to right";
            imgSeqDir = "";
            graphStepW = 1;
            graphLineW = 1;
            graphInterval = 25;
            graphLineColor = Color.Black;
        }

        public Meter(string lineFromFile, string imgsDir, Func<string, Image> LoadImg, Action<Image> DisposeImg)
        {
            this.imgsDir = imgsDir;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            string[] parts = lineFromFile.Split(new string[] { "--" }, StringSplitOptions.None);
            int i = 0;

            data = parts[i++];
            dataSubsource = parts[i++];
            vis = parts[i++];
            leftMargin = int.Parse(parts[i++]);
            topMargin = int.Parse(parts[i++]);
            min = int.Parse(parts[i++]);
            max = int.Parse(parts[i++]);
            zoom = int.Parse(parts[i++]);
            clickAction = parts[i++];
            dragFileAction = parts[i++];
            mWheelAction = parts[i++];

            prefix = parts[i++];
            postfix = parts[i++];
            onlyValue = bool.Parse(parts[i++]);

            spinner = parts[i++];
            minSpin = int.Parse(parts[i++]);
            maxSpin = int.Parse(parts[i++]);

            background = parts[i++];
            foreground = parts[i++];
            vector = parts[i++];

            imgSeqDir = parts[i++];

            graphW = int.Parse(parts[i++]);
            graphH = int.Parse(parts[i++]);
            graphStepW = int.Parse(parts[i++]);
            graphLineW = int.Parse(parts[i++]);
            graphInterval = int.Parse(parts[i++]);
            graphLineColor = Color.FromArgb(int.Parse(parts[i++]), int.Parse(parts[i++]), int.Parse(parts[i++]));
            graphBorder = bool.Parse(parts[i++]);
            graphTex = parts[i++];
            graphTexFront = bool.Parse(parts[i++]);

            LoadResources();
        }

        public void LoadResources()
        {
            DisposeImg(spinnerImg);
            DisposeImg(backgroundImg);
            DisposeImg(foregroundImg);
            DisposeImg(graphTexImg);

            if (imgSeq != null)
                foreach (Image img in imgSeq)
                    DisposeImg(img);
            
            switch (vis)
            {
                case "Spinner":
                    spinnerImg = LoadImg(imgsDir + spinner);
                    break;
                case "Progress bar":
                    backgroundImg = LoadImg(imgsDir + background);
                    foregroundImg = LoadImg(imgsDir + foreground);
                    break;
                case "Image sequence":
                    if (imgSeqDir != "" && Directory.Exists(imgsDir + "\\" + imgSeqDir))
                    {
                        string[] files = Misc.GetFilesInNaturalOrder(imgsDir + imgSeqDir);
                        imgSeq = new Image[files.Length];
                        
                        for (int i = 0; i < files.Length; i++)
                            imgSeq[i] = LoadImg(imgsDir + imgSeqDir + "\\" + Path.GetFileName(files[i]));
                    }
                    break;
                case "Graph":
                    graphTexImg = LoadImg(imgsDir + graphTex);
                    break;
            }
            
            //drawing resources
            graphPen = new Pen(graphLineColor, graphLineW);
        }

        public string FormatForFile()
        {
            return
                data + "--" + dataSubsource + "--" + vis + "--" + leftMargin + "--" + topMargin + "--" + min + "--" + max + "--" + zoom + "--" + clickAction + "--" + dragFileAction + "--" + mWheelAction + "--" +
                prefix + "--" + postfix + "--" + onlyValue + "--" +
                spinner + "--" + minSpin + "--" + maxSpin + "--" +
                background + "--" + foreground + "--" + vector + "--" +
                imgSeqDir + "--" +
                graphW + "--" + graphH + "--" + graphStepW + "--" + graphLineW + "--" + graphInterval + "--" + graphLineColor.R + "--" + graphLineColor.G + "--" + graphLineColor.B + "--" + graphBorder + "--" + graphTex + "--" + graphTexFront;
        }

        public void Draw(Graphics gfx, Font font, int fixedH, VertAlign align, ref int left, ref int h)
        {
            left += leftMargin;
            this.left = left;
            int y = topMargin;

            if (fixedH == 0)
                fixedH = h;

            switch (vis)
            {
                case "Text":
                    string output;
                    if (onlyValue)
                        output = data.ToString();
                    else
                        output = prefix + data.ToString() + postfix;

                    SizeF txtSize = gfx.MeasureString(output, font);
                    y += setAlignment(align, (int)txtSize.Height, fixedH);

                    gfx.DrawString(output, font, Brushes.Black, left, y);

                    left += (int)txtSize.Width;
                    this.h = (int)txtSize.Height;
                    h = Math.Max(h, this.h);
                    break;
                case "Spinner":
                    if (!(spinnerImg == null || prevDraw.Ticks == 0 || min == max))
                    {
                        float speed = interpolateData(currDataValue, minSpin, maxSpin);
                        double s = DateTime.Now.Subtract(prevDraw).TotalSeconds; //seconds elapsed since last draw
                        int destW = zoomLength(spinnerImg.Width, zoom);
                        int destH = zoomLength(spinnerImg.Height, zoom);

                        //left += spinMargin - destW / 2;

                        spin += speed * s;

                        y += setAlignment(align, destH, fixedH);

                        gfx.TranslateTransform(left + destW / 2, y + destH / 2);
                        gfx.RotateTransform((float)spin);
                        gfx.TranslateTransform(-destW / 2, -destH / 2);

                        gfx.DrawImage(spinnerImg, 0, 0, destW, destH);
                        gfx.ResetTransform();

                        left += destW;
                        this.h = destH;
                        h = Math.Max(h, this.h);
                    }
                    break;
                case "Progress bar":
                    if (!(backgroundImg == null || foregroundImg == null || min == max))
                    {
                        gfx.DrawImage(backgroundImg, left, y + setAlignment(align, zoomLength(backgroundImg.Height, zoom), fixedH), zoomLength(backgroundImg.Width, zoom), zoomLength(backgroundImg.Height, zoom));
                        
                        y += setAlignment(align, zoomLength(foregroundImg.Height, zoom), fixedH);
                        int progress;

                        switch (vector)
                        {
                            case "Left to right":
                                progress = (int)interpolateData(currDataValue, 0, foregroundImg.Width);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y, zoomLength(progress, zoom), zoomLength(foregroundImg.Height, zoom)), new Rectangle(0, 0, progress, foregroundImg.Height), GraphicsUnit.Pixel);
                                break;
                            case "Right to left":
                                progress = (int)interpolateData(currDataValue, 0, foregroundImg.Width);
                                gfx.DrawImage(foregroundImg, new Rectangle(left + zoomLength(foregroundImg.Width - progress, zoom), y, zoomLength(progress, zoom), zoomLength(foregroundImg.Height, zoom)), new Rectangle(foregroundImg.Width - progress, 0, progress, foregroundImg.Height), GraphicsUnit.Pixel);
                                break;
                            case "Bottom to top":
                                progress = (int)interpolateData(currDataValue, 0, foregroundImg.Height);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y + zoomLength(foregroundImg.Height - progress, zoom), zoomLength(foregroundImg.Width, zoom), zoomLength(progress, zoom)), new Rectangle(0, foregroundImg.Height - progress, foregroundImg.Width, progress), GraphicsUnit.Pixel);
                                break;
                            case "Top to bottom":
                                progress = (int)interpolateData(currDataValue, 0, foregroundImg.Height);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y, zoomLength(foregroundImg.Width, zoom), zoomLength(progress, zoom)), new Rectangle(0, 0, foregroundImg.Width, progress), GraphicsUnit.Pixel);
                                break;
                        }

                        left += Math.Max(zoomLength(backgroundImg.Width, zoom), zoomLength(foregroundImg.Width, zoom));
                        this.h = Math.Max(zoomLength(backgroundImg.Height, zoom), zoomLength(foregroundImg.Height, zoom));
                        h = Math.Max(h, this.h);
                    }
                    break;
                case "Image sequence":
                    if (!(imgSeq == null || imgSeq.Length == 0 || min == max))
                    {
                        int imgInd = (int)interpolateData(currDataValue, 0, imgSeq.Length);
                        imgInd = Math.Max(Math.Min(imgInd, imgSeq.Length - 1), 0);

                        int destW = zoomLength(imgSeq[imgInd].Width, zoom);
                        int destH = zoomLength(imgSeq[imgInd].Height, zoom);

                        y += setAlignment(align, destH, fixedH);

                        gfx.DrawImage(imgSeq[imgInd], left, y, destW, destH);

                        left += destW;
                        this.h = destH;
                        h = Math.Max(h, this.h);
                    }
                    break;
                case "Graph":
                    if (!(graphW == 0 || graphH == 0 || min == max))
                    {
                        //init
                        if (graphValues == null)
                            graphValues = new List<float>();
                        if (graphPen == null)
                            graphPen = new Pen(graphLineColor, graphLineW);

                        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if (DateTime.Now.Subtract(prevGraphTick).TotalMilliseconds >= graphInterval)
                        {
                            //get new value
                            graphValues.Add(interpolateData(currDataValue, graphH, 1));
                            if (graphValues.Count > Math.Ceiling((float)graphW / graphStepW) + 1)
                                graphValues.RemoveAt(0); //discard last value when graph too large

                            prevGraphTick = DateTime.Now;
                        }

                        //build graph path
                        GraphicsPath path = new GraphicsPath();
                        float x = left + graphW;
                        y += setAlignment(align, graphH, fixedH) + graphLineW / 2;

                        for (int i = graphValues.Count - 1; i >= 1; i--)
                        {
                            path.AddLine(x, y + graphValues[i], x - graphStepW, y + graphValues[i - 1]);
                            x -= graphStepW;
                        }

                        //draw graph
                        gfx.DrawPath(graphPen, path);

                        //fill texture
                        if (graphTexImg != null)
                        {
                            //finish path
                            if (graphTexFront)
                            {
                                path.AddLine(x, y + graphValues[0], x, y + graphH);
                                path.AddLine(x, y + graphH, left + graphW, y + graphH);
                                path.AddLine(left + graphW, y + graphH, left + graphW, y + graphValues[graphValues.Count - 1]);
                            }
                            else
                            {
                                path.AddLine(x, y + graphValues[0], x, y);
                                path.AddLine(x, y, left + graphW, y);
                                path.AddLine(left + graphW, y, left + graphW, y + graphValues[graphValues.Count - 1]);
                            }

                            //draw texture to img (because the texture can be an animated gif, and this will copy its current state)
                            int destW = (int)((float)zoom / 100 * graphTexImg.Width);
                            int destH = (int)((float)zoom / 100 * graphTexImg.Height);

                            Image temp = new Bitmap(destW, destH);
                            Graphics tempGfx = Graphics.FromImage(temp);

                            tempGfx.DrawImage(graphTexImg, 0, 0, destW, destH);
                            TextureBrush tex = new TextureBrush(temp);

                            //fill path with the texture
                            gfx.FillPath(tex, path);

                            //cleanup
                            tex.Dispose();
                            tempGfx.Dispose();
                            temp.Dispose();
                        }

                        if (graphBorder)
                        {
                            //draw border
                            int right = left + graphW;
                            int bottom = graphH;

                            gfx.DrawLine(graphPen, left, y, right, y);
                            gfx.DrawLine(graphPen, right, y, right, y + bottom);
                            gfx.DrawLine(graphPen, left, y + bottom, right, y + bottom);
                            gfx.DrawLine(graphPen, left, y, left, y + bottom);
                        }

                        left += graphW + (graphBorder ? 1 : 0);
                        this.h = graphH + (graphBorder ? 1 : 0);
                        h = Math.Max(h, this.h);
                    }
                    break;
            }

            prevDraw = DateTime.Now;
        }

        float interpolateData(int data, int visMin, int visMax)
        {
            return visMin + (visMax - visMin) * (data - min) / (max - min);
        }

        int zoomLength(int l, int zoom)
        {
            return (int)(l * ((float)zoom / 100));
        }

        int setAlignment(VertAlign align, int drawH, int fixedH)
        {
            switch (align)
            {
                default:
                case VertAlign.Top:
                    return 0;
                case VertAlign.Center:
                    return (fixedH - drawH) / 2;
                case VertAlign.Bottom:
                    return fixedH - drawH;
            }
        }
    }
}
