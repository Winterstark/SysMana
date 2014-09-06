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
        public string Data, DataSubsource, Vis;
        public int LeftMargin, TopMargin, Min, Max, Zoom, CurrDataValue;
        public string ClickAction, DragFileAction, MouseWheelAction;

        DateTime prevDraw;
        string imgsDir;

        public string Prefix, Postfix;
        public bool OnlyValue;
        
        public string Spinner;
        Image spinnerImg;
        public int MinSpin, MaxSpin;
        double spin;

        public string Background, Foreground, Vector;
        Image backgroundImg, foregroundImg;

        public string ImgSeqDir;
        Image[] imgSeq;

        public int GraphW, GraphH, GraphStepW, GraphLineW, GraphInterval;
        public Color GraphLineColor;
        public bool GraphBorder, GraphTexFront;
        public string GraphTex;
        List<float> graphValues;
        Pen graphPen;
        Image graphTexImg;
        DateTime prevGraphTick;

        public int Left, H;
        Func<string, Image> LoadImg;
        Action<Image> DisposeImg;


        public Meter(string data, string vis, string imgsDir, Func<string, Image> LoadImg, Action<Image> DisposeImg)
        {
            this.Data = data;
            this.Vis = vis;
            this.imgsDir = imgsDir;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            Prefix = "CPU usage: ";
            Postfix = "%";
            LeftMargin = 0;
            TopMargin = 0;
            Min = 0;
            Max = 100;
            Zoom = 100;
            Vector = "Left to right";
            ImgSeqDir = "";
            GraphStepW = 1;
            GraphLineW = 1;
            GraphInterval = 25;
            GraphLineColor = Color.Black;
        }

        public Meter(string lineFromFile, string imgsDir, Func<string, Image> LoadImg, Action<Image> DisposeImg)
        {
            this.imgsDir = imgsDir;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            string[] parts = lineFromFile.Split(new string[] { "--" }, StringSplitOptions.None);
            int i = 0;

            Data = parts[i++];
            DataSubsource = parts[i++];
            Vis = parts[i++];
            LeftMargin = int.Parse(parts[i++]);
            TopMargin = int.Parse(parts[i++]);
            Min = int.Parse(parts[i++]);
            Max = int.Parse(parts[i++]);
            Zoom = int.Parse(parts[i++]);
            ClickAction = parts[i++];
            DragFileAction = parts[i++];
            MouseWheelAction = parts[i++];

            Prefix = parts[i++];
            Postfix = parts[i++];
            OnlyValue = bool.Parse(parts[i++]);

            Spinner = parts[i++];
            MinSpin = int.Parse(parts[i++]);
            MaxSpin = int.Parse(parts[i++]);

            Background = parts[i++];
            Foreground = parts[i++];
            Vector = parts[i++];

            ImgSeqDir = parts[i++];

            GraphW = int.Parse(parts[i++]);
            GraphH = int.Parse(parts[i++]);
            GraphStepW = int.Parse(parts[i++]);
            GraphLineW = int.Parse(parts[i++]);
            GraphInterval = int.Parse(parts[i++]);
            GraphLineColor = Color.FromArgb(int.Parse(parts[i++]), int.Parse(parts[i++]), int.Parse(parts[i++]));
            GraphBorder = bool.Parse(parts[i++]);
            GraphTex = parts[i++];
            GraphTexFront = bool.Parse(parts[i++]);

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
            
            switch (Vis)
            {
                case "Spinner":
                    spinnerImg = LoadImg(imgsDir + Spinner);
                    break;
                case "Progress bar":
                    backgroundImg = LoadImg(imgsDir + Background);
                    foregroundImg = LoadImg(imgsDir + Foreground);
                    break;
                case "Image sequence":
                    if (ImgSeqDir != "" && Directory.Exists(imgsDir + "\\" + ImgSeqDir))
                    {
                        string[] files = Misc.GetFilesInNaturalOrder(imgsDir + ImgSeqDir);
                        imgSeq = new Image[files.Length];
                        
                        for (int i = 0; i < files.Length; i++)
                            imgSeq[i] = LoadImg(imgsDir + ImgSeqDir + "\\" + Path.GetFileName(files[i]));
                    }
                    break;
                case "Graph":
                    graphTexImg = LoadImg(imgsDir + GraphTex);
                    break;
            }
            
            //drawing resources
            graphPen = new Pen(GraphLineColor, GraphLineW);
        }

        public string FormatForFile()
        {
            return
                Data + "--" + DataSubsource + "--" + Vis + "--" + LeftMargin + "--" + TopMargin + "--" + Min + "--" + Max + "--" + Zoom + "--" + ClickAction + "--" + DragFileAction + "--" + MouseWheelAction + "--" +
                Prefix + "--" + Postfix + "--" + OnlyValue + "--" +
                Spinner + "--" + MinSpin + "--" + MaxSpin + "--" +
                Background + "--" + Foreground + "--" + Vector + "--" +
                ImgSeqDir + "--" +
                GraphW + "--" + GraphH + "--" + GraphStepW + "--" + GraphLineW + "--" + GraphInterval + "--" + GraphLineColor.R + "--" + GraphLineColor.G + "--" + GraphLineColor.B + "--" + GraphBorder + "--" + GraphTex + "--" + GraphTexFront;
        }

        public void Draw(Graphics gfx, Font font, int fixedH, VertAlign align, ref int left, ref int h)
        {
            left += LeftMargin;
            this.Left = left;
            int y = TopMargin;

            if (fixedH == 0)
                fixedH = h;

            switch (Vis)
            {
                case "Text":
                    string output;
                    if (OnlyValue)
                        output = CurrDataValue.ToString();
                    else
                        output = Prefix + CurrDataValue.ToString() + Postfix;

                    SizeF txtSize = gfx.MeasureString(output, font);
                    y += setAlignment(align, (int)txtSize.Height, fixedH);

                    gfx.DrawString(output, font, Brushes.Black, left, y);

                    left += (int)txtSize.Width;
                    this.H = (int)txtSize.Height;
                    h = Math.Max(h, this.H);
                    break;
                case "Spinner":
                    if (!(spinnerImg == null || prevDraw.Ticks == 0 || Min == Max))
                    {
                        float speed = interpolateData(CurrDataValue, MinSpin, MaxSpin);
                        double s = DateTime.Now.Subtract(prevDraw).TotalSeconds; //seconds elapsed since last draw
                        int destW = zoomLength(spinnerImg.Width, Zoom);
                        int destH = zoomLength(spinnerImg.Height, Zoom);

                        //left += spinMargin - destW / 2;

                        spin += speed * s;

                        y += setAlignment(align, destH, fixedH);

                        gfx.TranslateTransform(left + destW / 2, y + destH / 2);
                        gfx.RotateTransform((float)spin);
                        gfx.TranslateTransform(-destW / 2, -destH / 2);

                        gfx.DrawImage(spinnerImg, 0, 0, destW, destH);
                        gfx.ResetTransform();

                        left += destW;
                        this.H = destH;
                        h = Math.Max(h, this.H);
                    }
                    break;
                case "Progress bar":
                    if (!(foregroundImg == null || Min == Max))
                    {
                        if (backgroundImg != null)
                            gfx.DrawImage(backgroundImg, left, y + setAlignment(align, zoomLength(backgroundImg.Height, Zoom), fixedH), zoomLength(backgroundImg.Width, Zoom), zoomLength(backgroundImg.Height, Zoom));

                        y += setAlignment(align, zoomLength(foregroundImg.Height, Zoom), fixedH);
                        int progress;

                        switch (Vector)
                        {
                            case "Left to right":
                                progress = (int)interpolateData(CurrDataValue, 0, foregroundImg.Width);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y, zoomLength(progress, Zoom), zoomLength(foregroundImg.Height, Zoom)), new Rectangle(0, 0, progress, foregroundImg.Height), GraphicsUnit.Pixel);
                                break;
                            case "Right to left":
                                progress = (int)interpolateData(CurrDataValue, 0, foregroundImg.Width);
                                gfx.DrawImage(foregroundImg, new Rectangle(left + zoomLength(foregroundImg.Width - progress, Zoom), y, zoomLength(progress, Zoom), zoomLength(foregroundImg.Height, Zoom)), new Rectangle(foregroundImg.Width - progress, 0, progress, foregroundImg.Height), GraphicsUnit.Pixel);
                                break;
                            case "Bottom to top":
                                progress = (int)interpolateData(CurrDataValue, 0, foregroundImg.Height);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y + zoomLength(foregroundImg.Height - progress, Zoom), zoomLength(foregroundImg.Width, Zoom), zoomLength(progress, Zoom)), new Rectangle(0, foregroundImg.Height - progress, foregroundImg.Width, progress), GraphicsUnit.Pixel);
                                break;
                            case "Top to bottom":
                                progress = (int)interpolateData(CurrDataValue, 0, foregroundImg.Height);
                                gfx.DrawImage(foregroundImg, new Rectangle(left, y, zoomLength(foregroundImg.Width, Zoom), zoomLength(progress, Zoom)), new Rectangle(0, 0, foregroundImg.Width, progress), GraphicsUnit.Pixel);
                                break;
                            case "Radial":
                                TextureBrush foreBrush = new TextureBrush(foregroundImg);
                                progress = (int)interpolateData(CurrDataValue, 0, Math.Min(foregroundImg.Width, foregroundImg.Height));

                                gfx.TranslateTransform(left, y);
                                gfx.ScaleTransform((float)Zoom / 100, (float)Zoom / 100);

                                gfx.FillEllipse(foreBrush, (foregroundImg.Width - progress) / 2, (foregroundImg.Height - progress) / 2, progress, progress);
                                
                                gfx.ResetTransform();
                                foreBrush.Dispose();
                                break;
                        }
                        
                        if (backgroundImg != null)
                        {
                            left += Math.Max(zoomLength(backgroundImg.Width, Zoom), zoomLength(foregroundImg.Width, Zoom));
                            this.H = Math.Max(zoomLength(backgroundImg.Height, Zoom), zoomLength(foregroundImg.Height, Zoom));
                        }
                        else
                        {
                            left += zoomLength(foregroundImg.Width, Zoom);
                            this.H = zoomLength(foregroundImg.Height, Zoom);
                        }

                        h = Math.Max(h, this.H);
                    }
                    break;
                case "Image sequence":
                    if (!(imgSeq == null || imgSeq.Length == 0 || Min == Max))
                    {
                        int imgInd = (int)interpolateData(CurrDataValue, 0, imgSeq.Length);
                        imgInd = Math.Max(Math.Min(imgInd, imgSeq.Length - 1), 0);

                        int destW = zoomLength(imgSeq[imgInd].Width, Zoom);
                        int destH = zoomLength(imgSeq[imgInd].Height, Zoom);

                        y += setAlignment(align, destH, fixedH);

                        gfx.DrawImage(imgSeq[imgInd], left, y, destW, destH);

                        left += destW;
                        this.H = destH;
                        h = Math.Max(h, this.H);
                    }
                    break;
                case "Graph":
                    if (!(GraphW == 0 || GraphH == 0 || Min == Max))
                    {
                        //init
                        if (graphValues == null)
                            graphValues = new List<float>();
                        if (graphPen == null)
                            graphPen = new Pen(GraphLineColor, GraphLineW);

                        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if (DateTime.Now.Subtract(prevGraphTick).TotalMilliseconds >= GraphInterval)
                        {
                            //get new value
                            graphValues.Add(interpolateData(CurrDataValue, GraphH, 1));
                            if (graphValues.Count > Math.Ceiling((float)GraphW / GraphStepW) + 1)
                                graphValues.RemoveAt(0); //discard last value when graph too large

                            prevGraphTick = DateTime.Now;
                        }

                        //build graph path
                        GraphicsPath path = new GraphicsPath();
                        float x = left + GraphW;
                        y += setAlignment(align, GraphH, fixedH) + GraphLineW / 2;

                        for (int i = graphValues.Count - 1; i >= 1; i--)
                        {
                            path.AddLine(x, y + graphValues[i], x - GraphStepW, y + graphValues[i - 1]);
                            x -= GraphStepW;
                        }

                        //draw graph
                        gfx.DrawPath(graphPen, path);

                        //fill texture
                        if (graphTexImg != null)
                        {
                            //finish path
                            if (GraphTexFront)
                            {
                                path.AddLine(x, y + graphValues[0], x, y + GraphH);
                                path.AddLine(x, y + GraphH, left + GraphW, y + GraphH);
                                path.AddLine(left + GraphW, y + GraphH, left + GraphW, y + graphValues[graphValues.Count - 1]);
                            }
                            else
                            {
                                path.AddLine(x, y + graphValues[0], x, y);
                                path.AddLine(x, y, left + GraphW, y);
                                path.AddLine(left + GraphW, y, left + GraphW, y + graphValues[graphValues.Count - 1]);
                            }

                            //draw texture to img (because the texture can be an animated gif, and this will copy its current state)
                            int destW = (int)((float)Zoom / 100 * graphTexImg.Width);
                            int destH = (int)((float)Zoom / 100 * graphTexImg.Height);

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

                        if (GraphBorder)
                        {
                            //draw border
                            int right = left + GraphW;
                            int bottom = GraphH;

                            gfx.DrawLine(graphPen, left, y, right, y);
                            gfx.DrawLine(graphPen, right, y, right, y + bottom);
                            gfx.DrawLine(graphPen, left, y + bottom, right, y + bottom);
                            gfx.DrawLine(graphPen, left, y, left, y + bottom);
                        }

                        left += GraphW + (GraphBorder ? 1 : 0);
                        this.H = GraphH + (GraphBorder ? 1 : 0);
                        h = Math.Max(h, this.H);
                    }
                    break;
            }

            prevDraw = DateTime.Now;
        }

        float interpolateData(int data, int visMin, int visMax)
        {
            return visMin + (visMax - visMin) * (data - Min) / (Max - Min);
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
