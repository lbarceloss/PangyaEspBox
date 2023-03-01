using Memory;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace PangyaEspBox
{
    public partial class PangyaESPBox : Form
    {
        string WINDOW_NAME;

        Overlay overlay = new Overlay();

        Mem m = new Mem();
        MemoryHandler m_handler = new MemoryHandler("ProjectG");


        BackgroundWorker BGW = new BackgroundWorker();

        const string PANGYA_VIEW_MATRIX_BASE_ADDRESS = "0x00F02494"; // view matrix 0x00F02494 || 0x00F02484 
        const string PANGYA_BALL_BASE_ADDRESS = "0x00E47E30"; // ball 0x00E47E30

        Ball ball = new Ball();
        Point pointInWorld;

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public PangyaESPBox()
        {
            InitializeComponent();
        }

        void PangyaESPBox_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            Process p = Process.GetProcessesByName("ProjectG").FirstOrDefault();
            if (p != null)
            {
                IntPtr hWnd = p.MainWindowHandle;

                const int nChars = 256;
                StringBuilder Buff = new StringBuilder(nChars);

                if (GetWindowText(hWnd, Buff, nChars) > 0)
                {

                    WINDOW_NAME = Buff.ToString();
                    overlay.SetInvi(this);
                    overlay.StartLoop(10, WINDOW_NAME, this);

                    Thread thread = new Thread(ESP) { IsBackground = true };
                    thread.Start();
                }
                else
                {
                    MessageBox.Show("Unable to get Pangya window name. Restart the program, or check if your operating system is blocking the game process from reading.");
                }
            }
            else
            {
                MessageBox.Show("Pangya is not currently running. The application will be closed, open again when the game is running.");
                Application.Exit();
            }

        }

        ViewMatrix GetViewMatrix(string address = PANGYA_VIEW_MATRIX_BASE_ADDRESS)
        {
            ViewMatrix matrix = new ViewMatrix();

            byte[] buffer = new byte[16 * 4];
            var bytes = m_handler.ReadBytes(Convert.ToUInt32(address, 16), buffer.Length);

            matrix.m11 = BitConverter.ToSingle(bytes, (0 * 4));
            matrix.m12 = BitConverter.ToSingle(bytes, (1 * 4));
            matrix.m13 = BitConverter.ToSingle(bytes, (2 * 4));
            matrix.m14 = BitConverter.ToSingle(bytes, (3 * 4));

            matrix.m21 = BitConverter.ToSingle(bytes, (4 * 4));
            matrix.m22 = BitConverter.ToSingle(bytes, (5 * 4));
            matrix.m23 = BitConverter.ToSingle(bytes, (6 * 4));
            matrix.m24 = BitConverter.ToSingle(bytes, (7 * 4));

            matrix.m31 = BitConverter.ToSingle(bytes, (8 * 4));
            matrix.m32 = BitConverter.ToSingle(bytes, (9 * 4));
            matrix.m33 = BitConverter.ToSingle(bytes, (10 * 4));
            matrix.m34 = BitConverter.ToSingle(bytes, (11 * 4));
 
            matrix.m41 = BitConverter.ToSingle(bytes, (12 * 4));
            matrix.m42 = BitConverter.ToSingle(bytes, (13 * 4));
            matrix.m43 = BitConverter.ToSingle(bytes, (14 * 4));
            matrix.m44 = BitConverter.ToSingle(bytes, (15 * 4));
            

            return matrix;
        }

        Point WorldToScreen(ViewMatrix mtx, Vector3 position, int width, int height)
        {
            Point twoDPoint = new Point();

            float screenposx = (mtx.m11 * position.x) + (mtx.m12 * position.y) + (mtx.m13 * position.z) + mtx.m14;
            float screenposy = (mtx.m21 * position.x) + (mtx.m22 * position.y) + (mtx.m23 * position.z) + mtx.m24;
            float screenposz = (mtx.m31 * position.x) + (mtx.m32 * position.y) + (mtx.m33 * position.z) + mtx.m34;

            if (screenposx > 0.001)
            {
                screenposz = 1.0f / screenposz;
                screenposx *= screenposz;
                screenposy *= screenposz;

                float _x = width / 2;
                float _y = height / 2;

                screenposx += _x + ((float)0.5f * screenposx * width + 0.5f);
                screenposy = _y - ((float)0.5f * screenposy * height + 0.5f);

                twoDPoint.X = (int)screenposx;
                twoDPoint.Y = (int)screenposy;
            }
            else
            {
                return new Point(-99, -99);
            }
            return twoDPoint;

            /*
            float w = (mtx.m41 * position.x) + (mtx.m42 * position.y) + (mtx.m43 * position.z) + mtx.m44;

            if (w > 0.001)
            {
                float screenX = (mtx.m11 * position.x) + (mtx.m12 * position.y) + (mtx.m13 * position.z) + mtx.m14;
                float screenY = (mtx.m21 * position.x) + (mtx.m22 * position.y) + (mtx.m23 * position.z) + mtx.m24;

                float camX = width / 2f;
                float camY = height / 2f;

                float X = camX + (camX * screenX / w);
                float Y = camY - (camY * screenY / w);

                twoDPoint.X = (int)X;
                twoDPoint.Y = (int)Y;

            }
            else
            {
                return new Point(-99, -99);
            }
            
            return twoDPoint;
            */   
        }

        void ESP()
        {
            while (true)
            {
                panel1.Refresh();
                Thread.Sleep(50);
            }
        }

        Vector3 GetBallVector()
        {
            byte[] buffer = new byte[3 * 4];
            var bytes = m_handler.ReadBytes(Convert.ToUInt32(PANGYA_BALL_BASE_ADDRESS, 16), buffer.Length);
            return new Vector3(
                BitConverter.ToSingle(bytes, (0 * 4)),
                BitConverter.ToSingle(bytes, (1 * 4)),
                BitConverter.ToSingle(bytes, (2 * 4))
            );
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            ViewMatrix matrix = GetViewMatrix();

            ball.position = GetBallVector();

            pointInWorld = WorldToScreen(matrix, ball.position, Width, Height);

            Pen pencil = new Pen(Color.Red, 2);
            Rectangle rectangle = new Rectangle(pointInWorld.X, pointInWorld.Y, 25, 25);
            DrawSomething(pencil, rectangle, e);

        }

        void DrawSomething(Pen pencil, Rectangle rect, PaintEventArgs g)
        {
            g.Graphics.DrawRectangle(pencil, rect);
        }

    }
}

