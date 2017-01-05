using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Tasks;
using Microsoft.Advertising.Mobile.UI;
using Microsoft.Devices;



namespace ShapeTap
{

    public enum GameState
    {
        Loading = 0, Running, Results, RoundTransition
    }

    public enum MyColors
    {
        Red = 0, Orange, Yellow, Green, Blue, Indigo, Violet, DarkGrey, Gray, White, Black
    }
/*
    public class Sounds
    {
        public static enum Sounds
        {
            GoodHit, BadHit, LevelChange, GameOver
        }

        public void PlaySound(Sounds sound)
        {
            
        }

        public class SoundThread
        {
            private class Data
            {
                public MediaElement MediaElement;
                public String Source;

                public Data(MediaElement me, String src)
                {
                    this.MediaElement = me;
                    this.Source = src;
                }

            }

            private Thread thread;

            public SoundThread(MediaElement me, String src)
            {
                thread = new Thread(SoundThread.Do);
                Data data = new Data(me, src);
                thread.Start(data);

            }
            public static void Do(object oData)
            {
                Data d = (Data)oData;
                Uri uri = new Uri(d.Source);
                d.MediaElement.Dispatcher.BeginInvoke(delegate () { 
                d.MediaElement.SetSource(uri;}
            }
        }
    }
    */
    public class Game
    {
        
        private static LicenseInformation licenseInfo = new LicenseInformation();
        private static bool bLicCache = false;
        private static bool bTrialChecked = false; 
        private const double MaxStatusBar = 420;
        private int iTimeRemaining = 10000;
        private int iBoardShapeCount = 3;
        private int iShapesToGet = 4;
        private int iScore = 0;
        private double dLevelVelocity = .25;
        private int iLevel = 1;
        private double dStatusBarWidth = MaxStatusBar;
        private Grid GridBoard;
        private Grid GridToGet;
        private MainPage mainPage;
        private static int iColorCount = 10;
        private static int iShapeCount = 7;
        

        public Game(Grid gridBoard, Grid gridToGet, MainPage mainPage, int iLevel, int iScore, int iTimeRemaining)
        {
            this.GridBoard = gridBoard;
            this.GridToGet = gridToGet;
            this.mainPage = mainPage;
            this.iLevel = iLevel;
            this.SetUpLevel(iLevel);
            this.iScore = iScore;
            this.iTimeRemaining = iTimeRemaining;
            InitBoard();
        }
        public int Score
        {
            get { return iScore; }
            set { iScore = value; }
        }
        public static bool IsTrial
        {
            get
            {
                
                if (!bTrialChecked) 
                {
                    bTrialChecked = true; //do check only once
                    bLicCache = licenseInfo.IsTrial();
                }
                return bLicCache;
              //  return true;
            }
        }
        public static int ColorCount
        {
            get { return iColorCount; }
            
        }
        public static int ShapeCount
        {
            get { return iShapeCount; }

        }

        public int ShapesToGet
        {
            get { return iShapesToGet; }
            set
            {
                if (value > 10)
                {
                    iShapesToGet = 10;
                }
                else
                {
                    if (value < 0)
                    {
                        iShapesToGet = 0;
                    }
                }
                iShapesToGet = value;
            }
        }
        private void NewHeaderImage()
        {
            //pick random Image to match head
            Random rnd = new Random();
            int iHead = rnd.Next(iBoardShapeCount);
            object obj = this.GridBoard.Children[iHead];

            Canvas cvs = (Canvas)obj;
            Image img = (Image)cvs.Children[0];

            this.SetHeaderShape((SolidColorBrush)cvs.Background, (BitmapImage)img.Source);

        }
        public void KindHit()
        {
            iScore = iScore + iLevel;
            this.NewHeaderImage();
            this.iShapesToGet--;
            SetToGetBlocks();
            if (iShapesToGet == 0)
            {
                //level cleared
                this.iLevel++;
                SetUpLevel(this.iLevel);

            }

        }
        public void FailHit()
        {
            VibrateController vc = VibrateController.Default;
            vc.Start(new TimeSpan(0, 0, 0, 0, 200));
            iScore = iScore - iLevel;
            
        }
        private void SetToGetBlocks()
        {
            int i = 0;
            int iTop = GridToGet.Children.Count - (iShapesToGet + 1);
            //Init 
            foreach (object obj in GridToGet.Children)
            {
                if (obj is Canvas) //always should be true
                {
                    Canvas cvs = (Canvas)obj;
                    if (i > iTop)
                    {
                        cvs.Background = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        cvs.Background = ShapeHelper.GetSolidBrushFromMyColor(MyColors.Black);
                        cvs.IsHitTestVisible = false;
                    }
                }
                i++;
            }
        }
        private void SetHeaderShape(SolidColorBrush scb, BitmapImage bmi)
        {
            //set header image
            mainPage.HeadCanvas0.Background = scb;
            mainPage.ImgHead0.Source = bmi;
        }
        private void UpdateLevel()
        {
            this.mainPage.TextBlockLevel.Text = String.Format("level {0}", this.iLevel);
        }
        public double StatusBarWidth //is decrementing each check a good idea?
        {
            get {
                if (dStatusBarWidth > dLevelVelocity)
                {
                    dStatusBarWidth = dStatusBarWidth - dLevelVelocity;
                }
                else
                {
                    //todo: end game
                    dStatusBarWidth = MaxStatusBar; //REMOVE
                    
                    mainPage.gmState = GameState.Results;


                    // Show High Score message for non-trial version
                    if (Game.IsTrial)
                    {
                        MessageBoxResult result = MessageBox.Show(String.Format(" Score:{0}\n Level:{1}\n\n High Score: Buy full version to track score", iScore, iLevel), "game over", MessageBoxButton.OK);
                    }
                    else
                    {
                        int hs = mainPage.HighScore;
                        if (iScore > hs)
                        {
                            mainPage.HighScore = iScore;

                            MessageBoxResult result = MessageBox.Show(String.Format(" Score:{0}\n Level:{1}\n\n High Score:{0}", iScore, iLevel), "NEW HIGH SCORE!", MessageBoxButton.OK);
                        }
                        else
                        {
                            MessageBoxResult result = MessageBox.Show(String.Format(" Score:{0}\n Level:{1}\n\n High Score:{2}", iScore, iLevel, hs), "game over", MessageBoxButton.OK);
                        }
                    }
                }

                return dStatusBarWidth;
            }
            set { dStatusBarWidth = value; }
        }

        private void SetUpLevel(int iLevel)
        {
            //Diabolical formulas
            Random rnd = new Random();
            MyColors MyColor = (MyColors)rnd.Next(9);
            mainPage.TextBlockLevel.Foreground = ShapeTap.ShapeHelper.GetSolidBrushFromMyColor(MyColor);

            iColorCount = 2 + iLevel ;
            iShapeCount = 2 + iLevel / 3;
            if(iColorCount>8)
            {
                iColorCount = 8;
            }


            if (iShapeCount > 7)
            {
                iShapeCount = 7;
            }

            mainPage.CanvasTimeLeft.Width = MaxStatusBar;
            dStatusBarWidth = MaxStatusBar;
            UpdateLevel();
            this.ShapesToGet = 4 + iLevel / 2;
            SetToGetBlocks();
            double dDec = iLevel * .05;
            if (dDec>.25)
            {
                dDec = .25;
            }
            if (dLevelVelocity < 4)
            {
                dLevelVelocity = dLevelVelocity + (.25 - dDec) ;
               // dLevelVelocity = .1;
            }
            
            switch (iLevel)
            {
                case 4:
                    iBoardShapeCount = 6;
                    break;
                case 8:
                    iBoardShapeCount = 9;
                    break;
                case 12:
                    if(Game.IsTrial)
                    {
                        iBoardShapeCount = 9;
                        MessageBoxResult msg = MessageBox.Show("Buy the full version to remove all advertisements and this pop up message!", "Trial Mode",MessageBoxButton.OK);
                    }
                    else
                    {
                        iBoardShapeCount = 12;
                    }
                        break;
            }
            GridBoardInit();
            NewHeaderImage();
            
            /*
            Uri uri = new Uri("LevelCleared.jpg", UriKind.Relative);
            mainPage.ImgBranding.Source = new BitmapImage(uri);
            mainPage.ImgBranding.Visibility = Visibility.Visible;
            System.Threading.Thread.Sleep(1000);
            mainPage.ImgBranding.Visibility = Visibility.Collapsed;
            uri = new Uri("SplashScreenImage.jpg", UriKind.Relative)
             */
        }
        private void GridBoardInit()
        {
            int i = 0;

            foreach (object obj in GridBoard.Children)
            {
                //Set all images to random
                if (obj is Canvas) //always should be true
                {
                    Canvas cvs = (Canvas)obj;

                    if (i < this.iBoardShapeCount)
                    {
                        Image img = (Image)VisualTreeHelper.GetChild(cvs, 0);
                        ShapeHelper.SetRandomColorShape(img,Game.ColorCount,Game.ShapeCount);
                        cvs.IsHitTestVisible = true;

                    }
                    else
                    {
                        cvs.Background = ShapeHelper.GetSolidBrushFromMyColor(MyColors.Black);
                        cvs.IsHitTestVisible = false;
                    }
                }
                i++;
            }
        }
        public void InitBoard()
        {
            GridBoardInit();
            NewHeaderImage();
            SetToGetBlocks();
            UpdateLevel();

        }

    }

    public static class ShapeHelper
    {
        public static int ShapeCount = 7;
        public static String[] ShapeFileNames = { "Circle.png", "Diamond.png", "Heart.png", "Hexagon.png", "Square.png", "Star.png", "Triangle.png" };

        public static SolidColorBrush GetSolidBrushFromMyColor(MyColors MyColor)
        {
            SolidColorBrush b = new SolidColorBrush();
            Color clr = new Color();
            clr.A = 255; // Opaque alpha 

            switch (MyColor)
            {
                case MyColors.Red:
                    clr = Colors.Red;
                    break;
                case MyColors.Orange:
                    clr = Colors.Orange;
                    break;
                case MyColors.Yellow:
                    clr = Colors.Yellow;
                    break;
                case MyColors.Green:
                    clr = Colors.Green;
                    break;
                case MyColors.Blue:
                    clr = Colors.Blue;
                    break;
                case MyColors.Indigo:
                    //Indigo has to be set by ARGB as there is no static member in Colors
                    clr.A = 255; //Opaque
                    clr.R = 75; //4B
                    clr.G = 0;
                    clr.B = 130;//82
                    break;
                case MyColors.Violet:
                    //Violet has to be set by ARGB as there is no static member in Colors
                    clr.A = 255; //Opaque
                    clr.R = 238; //EE
                    clr.G = 130; //82
                    clr.B = 238; //EE
                    break;
                case MyColors.DarkGrey:
                    clr = Colors.DarkGray;
                    break;
                case MyColors.Gray:
                    clr = Colors.Gray;
                    break;
                case MyColors.White:
                    clr = Colors.White;
                    break;
                case MyColors.Black:
                    clr = Colors.Black;
                    break;

            }

            b.Color = clr;
            return b;
        }

        public static void SetRandomColorShape(Image img, int colorcount, int shapecount)
        {
            //todo
            //int colorcount = 10;
            //int shapecount = 7;
            Random rnd = new Random();
            MyColors MyColor = (MyColors)rnd.Next(colorcount);
            int iRndShape = rnd.Next(shapecount);
            ShapeHelper.SetImageSourceToRelativePathString(img, ShapeFileNames[iRndShape]);
            Canvas c = (Canvas)VisualTreeHelper.GetParent(img);
            c.Background = GetSolidBrushFromMyColor(MyColor);
        }
        public static void SetImageSourceToRelativePathString(Image img, String src)
        {
            Uri uri = new Uri(src, UriKind.Relative);
            BitmapImage bmi = new BitmapImage(uri);
            img.Source = bmi;
        }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        public IsolatedStorageSettings AppSettings = IsolatedStorageSettings.ApplicationSettings;
        
        private int iHighScore = 0;
        public int HighScore
        {
            get 
            {
                if (AppSettings.Contains("highscore"))
                {
                    iHighScore = (int)AppSettings["highscore"];
                }
                else
                {
                    AppSettings.Add("highscore",iHighScore);
                    AppSettings.Save();
                }
                return iHighScore; 
            }
            set
            {
                if (iHighScore < value)
                {
                    iHighScore = value;
                    AppSettings["highscore"] = value;
                    AppSettings.Save();
                }
            }
        }

        

        public GameState gmState = GameState.Loading;

        public AdControl myAdBanner;
        private bool bTouchReady = true;
        private int iLevel = 1;
        private int iScore = 0;
        //private int iTimeRemaining = 10000;
        private long lMainTimeSpan = 100;
        private Game game;
        private DispatcherTimer MainTimer = new DispatcherTimer();


        // Constructor
        public MainPage()
        {
            //Init objects
            InitializeComponent();
            if (Game.IsTrial)
            {
                ImgBranding.Source = new BitmapImage(new Uri("TrialScreenImage.jpg",UriKind.Relative));
            }

        }

        private void SetandSaveAppSettings()
        {
            if (AppSettings.Contains("highscore"))
            {
                HighScore = (int)AppSettings["highscore"];
            }

            try
            {
                
                AppSettings.Clear();
                AppSettings.Add("state", (int)gmState);
                AppSettings.Add("level", iLevel);
                AppSettings.Add("score", iScore);
                AppSettings.Add("time", (int)CanvasTimeLeft.Width);
                AppSettings.Save();
                //AppSettings.Add("highscore", (int)HighScore);
                
            }
            catch (IsolatedStorageException e)
            {
                //todo loop through inner exceptions
                Debug.WriteLine("{0}\n{1}\n{2}\n", e.Message, e.Data, e.StackTrace);
            }
        }

        private void RootPhoneAppPage_Loaded(object sender, RoutedEventArgs e)
        {

            //Load App Settings & Game State 

            
            if (!AppSettings.Contains("state"))
            {
                //No App Settings, assume 1st run & offer help
                if (Game.IsTrial)
                {
                    MessageBoxResult result = MessageBox.Show("It appears this is the first time you have played.  The trial version of ShapeTap uses the Microsoft Ad Control, which requires anonymous location information, and network access. Please press OK to authorize this and continue, or cancel to exit.", "Welcome to ShapeTap!", MessageBoxButton.OKCancel);

                    if (result != MessageBoxResult.OK)
                    {
                        //throw exception
                        NavigationService.GoBack();  
                    }

                }
                SetandSaveAppSettings();
                
            }
            
            //enable touch handling only after page is loaded
            Touch.FrameReported += OnTouchFrameReported;

            //enable Main Loop
            MainTimer.Interval = new TimeSpan (lMainTimeSpan);
            MainTimer.Tick += new EventHandler(MainLoop);

            game = new Game(GridShapes, GridToGet, this, (int)AppSettings["level"], (int)AppSettings["score"], (int)AppSettings["time"]);

            //init Ads if trial
            if (Game.IsTrial )
            {
                AdControl.TestMode = false;

                GridAds.Visibility = Visibility.Visible;
                myAdBanner = new AdControl("b1d70769-3d78-4a60-a0d5-b1be1d9b0f75", "25161", AdModel.Contextual, true);
                //myAdBanner = new AdControl("Test_client", "Image480_80", AdModel.Contextual, true);
                //myAdBanner.
                //GridAds.Background = new SolidColorBrush(Colors.Red);
                GridAds.Children.Add(myAdBanner);



            }

            MainTimer.Start();
        }

        private void OnTouchFrameReported(object sender, TouchFrameEventArgs args)
        {
           

            if (ImgBranding.Visibility != Visibility.Collapsed)
            {
                //ImgBranding Canvas is up - drop it and change state to running
                ImgBranding.Visibility = Visibility.Collapsed;
                System.Threading.Thread.Sleep(100);
                gmState = GameState.Running;
            }
            else
            {
                if (bTouchReady)
                {
                    bTouchReady = false;
                    Point p;
                    TouchPointCollection TPColl = args.GetTouchPoints(RootPhoneAppPage);

                    foreach (TouchPoint tp in TPColl)
                    {
                        if (tp.Action == TouchAction.Up)
                        {
                            p = tp.Position;

                            IEnumerable<UIElement> TouchedElements = VisualTreeHelper.FindElementsInHostCoordinates(p, RootPhoneAppPage);

                            foreach (UIElement uie in TouchedElements)
                            {

                                //Verifys touched object type is Image 
                                if (uie is Image)
                                {
                                    //Set Canvas to random color & Shape
                                    Image img = (Image)uie;

                                    //Compare Image 
                                    BitmapImage bmpTouched = (BitmapImage)img.Source;
                                    BitmapImage bmpHead = (BitmapImage)ImgHead0.Source;
                                    Uri uriTouched = bmpTouched.UriSource;
                                    Uri uriHead = bmpHead.UriSource;
                                   // Debug.WriteLine("{0} == {1}?", uriTouched.ToString(), uriHead.ToString());

                                    Canvas cvsTouched = (Canvas) VisualTreeHelper.GetParent(uie);
                                    SolidColorBrush scbTouched = (SolidColorBrush)cvsTouched.Background;
                                    SolidColorBrush scbHead = (SolidColorBrush)HeadCanvas0.Background;
                                    //Debug.WriteLine("{0} == {1}?", scbTouched.Color.ToString(), scbHead.Color.ToString());
                                    

                                    if ( (uriTouched.ToString() == uriHead.ToString()) && (scbTouched.Color.ToString()==scbHead.Color.ToString()))
                                    {
                                       
                                        ShapeHelper.SetRandomColorShape(img,Game.ColorCount, Game.ShapeCount);
                                        game.KindHit();
                                    }
                                    else
                                    {
                                        game.FailHit();
                                        //ShapeHelper.SetRandomColorShape(img);
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine(uie.GetType());
                                }
                            }
                        }
                    }
                    bTouchReady = true;
                }
            }

        }


        private void ShowInstructions()
        {
        //stub
            WebBrowserTask wbt = new WebBrowserTask();

            wbt.URL = "http://wilcamp.com/";
            wbt.Show();

        }

        private void MainLoop(object sender, EventArgs e)
        {
            //Debug.WriteLine("{0} gmState in MainLoop", gmState.ToString());
            switch (gmState)
            {
                
                case GameState.Loading:
                    
                    break;

                case GameState.Running:
                    TextBlockScore.Text = game.Score.ToString();
                    
                    //todo - Super hack status bar color
                    int width = (int) CanvasTimeLeft.Width;

                    switch (width)
                    {
                        case 420:
                            CanvasTimeLeft.Background = ShapeHelper.GetSolidBrushFromMyColor(MyColors.Green);
                            break;
                        case 260:
                        case 261:
                        case 262:
                        case 263:
                        case 264:
                        case 265:
                            CanvasTimeLeft.Background = ShapeHelper.GetSolidBrushFromMyColor(MyColors.Yellow);
                            break;
                        
                        case 117:
                        case 118:
                        case 119:
                        case 120:
                        case 121:

                            CanvasTimeLeft.Background = ShapeHelper.GetSolidBrushFromMyColor(MyColors.Red);
                            break;
                    }
                    
                    CanvasTimeLeft.Width = game.StatusBarWidth;

                    break;

                case GameState.Results:
                    if (Game.IsTrial)
                    {
                        ImgBranding.Source = new BitmapImage(new Uri("TrialScreenImage.jpg", UriKind.Relative));

                    }
                    else
                    {
                        ImgBranding.Source = new BitmapImage(new Uri("ScreenImage.jpg", UriKind.Relative));
                    }
                    ImgBranding.Visibility = Visibility.Visible;
                    System.Threading.Thread.Sleep(100);
                    game = new Game(GridShapes, GridToGet, this, 1, 0, 0);
                    break;
            }
        }


        private void RootPhoneAppPage_Unloaded(object sender, RoutedEventArgs args)
        {

            SetandSaveAppSettings();
        }





    
    }
}