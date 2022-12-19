using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using _1_Axis;

namespace Led_Cube
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Maak een SerialPort object
        SerialPort mijnPoort;
        // Maak een array aan, met daarin één byte
        byte[] data = new byte[81];

        bool startBoundaries = true; //Maakt variable boundaries array gelijk aan const (zorg dat dit ENKEL in het begin gebeurd)
        bool startRandomLed = false; //Maakt vorig random ledposition gelijk aan 0 (zorgt dat dit altijd gebeurd BEHALVE in het begin)

        int a=0; //=position
        int color=0; // 0=Red, 1=Green; 2=Blue
        string naamKleur = "Red";

        //Boundaries aangeven bij kleur rood
        readonly int[] maxY = { 6, 15, 24, 33, 42, 51, 60, 69, 78 };
        readonly int[] minY = { 0, 9, 18, 27, 36, 45, 54, 63, 72 };
        readonly int[] maxX = { 54, 57, 60, 63, 66, 69, 72, 75, 78 };
        readonly int[] minX = { 0, 3, 6, 9, 12, 15, 18, 21, 24 };
        readonly int[] maxZ = { 18, 21, 24, 45, 48, 51, 72, 75, 78 };
        readonly int[] minZ = { 0, 3, 6, 27, 30, 33, 54, 57, 60 };
        //Boundaries die veranderen bij kleur
        int[] maxYvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; int[] minYvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] maxXvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; int[] minXvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] maxZvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; int[] minZvar = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        //Game
        int randomLed = 81;
        int score = 0;
        int celebration;

        public MainWindow()
        {
            InitializeComponent();

            cbxCOMPort.Items.Add("None");
            foreach (string s in SerialPort.GetPortNames())
                cbxCOMPort.Items.Add(s);

            mijnPoort = new SerialPort();                 //Eigenschappen serialport
            mijnPoort.Parity = Parity.None;
            mijnPoort.DataBits = 8;
            mijnPoort.BaudRate = 250000;
            mijnPoort.StopBits = StopBits.One;

            lblColor.Content = naamKleur;

            txBlkIntro.Text = "Z = Forwards.\n";          //Info controls
            txBlkIntro.Text += "S = Backwards.\n";
            txBlkIntro.Text += "Q = Left.\n";
            txBlkIntro.Text += "D = Right.\n";
            txBlkIntro.Text += "R = Up.\n";
            txBlkIntro.Text += "F = Down.\n";
            txBlkIntro.Text += "C = Change color.\n";
            txBlkIntro.Text += "P = Stop program.";

            txBlkGameInfo.Text = "Er verschijnt ergens Random een ledje in de kleuren RGB,\n";      //Info game
            txBlkGameInfo.Text += "Beweeg en 'pak' het ledje in de juiste kleur,\n";
            txBlkGameInfo.Text += "De score verhoogt.";

            txBlkAnimatieInfo.Text = "De kleur van 'Player'-ledje wordt gebruikt in de animatie.";  //Info animatie

            if (startBoundaries)                            //Variabele boundaries gelijkstellen aan constante boundaries
            {
                for (int i = 0; i < maxX.Length; i++)
                {
                    maxXvar[i] = maxX[i];
                    minXvar[i] = minX[i];
                    maxYvar[i] = maxY[i];
                    minYvar[i] = minY[i];
                    maxZvar[i] = maxZ[i];
                    minZvar[i] = minZ[i];
                }
                startBoundaries = false;
            }
        }
        private void cbxCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mijnPoort != null)
            {
                if (mijnPoort.IsOpen)
                    mijnPoort.Close();
                if (cbxCOMPort.SelectedItem.ToString() != "None")
                {
                    mijnPoort.PortName = cbxCOMPort.SelectedItem.ToString();
                    mijnPoort.Open();
                    data[78] = 255;
                    Run();
                    Thread.Sleep(50);
                    data[78] = 0;
                    data[0] = 255;
                    Run();
                    LabelColor();
                    DebugDefault();
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)  //Toets ingedrukt
        {
            if (mijnPoort.IsOpen)
            {
                if (e.Key == Key.Z)                 //Vooruit
                {
                    CheckYMax();                    //Is 'a' uit de boundaries + position opschuiven 
                }
                else if (e.Key == Key.S)            //Achteruit
                {
                    CheckYMin();
                }
                else if (e.Key == Key.D)            //Rechts
                {
                    CheckXMax();
                }
                else if (e.Key == Key.Q)            //Links
                {
                    CheckXMin();
                }
                else if (e.Key == Key.R)            //Omhoog
                {
                    CheckZMax();
                }
                else if (e.Key == Key.F)            //Beneden
                {
                    CheckZMin();
                }
                else if (e.Key == Key.C)
                {
                    ChangeColor();
                    Run();
                    LabelColor();
                }
                else if (e.Key == Key.P)            //Stop Program
                {
                    mijnPoort.Close();
                    Close();
                }
                Run();
                DebugDefault();
                Game();                             //Checkt voor voorwaarden van de Random Led Game
            }
        }

        private void CheckYMax()                    //Is 'a' uit de boundaries + position opschuiven
        {
            for (int i = 0; i < maxYvar.Length; i++)
            {
                if (a == maxYvar[i])               //Als a uit boundaries is: zet terug naar 1e positie rij
                {
                    data[a] = 0;
                    a = minYvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(YMax): a= {a}");
                    break;
                }
                if(i == maxYvar.Length - 1)        //Als a niet uit boundaries is: verplaats gwn positie
                {
                    data[a] = 0;
                    data[a + 3] = 255;
                    a += 3;
                }
            }
        }
        private void CheckYMin()                    //Idem CheckYMax
        {
            for (int i = 0; i < minYvar.Length; i++)
            {
                if (a == minYvar[i])
                {
                    data[a] = 0;
                    a = maxYvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(YMin): a= {a}");
                    break;
                }
                if (i == minYvar.Length - 1)
                {
                    data[a] = 0;
                    data[a - 3] = 255;
                    a -= 3;
                }
            }
        }
        private void CheckXMax()                    //Idem CheckYMax
        {
            for(int i = 0; i < maxXvar.Length; i++ )
            {
                if(a == maxXvar[i])
                {
                    data[a] = 0;
                    a = minXvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(XMax): a= {a}");
                    break;
                }
                if(i == maxXvar.Length - 1)
                {
                    data[a] = 0;
                    data[a + 27] = 255;
                    a += 27;
                }
            }
        }
        private void CheckXMin()                   //Idem CheckYMax 
        {
            for (int i = 0; i < minXvar.Length; i++)
            {
                if (a == minXvar[i])
                {
                    data[a] = 0;
                    a = maxXvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(XMin): a= {a}");
                    break;
                }
                if (i == minXvar.Length - 1)
                {
                    data[a] = 0;
                    data[a - 27] = 255;
                    a -= 27;
                }
            }
        }
        private void CheckZMax()                    //Idem CheckYMax
        {
            for (int i = 0; i < maxZvar.Length; i++)
            {
                if (a == maxZvar[i])
                {
                    data[a] = 0;
                    a = minZvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(ZMax): a= {a}");
                    break;
                }
                if (i == maxZvar.Length - 1)
                {
                    data[a] = 0;
                    data[a + 9] = 255;
                    a += 9;
                }
            }
        }

        private void CheckZMin()                    //Idem CheckYMax
        {
            for (int i = 0; i < minZvar.Length; i++)
            {
                if (a == minZvar[i])
                {
                    data[a] = 0;
                    a = maxZvar[i];
                    data[a] = 255;
                    Debug.WriteLine($"Led Versprongen(ZMin): a= {a}");
                    break;
                }
                if (i == minZvar.Length - 1)
                {
                    data[a] = 0;
                    data[a - 9] = 255;
                    a -= 9;
                }
            }
        }
        private void ChangeColor()                  //Veranderd kleur van led
        {
            color++;
            data[a] = 0;
            a++;
            if (color > 2)                          //Bij blauw terug naar rood 2e byte --> 0e byte
            {
                color = 0;
                a -= 3;
            }
            Debug.WriteLine($"Kleur veranderd: color= {color}");
            for (int i = 0; i < maxX.Length; i++)   //Boundaries aanpassen per kleur
            {
                maxXvar[i] = maxX[i] + color;
                minXvar[i] = minX[i] + color;
                maxYvar[i] = maxY[i] + color;
                minYvar[i] = minY[i] + color;
                maxZvar[i] = maxZ[i] + color;
                minZvar[i] = minZ[i] + color;
            }
            data[a] = 255;
        }
        private void Run()                          //Schrijf alle 81 bytes naar poort
        {
            if(mijnPoort.IsOpen)
                mijnPoort.Write(data, 0, data.Length);
        }
        private void DebugDefault()                 //Debug nodige info
        {
            WhichColor();
            Debug.WriteLine($"Color= {naamKleur}");
            Debug.WriteLine($"a= {a}\n");
        }
        private void WhichColor()                   //Veranderd color(int) naar Leesbare kleur(string)
        {
            switch(color)
            {
                case 0:
                    naamKleur = "Red";
                    break;
                case 1:
                    naamKleur = "Green";
                    break;
                case 2:
                    naamKleur = "Blue";
                    break;
                default:
                    Debug.WriteLine("stop");
                    break;
            }
        }
        private void LabelColor()                   //Zet label op gebruikte kleur
        {
            WhichColor();
            lblColor.Content = naamKleur;
        }

        private void Random_Led()                   //Led gaat aan op random positie met random kleur
        {
            if ((startRandomLed == true) && (randomLed != 81))                     //RandomLed gaat uit BEHALVE bij de eerste keer
                data[randomLed] = 0;
            startRandomLed = true;
            int[] positionsRedLed = { 0, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57, 60, 63, 66, 69, 72, 75, 78 };
            // Create a Random object  
            Random rand = new Random();
            // Generate a random index less than the size of the array.  
            int index = rand.Next(positionsRedLed.Length);
            int indexKleur = rand.Next(3);
            randomLed = positionsRedLed[index] + indexKleur;
            // Display the result.  
            Debug.WriteLine($"Random gekozen position is {randomLed}.");
            data[randomLed] = 255;
            Run();
        }
        private void Game()                         //Game, check als 2 leds overlappen
        {
            if (a == randomLed)
            {           
                Debug.WriteLine("Game Gelukt\n");
                Random_Led();
                data[randomLed] = 0;
                data[a] = 0;
                Run();
                Celebration();
                data[randomLed] = 255;
                Run();
                Tag_Player();
                data[a] = 255;
                Run();
                score += 1;
                lblScore.Content = score;
            }
        }
        private void Celebration()                  //Chaotisch random led knipper
        {
            for (int i = 0; i < 101; i++)
            {
                data[a] = 0;

                data[celebration] = 0;
                int[] positionsRedLed = { 0, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57, 60, 63, 66, 69, 72, 75, 78 };
                // Create a Random object  
                Random rand = new Random();
                // Generate a random index less than the size of the array.  
                int index = rand.Next(positionsRedLed.Length);
                int index2 = rand.Next(3);
                celebration = positionsRedLed[index] + index2;
                // Display the result.  
                Debug.WriteLine($"Random gekozen position is {celebration}.");
                data[celebration] = 255;
                Run();
                Thread.Sleep(5);
            }
            data[celebration] = 0;
            Run();
        }
        private void Tag_Player()                   //Pinkt spelend ledje paar keer om dat ledje te identificeren
        {
            for (int i = 0; i <= 2; i++)
            {
                data[a] = 255;
                Run();
                Thread.Sleep(100);
                data[a] = 0;
                Run();
                Thread.Sleep(50);
            }
        }
        private void btnGame_Click(object sender, RoutedEventArgs e)
        {
            score = 0;
            lblScore.Content = score;
            Random_Led();
        }

        private void btnAnimatie_Click(object sender, RoutedEventArgs e) 
        {
            if (mijnPoort.IsOpen)
            {
                data[a] = 0;                        //Player led uitschakelen
                if(randomLed != 81)
                    data[randomLed] = 0;            //Random Led van Game uitschakelen
                randomLed = 81;
                Run();
                data[30] = 255;                     //Pilaar center magenta
                data[32] = 255;
                data[39] = 255;
                data[41] = 255;
                data[48] = 255;
                data[50] = 255;
                for (int j = 0; j < 5; j++)         //Animatie 10x doorlopen
                {
                    for (int i = 2; i < 21; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 2; i < 21; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 29; i < 49; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 29; i < 49; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 56; i < 75; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 56; i < 75; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 59; i < 78; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 59; i < 78; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 62; i < 81; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 62; i < 81; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 35; i < 54; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 35; i < 54; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 8; i < 27; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 8; i < 27; i += 9)
                        data[i - 2 + color] = 0;
                    for (int i = 5; i < 24; i += 9)
                        data[i - 2 + color] = 255;
                    Run();
                    Thread.Sleep(100);
                    for (int i = 5; i < 24; i += 9)
                        data[i - 2 + color] = 0;
                }
                for (int i = 2; i < 21; i += 9)
                    data[i - 2 + color] = 255;
                Run();
                Thread.Sleep(100);
                for (int i = 2; i < 21; i += 9)         //Alle leds van animatie uitschakelen & andere leds weer inschakelen
                    data[i - 2 + color] = 0;
                data[30] = 0;
                data[32] = 0;
                data[39] = 0;
                data[41] = 0;
                data[48] = 0;
                data[50] = 0;
                data[a] = 255;
                Run();
                LabelColor();
            }
        }

        private void btnAuthor_Click(object sender, RoutedEventArgs e)      //Auteur weergeven via klasse 'Author'
        {
            Author persoon = new Author();
            persoon.Voornaam = "Remi";
            persoon.Achternaam = "Delobelle";
            lblAuthor.Content = persoon.ToonNaam();
        }
    }
}