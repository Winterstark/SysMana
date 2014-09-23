using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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

        public bool Clock24HrFormat, ClockPlaySounds, ClockPlaySoundsOnStartup, ClockMouseover;
        public double ClockLatitude, ClockLongitude;
        public int ClockTimeZone;
        DateTime clockSunrise, clockSunset, clockYesterdaySunset, clockTomorrowSunrise, clockNextUpdate;
        Image clockOrb, clockRotatedOrb, clockFrame, clockDayIcon, clockNightIcon;
        TextureBrush clockDayBrush, clockNightBrush;
        float clockDayAngle;
        int clockYOffset;
        bool clockSpecialDay;
        enum UpdateStatus { FirstUpdate, Day, Night };
        UpdateStatus prevUpdateStatus;

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

            Prefix = "Available memory: ";
            Postfix = "MB";
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
            Clock24HrFormat = true;
            ClockPlaySounds = true;
            ClockPlaySoundsOnStartup = true;
            ClockLatitude = 0;
            ClockLongitude = 0;
            ClockTimeZone = 0;
            clockYOffset = 0;
        }

        public Meter(string lineFromFile, string imgsDir, Func<string, Image> LoadImg, Action<Image> DisposeImg)
        {
            this.imgsDir = imgsDir;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            try
            {
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

                Clock24HrFormat = bool.Parse(parts[i++]);
                ClockPlaySounds = bool.Parse(parts[i++]);
                ClockPlaySoundsOnStartup = bool.Parse(parts[i++]);
                ClockLatitude = double.Parse(parts[i++]);
                ClockLongitude = double.Parse(parts[i++]);
                ClockTimeZone = int.Parse(parts[i++]);

                clockYOffset = 0;
            }
            catch
            {
                //meters file is corrupted or comes from a previous version (in which case SysMana should continue to run fine)
            }

            LoadResources();
        }

        public void LoadResources()
        {
            //cleanup previous resources
            DisposeImg(spinnerImg);
            DisposeImg(backgroundImg);
            DisposeImg(foregroundImg);
            DisposeImg(graphTexImg);
            DisposeImg(clockOrb);
            DisposeImg(clockRotatedOrb);
            DisposeImg(clockFrame);
            DisposeImg(clockDayIcon);
            DisposeImg(clockNightIcon);

            if (imgSeq != null)
                foreach (Image img in imgSeq)
                    DisposeImg(img);
            
            //load images
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
                    if (ImgSeqDir != "" && Directory.Exists(imgsDir + ImgSeqDir))
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
                case "Dota-style clock":
                    clockFrame = LoadImg(imgsDir + "dota_clock\\frame.png");
                    clockDayIcon = LoadImg(imgsDir + "dota_clock\\day.png");
                    clockNightIcon = LoadImg(imgsDir + "dota_clock\\night.png");

                    loadClockOrbs();

                    clockNextUpdate = DateTime.Now;
                    clockSunrise = new DateTime(); //force calcTwilights call
                    break;
            }
            
            //drawing resources
            graphPen = new Pen(GraphLineColor, GraphLineW);
        }

        void loadClockOrbs()
        {
            if (!clockSpecialDay)
            {
                clockDayBrush = new TextureBrush(LoadImg(imgsDir + "dota_clock\\day orb.png"));
                clockNightBrush = new TextureBrush(LoadImg(imgsDir + "dota_clock\\night orb.png"));
            }
            else
            {
                clockDayBrush = new TextureBrush(LoadImg(imgsDir + "dota_clock\\special day orb.png"));
                clockNightBrush = new TextureBrush(LoadImg(imgsDir + "dota_clock\\special night orb.png"));
            }
        }

        public string FormatForFile()
        {
            return
                Data + "--" + DataSubsource + "--" + Vis + "--" + LeftMargin + "--" + TopMargin + "--" + Min + "--" + Max + "--" + Zoom + "--" + ClickAction + "--" + DragFileAction + "--" + MouseWheelAction + "--" +
                Prefix + "--" + Postfix + "--" + OnlyValue + "--" +
                Spinner + "--" + MinSpin + "--" + MaxSpin + "--" +
                Background + "--" + Foreground + "--" + Vector + "--" +
                ImgSeqDir + "--" +
                GraphW + "--" + GraphH + "--" + GraphStepW + "--" + GraphLineW + "--" + GraphInterval + "--" + GraphLineColor.R + "--" + GraphLineColor.G + "--" + GraphLineColor.B + "--" + GraphBorder + "--" + GraphTex + "--" + GraphTexFront + "--" +
                Clock24HrFormat + "--" + ClockPlaySounds + "--" + ClockPlaySoundsOnStartup + "--" + ClockLatitude + "--" + ClockLongitude + "--" + ClockTimeZone;
        }

        public void Draw(Graphics gfx, Font font, Brush textBrush, int fixedH, VertAlign align, ref int left, ref int h)
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

                    gfx.DrawString(output, font, textBrush, left, y);

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
                case "Dota-style clock":
                    if (!(clockDayBrush == null || clockNightBrush == null || clockFrame == null || clockDayIcon == null || clockNightIcon == null))
                    {
                        //calculate clock rotation
                        if (DateTime.Now > clockNextUpdate)
                        {
                            DisposeImg(clockRotatedOrb); //cleanup previous orb

                            //recalculate sunrise/sunset times if new day
                            if (clockSunrise.Date != DateTime.Now.Date)
                            {
                                calcTwilights();

                                //generate orb with new day/night ratio
                                clockDayAngle = 360.0f * (float)(clockSunset - clockSunrise).TotalSeconds / (60 * 60 * 24);
                                int orbSize = clockDayBrush.Image.Width;

                                DisposeImg(clockOrb);
                                clockOrb = new Bitmap(orbSize, orbSize);

                                Graphics tempOrbGfx = Graphics.FromImage(clockOrb);
                                tempOrbGfx.FillPie(clockDayBrush, 0, 0, orbSize, orbSize, -90, clockDayAngle);
                                tempOrbGfx.FillPie(clockNightBrush, 0, 0, orbSize, orbSize, -90 + clockDayAngle, 360 - clockDayAngle);
                                tempOrbGfx.Dispose();
                            }

                            //rotate orb
                            float rotAngle;
                            if (DateTime.Now < clockSunrise)
                                //before sunrise
                                rotAngle = (360.0f - clockDayAngle) * (clockSunrise - DateTime.Now).Ticks / (clockSunrise - clockYesterdaySunset).Ticks;
                            else if (DateTime.Now < clockSunset)
                                //daytime
                                rotAngle = -clockDayAngle * (DateTime.Now - clockSunrise).Ticks / (clockSunset - clockSunrise).Ticks;
                            else
                                //after sunset
                                rotAngle = -clockDayAngle - (360.0f - clockDayAngle) * (DateTime.Now - clockSunset).Ticks / (clockTomorrowSunrise - clockSunset).Ticks;

                            clockRotatedOrb = new Bitmap(clockOrb.Width, clockOrb.Height);

                            Graphics tempGfx = Graphics.FromImage(clockRotatedOrb);
                            tempGfx.TranslateTransform(clockOrb.Width / 2, clockOrb.Height / 2);
                            tempGfx.RotateTransform(rotAngle);
                            tempGfx.TranslateTransform(-clockOrb.Width / 2, -clockOrb.Height / 2);
                            tempGfx.DrawImage(clockOrb, 0, 0, clockOrb.Width, clockOrb.Height);
                            tempGfx.Dispose();

                            //sound notifications
                            bool daytime = clockSunrise < DateTime.Now && DateTime.Now < clockSunset;

                            if (ClockPlaySounds)
                                switch (prevUpdateStatus)
                                {
                                    case UpdateStatus.FirstUpdate:
                                        if (ClockPlaySoundsOnStartup && clockSunrise < DateTime.Now && DateTime.Now.Hour < 12)
                                            Misc.PlaySound(imgsDir + "dota_clock\\morning.wav");

                                        if (daytime)
                                            prevUpdateStatus = UpdateStatus.Day;
                                        else
                                            prevUpdateStatus = UpdateStatus.Night;
                                        break;
                                    case UpdateStatus.Day:
                                        if (!daytime)
                                        {
                                            Misc.PlaySound(imgsDir + "dota_clock\\night.wav");
                                            prevUpdateStatus = UpdateStatus.Night;
                                        }
                                        break;
                                    case UpdateStatus.Night:
                                        if (daytime)
                                        {
                                            Misc.PlaySound(imgsDir + "dota_clock\\morning.wav");
                                            prevUpdateStatus = UpdateStatus.Day;
                                        }
                                        break;
                                }

                            //set time for next update
                            clockNextUpdate = DateTime.Now.AddMinutes(5);

                            if (daytime)
                            {
                                if (clockSunset < clockNextUpdate)
                                    clockNextUpdate = clockSunset;
                            }
                            else
                            {
                                if (clockSunrise < clockNextUpdate)
                                    clockNextUpdate = clockSunrise;
                            }
                        }

                        //set clock height if window is set to autosize
                        if (fixedH == 0)
                            fixedH = zoomLength(112, Zoom);

                        //calc sizes of clock elements
                        int midX = left + zoomLength(clockFrame.Width / 2, Zoom);
                        int frameW = zoomLength(clockFrame.Width, Zoom);
                        int frameH = zoomLength(clockFrame.Height, Zoom);

                        y = fixedH - frameH; //ignore TopMargin and set y-coordinate such that the clock is drawn at the bottom of the window

                        int orbH = zoomLength(clockOrb.Height, Zoom);
                        int orbY = y + zoomLength(29, Zoom) + clockYOffset;
                        int iconW = zoomLength(clockDayIcon.Width, (int)(Zoom * 1.33));
                        int iconH = zoomLength(clockDayIcon.Height, (int)(Zoom * 1.33));

                        //clock animation?
                        if (ClockMouseover)
                        {
                            //hide frame and show orb in full
                            int orbMinY = Math.Max(y + frameH - orbH, 0);

                            if (orbY > orbMinY)
                            {
                                orbY -= 2;
                                clockYOffset -= 2;

                                if (orbY < orbMinY)
                                {
                                    clockYOffset += orbMinY - orbY;
                                    orbY = orbMinY;
                                }
                            }
                        }
                        else if (clockYOffset < 0)
                        {
                            //reverse animation until beginning
                            clockYOffset += 2;

                            if (clockYOffset > 0)
                                clockYOffset = 0;
                        }

                        //draw clock
                        gfx.DrawImage(clockRotatedOrb, midX - zoomLength(clockOrb.Width, Zoom) / 2 + 1, orbY, zoomLength(clockOrb.Width, Zoom), orbH);

                        if (clockYOffset == 0)
                        {
                            gfx.DrawImage(clockFrame, left, y, frameW, frameH);

                            if (clockSunrise < DateTime.Now && DateTime.Now < clockSunset)
                                gfx.DrawImage(clockDayIcon, midX - iconW / 2 + 1, y + zoomLength(2, Zoom), iconW, iconH);
                            else
                                gfx.DrawImage(clockNightIcon, midX - iconW / 2 + 1, y + zoomLength(2, Zoom), iconW, iconH);

                            string time;
                            if (Clock24HrFormat)
                                time = DateTime.Now.ToString("H:mm");
                            else
                                time = DateTime.Now.ToString("h:mm");

                            var textSize = gfx.MeasureString(time, font);
                            gfx.DrawString(time, font, textBrush, midX - textSize.Width / 2, y + frameH - textSize.Height);
                        }

                        left += frameW;
                        this.H = fixedH;
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
            if (fixedH == 0)
                return 0;

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

        #region Astronomic calculations
        void calcTwilights()
        {
            DateTime yesterdaySunrise, tomorrowSunset;
            calcTwilightsForDate(DateTime.Now.AddDays(-1), out yesterdaySunrise, out clockYesterdaySunset); //calc yesterday's sunset
            calcTwilightsForDate(DateTime.Now, out clockSunrise, out clockSunset); //calc today's sunrise & sunset
            calcTwilightsForDate(DateTime.Now.AddDays(1), out clockTomorrowSunrise, out tomorrowSunset); //calc tomorrow's sunrise

            //check if today is equinox or solstice
            clockSpecialDay = false;
            clockSpecialDay |= DateTime.Now.Date == getSeasonStartDate(DateTime.Now.Year, 0).Date; //spring equinox
            clockSpecialDay |= DateTime.Now.Date == getSeasonStartDate(DateTime.Now.Year, 1).Date; //summer solstice
            clockSpecialDay |= DateTime.Now.Date == getSeasonStartDate(DateTime.Now.Year, 2).Date; //autumn equinox
            clockSpecialDay |= DateTime.Now.Date == getSeasonStartDate(DateTime.Now.Year, 3).Date; //winter solstice
            
            loadClockOrbs();
        }

        void calcTwilightsForDate(DateTime date, out DateTime sunrise, out DateTime sunset)
        {
            if (ClockLatitude == 0 && ClockLongitude == 0)
            {
                sunrise = new DateTime(date.Year, date.Month, date.Day, 6, 0, 0);
                sunset = new DateTime(date.Year, date.Month, date.Day, 18, 0, 0);
                return;
            }

            //calc julian day
            int a = (14 - date.Month) / 12;
            int y = date.Year + 4800 - a;
            int m = date.Month + 12 * a - 3;
            int JD = date.Day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;

            //calc julian cycle
            double lw = -ClockLongitude;
            double nAsterisk = JD - 2451545.0009 - lw / 360;
            double n = Math.Round(nAsterisk);

            //calc approximate solar noon
            double JAsterisk = 2451545.0009 + lw / 360 + n;

            //calc solar mean anomaly
            double M = (357.5291 + 0.98560028 * (JAsterisk - 2451545)) % 360;

            //calc equation of center
            double C = 1.9148 * sin(M) + 0.02 * sin(2 * M) + 0.0003 * sin(3 * M);

            //calc ecliptic longitude
            double λ = (M + 102.9372 + C + 180) % 360;

            //calc solar transit
            double JTransit = JAsterisk + 0.0053 * sin(M) - 0.0069 * sin(2 * λ);

            //calc sun declination
            double δ = Math.Asin(sin(λ) * sin(23.45)) * 180.0 / Math.PI;

            //calc hour angle
            double Φ = ClockLatitude;
            double ω0 = Math.Acos((sin(-0.83) - sin(Φ) * sin(δ)) / (cos(Φ) * cos(δ))) * 180.0 / Math.PI;

            //calc sunrise & sunset
            double JSet = 2451545.0009 + (ω0 + lw) / 360 + n + 0.0053 * sin(M) - 0.0069 * sin(2 * λ);
            double JRise = JTransit - (JSet - JTransit);

            //convert to time of day
            double JRiseDiff = 1.0 - JRise % 1.0f;
            double JSetDiff = JSet % 1.0f;

            sunrise = date.Date.AddHours(12).Subtract(new TimeSpan(0, 0, (int)(JRiseDiff * 86400)));
            sunset = date.Date.AddHours(12).AddSeconds(JSetDiff * 86400);

            sunrise = sunrise.AddHours(utcTZone());
            sunset = sunset.AddHours(utcTZone());
        }

        int utcTZone()
        {
            if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
                return ClockTimeZone + 1;
            else
                return ClockTimeZone;
        }

        double sin(double degs)
        {
            return Math.Sin(degs * Math.PI / 180.0);
        }

        double cos(double degs)
        {
            return Math.Cos(degs * Math.PI / 180.0);
        }

        DateTime getSeasonStartDate(int seasonYear, int seasonInd)
        {
            double mCalc = ((double)seasonYear - 2000) / 1000;
            double val = 0;

            switch (seasonInd)
            {
                case 0:
                    val = 2451623.80984 + 365242.37404 * mCalc + 0.05169 * mCalc * mCalc - 0.00411 * mCalc * mCalc * mCalc - 0.00057 * mCalc * mCalc * mCalc * mCalc;
                    break;
                case 1:
                    val = 2451716.56767 + 365241.62603 * mCalc + 0.00325 * mCalc * mCalc + 0.00888 * mCalc * mCalc * mCalc - 0.00030 * mCalc * mCalc * mCalc * mCalc;
                    break;
                case 2:
                    val = 2451810.21715 + 365242.01767 * mCalc - 0.11575 * mCalc * mCalc + 0.00337 * mCalc * mCalc * mCalc + 0.00078 * mCalc * mCalc * mCalc * mCalc;
                    break;
                case 3:
                    val = 2451900.05952 + 365242.74049 * mCalc - 0.06223 * mCalc * mCalc - 0.00823 * mCalc * mCalc * mCalc + 0.00032 * mCalc * mCalc * mCalc * mCalc;
                    break;
            }

            double ut;
            int jdn;
            int year, month, day;
            int hour, minute;
            bool julian;
            long x, z, m, d, y;
            long daysPer400Years = 146097L;
            long fudgedDaysPer4000Years = 1460970L + 31;

            val += 0.5;

            jdn = (int)Math.Floor(val);
            ut = val - jdn;
            julian = (jdn <= 2361221);
            x = jdn + 68569L;

            if (julian)
            {
                x += 38;
                daysPer400Years = 146100L;
                fudgedDaysPer4000Years = 1461000L + 1;
            }

            z = 4 * x / daysPer400Years;
            x = x - (daysPer400Years * z + 3) / 4;
            y = 4000 * (x + 1) / fudgedDaysPer4000Years;
            x = x - 1461 * y / 4 + 31;
            m = 80 * x / 2447;
            d = x - 2447 * m / 80;
            x = m / 11;
            m = m + 2 - 12 * x;
            y = 100 * (z - 49) + y + x;
            year = (int)y;
            month = (int)m;
            day = (int)d;

            if (year <= 0)
                year--;

            hour = (int)(ut * 24);
            minute = (int)((ut * 24 - hour) * 60);

            return new DateTime(year, month, day, hour, minute, 0);
        }
        #endregion
    }
}
