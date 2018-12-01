#undef DRAW_BUSES

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace BreadBoard
{
    /// <summary>
    /// Provides a way to interact with and simulate a Board
    /// </summary>
    public partial class BoardEditor : Form
    {
        /// <summary>
        /// The scale to use for drawing the Board
        /// </summary>
        private float BoardScale = 1f;

        /// <summary>
        /// The position of the top-left corner of the drawn Board
        /// </summary>
        private PointF BoardStart = new PointF(10f, 70f);
        /// <summary>
        /// The position of the top-left corner of the debug info on the canvas
        /// </summary>
        private PointF DebugInfoStart = new PointF(10f, 30f);

        /// <summary>
        /// The Board to simulate and display
        /// </summary>
        private Board Board = new Board(24, 14);

        /// <summary>
        /// The Component to draw over the mouse. Used for Component drag-drop actions
        /// </summary>
        private Component TempComponent = null;
        /// <summary>
        /// The position of the top-left corner of the TempComponent to draw on the canvas
        /// </summary>
        private PointF TempComponentStart = PointF.Empty;

        /// <summary>
        /// The file handle being edited
        /// </summary>
        private FileInfo _WorkingFile = null;
        /// <summary>
        /// Gets or sets the file handle being operated. Setting changes Text of editor
        /// </summary>
        private FileInfo WorkingFile
        {
            get { return _WorkingFile; }
            set
            {
                _WorkingFile = value;
                Text = string.Format("BreadBoard - {0}", value != null ? value.FullName : "Untitled");
            }
        }

        /// <summary>
        /// Creates an editor with a Board of default size. Populates components menu
        /// </summary>
        public BoardEditor()
        {
            InitializeComponent();
            WorkingFile = null;

            AddTool(new MicroController()
            {
                Size = new Size(4, 2),
                _Image = "img/mc 4x2 4s4x.png",

                Registers = new Register[]
                {
                    new Register(MicroController.AccumulatorAddress),
                    new Register("r0"), new Register("r1"), new Register("r2"), new Register("r3"),
                    new Register("r4"), new Register("r5"), new Register("r6"), new Register("r7")
                },
                SBuses = new SBus[]
                {
                    new SBus("s0", new Position(0, 0), Direction.Up),
                    new SBus("s1", new Position(1, 0), Direction.Up),

                    new SBus("s2", new Position(3, 1), Direction.Down),
                    new SBus("s3", new Position(2, 1), Direction.Down)
                    
                },
                XBuses = new XBus[]
                {
                    new XBus("x0", new Position(2, 0), Direction.Up),
                    new XBus("x1", new Position(3, 0), Direction.Up),

                    new XBus("x2", new Position(1, 1), Direction.Down),
                    new XBus("x3", new Position(0, 1), Direction.Down)
                },
            }, "MC 4x2 4s4x r0-7");
            AddTool(new MicroController()
            {
                Size = new Size(4, 2),
                _Image = "img/mc 4x2 8x.png",

                Registers = new Register[]
                {
                    new Register(MicroController.AccumulatorAddress),
                    new Register("r0"), new Register("r1"), new Register("r2"), new Register("r3"),
                    new Register("r4"), new Register("r5"), new Register("r6"), new Register("r7")
                },
                XBuses = new XBus[] {
                    new XBus("x0", new Position(0, 0), Direction.Up),
                    new XBus("x1", new Position(1, 0), Direction.Up),
                    new XBus("x2", new Position(2, 0), Direction.Up),
                    new XBus("x3", new Position(3, 0), Direction.Up),

                    new XBus("x4", new Position(3, 1), Direction.Down),
                    new XBus("x5", new Position(2, 1), Direction.Down),
                    new XBus("x6", new Position(1, 1), Direction.Down),
                    new XBus("x7", new Position(0, 1), Direction.Down)
                }
            }, "MC 4x2 8x r0-7");
            AddTool(new MicroController()
            {
                Size = new Size(4, 2),
                _Image = "img/mc 4x2 8s.png",

                Registers = new Register[]
                {
                    new Register(MicroController.AccumulatorAddress),
                    new Register("r0"), new Register("r1"), new Register("r2"), new Register("r3"),
                    new Register("r4"), new Register("r5"), new Register("r6"), new Register("r7")
                },
                SBuses = new SBus[]
                {
                    new SBus("s0", new Position(0, 0), Direction.Up),
                    new SBus("s1", new Position(1, 0), Direction.Up),
                    new SBus("s2", new Position(2, 0), Direction.Up),
                    new SBus("s3", new Position(3, 0), Direction.Up),

                    new SBus("s4", new Position(3, 1), Direction.Down),
                    new SBus("s5", new Position(2, 1), Direction.Down),
                    new SBus("s6", new Position(1, 1), Direction.Down),
                    new SBus("s7", new Position(0, 1), Direction.Down)
                }
            }, "MC 4x2 4s r0-7");
            AddTool(new Memory()
            {
                Size = new Size(4, 2),
                _Image = "img/ram 4x2.png",

                Capacity = 1024,

                XBuses = new XBus[]
                {
                    new XBus("xp0", new Position(0, 0), Direction.Up),
                    new XBus("xd0", new Position(1, 0), Direction.Up),

                    new XBus("xp1", new Position(3, 1), Direction.Down),
                    new XBus("xd1", new Position(2, 1), Direction.Down)
                }
            });
            AddTool(new PressButton()
            {
                Size = new Size(2, 2),
                _OffImage = "img/button 2x2 off.png",
                _OnImage = "img/button 2x2 on.png",

                SBuses = new SBus[]
                {
                    new SBus(new Position(0, 1), Direction.Left),
                    new SBus(new Position(1, 0), Direction.Right)
                }
            });
            AddTool(new ToggleButton()
            {
                Size = new Size(2, 2),
                _OffImage = "img/switch 2x2 off.png",
                _OnImage = "img/switch 2x2 on.png",

                SBuses = new SBus[]
                {
                    new SBus(new Position(0, 1), Direction.Left),
                    new SBus(new Position(1, 0), Direction.Right)
                }
            });

            LED LED_Base = new LED()
            {
                Size = new Size(1, 1),
                _Image = "img/led 1x1.png",

                Color = Color.Red,

                LeftOffset = 0.125f,
                TopOffset = 0.375f,
                RightOffset = 0.125f,
                BottomOffset = 0.375f,

                SBuses = new SBus[]
                {
                    new SBus(new Position(0, 0), Direction.Up),
                    new SBus(new Position(0, 0), Direction.Down)
                }
            };

            foreach (Color c in new Color[] { Color.Red, Color.Green, Color.Blue })
            {
                LED led = (LED)LED_Base.Clone();
                led.Color = c;
                led.Value = 255;
                AddTool(led);
            }

            AddTool(new NumericDisplay()
            {
                Size = new Size(8, 2),
                _Image = "img/display 8x2.png",

                TextColor = Color.Black,
                FontScale = 1f,

                HorizontalTextOffset = 0.45f,
                VerticalTextOffset = 0.45f,

                MinValue = -9999999999L,
                MaxValue =  9999999999L,

                XBuses = new XBus[]
                {
                    new XBus(new Position(0, 1), Direction.Left),
                    new XBus(new Position(7, 0), Direction.Right)
                }
            });
            AddTool(new TextDisplay()
            {
                Size = new Size(8, 2),
                _Image = "img/display 8x2.png",

                TextColor = Color.Black,
                FontScale = 1f,

                HorizontalTextOffset = 0.45f,
                VerticalTextOffset = 0.45f,

                MaxTextLength = 11,

                XBuses = new XBus[]
                {
                    new XBus(new Position(0, 1), Direction.Left),
                    new XBus(new Position(7, 0), Direction.Right)
                }
            });
            AddTool(new BitmapDisplay()
            {
                Size = new Size(8,8),
                _Image = "img/img disp 8x8.png",

                DefaultColor = Color.White,
                InactiveColor = Color.LightGray,

                Height = 32,
                Width = 32,

                LeftOffset = 0.4375f,
                TopOffset = 0.4375f,
                RightOffset = 0.4375f,
                BottomOffset = 0.4375f,
                
                XBuses = new XBus[]
                {
                    new XBus(new Position(0, 0), Direction.Left),
                    new XBus(new Position(7, 0), Direction.Right),
                    new XBus(new Position(0, 7), Direction.Left),
                    new XBus(new Position(7, 7), Direction.Right)
                }
            });
        }

        /// <summary>
        /// The size of the Control used to display a tool in the toolbox
        /// </summary>
        private static readonly Size ToolSize = new Size(200, 100);
        /// <summary>
        /// Adds the specified object (Component) to the toolbox with the specified caption
        /// </summary>
        /// <param name="obj">The object to add to the toolbox</param>
        /// <param name="caption">The caption to attach to the added toolbox item</param>
        private void AddTool<T>(T obj, string caption = "N/A") where T : Component, new()
        {
            RectangleF clip = obj.GetClip(PointF.Empty, 1f);
            Image img = new Bitmap((int)clip.Width, (int)clip.Height);
            Graphics g = Graphics.FromImage(img);

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            obj.Paint(g, PointF.Empty, 1f);

            g.Dispose();
            InterpPictureBox box = new InterpPictureBox();

            box.InterpolationMode = InterpolationMode.NearestNeighbor;
            box.SizeMode = PictureBoxSizeMode.Zoom;
            box.Size = ToolSize;
            box.Image = img;

            // setup tooltips

            ToolTip tip = new ToolTip();

            tip.ToolTipTitle = "Component";
            tip.SetToolTip(box, caption);

            ToolboxFlow.Controls.Add(box);

            // add spawn event

            box.MouseDown += (o, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                RectangleF act_rect = obj.GetClip(PointF.Empty, BoardScale);
                Point mouse = PointToClient(MousePosition);
                DragComponent(obj.Clone(),
                    new Point((int)(-act_rect.Width / 2) + mouse.X, (int)(-act_rect.Height / 2) + mouse.Y));
            };
        }

        /// <summary>
        /// The sum of the operations each MicroController has performed
        /// </summary>
        private long TotalOperations = 0;
        /// <summary>
        /// An average of all the operations the MicroControllers have performed since the last frame (in hertz)
        /// </summary>
        private double OperationsPerSecond = 0d;

        /// <summary>
        /// The brush to use for drawing the debugging info
        /// </summary>
        private static readonly SolidBrush DebugInfoBrush = new SolidBrush(Color.Black);
        /// <summary>
        /// The font to use for drawing the debugging info
        /// </summary>
        private static readonly Font DebugInfoFont = new Font(FontFamily.GenericMonospace, 30, GraphicsUnit.Pixel);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Board.Paint(e.Graphics, BoardStart, BoardScale);
            TempComponent?.Paint(e.Graphics, TempComponentStart, BoardScale);

            e.Graphics.DrawString(string.Format("{0} ops ({1:f2} hz)", TotalOperations, OperationsPerSecond),
                DebugInfoFont, DebugInfoBrush, DebugInfoStart);

#if DRAW_BUSES
            foreach (Component com in Board.EnumerateComponents)
                foreach (Bus bus in com.Buses)
                    bus.Paint(e.Graphics, com.GetBoardPoint(BoardOffset, BoardScale), BoardScale);
#endif
        }

        /// <summary>
        /// The point where the mouse was first pressed down
        /// </summary>
        private Point DownPoint = new Point();
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            DownPoint = e.Location;
            Component c = GetClicked(DownPoint);
            if (c != null)
            {
                if (Running) ComponentMouseDown(c);
                else
                {
                    if (e.Button == MouseButtons.Left)
                        DragComponent(c, Point.Truncate(c.GetBoardPoint(BoardStart, BoardScale)));
                    else if (e.Button == MouseButtons.Middle) c.Edit();
                }
            }
            else
            {
                if (!Running)
                {
                    if (e.Button == MouseButtons.Left) DrawCable(e.Location);
                    else if (e.Button == MouseButtons.Right) EraseCable(e.Location);
                }
            }
        }

        private async void ComponentMouseDown(Component com)
        {
            if (com == null) return;

            com.MouseDown();
            while (MouseButtons == MouseButtons.Left) await Task.Delay(DragSleepTime);
            com.MouseUp();
        }

        /// <summary>
        /// The amount of time (in ms) to wait between frames of drag action
        /// </summary>
        private const int DragSleepTime = 13;

        /// <summary>
        /// Allows the user to draw cables on the Board via dragging
        /// </summary>
        /// <param name="start">The point where the mouse was first pressed down</param>
        private async void DrawCable(Point start)
        {
            Position lastPos = GetPosition(start);
            Position nowPos;

            Point lastMouse = start;
            while (!Running && MouseButtons == MouseButtons.Left)
            {
                await Task.Delay(DragSleepTime);

                Point nowMouse = PointToClient(MousePosition);
                if (lastMouse == nowMouse) continue;
                lastMouse = nowMouse;

                nowPos = GetPosition(nowMouse);
                if (lastPos == nowPos) continue;

                Cable c = ModifierKeys == Keys.Shift ? (Cable)(new Bridge(lastPos, nowPos)) : new Solder(lastPos, nowPos);
                if (Board.AddCable(c)) Invalidate();

                lastPos = nowPos;
            }
        }
        /// <summary>
        /// Allows the user to erase cables on the Board via dragging
        /// </summary>
        /// <param name="start">The point where the mouse was first pressed down</param>
        private async void EraseCable(Point start)
        {
            Position lastPos = GetPosition(start);
            Position nowPos;

            Point lastMouse = start;
            while (!Running && MouseButtons == MouseButtons.Right)
            {
                await Task.Delay(DragSleepTime);

                Point nowMouse = PointToClient(MousePosition);
                if (lastMouse == nowMouse) continue;
                lastMouse = nowMouse;

                nowPos = GetPosition(nowMouse);
                if (lastPos == nowPos) continue;

                Cable cable = null;
                foreach (Cable cab in Board.EnumerateCables)
                    if (cab.Contains(lastPos) && cab.Contains(nowPos)) { cable = cab; break; }

                if (cable != null)
                {
                    Board.RemoveCable(cable);
                    Invalidate();
                }
                lastPos = nowPos;
            }
        }

        /// <summary>
        /// Drags the component around the Board
        /// </summary>
        /// <param name="com">The component to be dragged</param>
        /// <param name="start">The point where the mouse was pressed down</param>
        private async void DragComponent(Component com, Point start)
        {
            bool existed = Board.RemoveComponent(com);
            TempComponent = com;

            Position origPos = com.Position;

            Point mouseStart = PointToClient(MousePosition);
            Point mouseLast = mouseStart;
            while (!Running && MouseButtons == MouseButtons.Left)
            {
                await Task.Delay(DragSleepTime);

                Point mouseNow = PointToClient(MousePosition);
                if (mouseLast == mouseNow) continue;
                mouseLast = mouseNow;

                TempComponentStart = new PointF(start.X + mouseNow.X - mouseStart.X, start.Y + mouseNow.Y - mouseStart.Y);

                Invalidate();
            }

            TempComponent = null;

            if (!Running)
            {
                com.Position = GetClosestPosition(Point.Truncate(TempComponentStart));
                if (!Board.AddComponent(com) && existed)
                {
                    com.Position = origPos;
                    Board.AddComponent(com);
                }

                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!Running)
            {
                // handle component deletion
                Component com = GetClicked(DownPoint);
                if (com != null && GetClicked(e.Location) == com && e.Button == MouseButtons.Right)
                {
                    Board.RemoveComponent(com);
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets the component at the specified position on the Canvas (null if none)
        /// </summary>
        /// <param name="click">The position on the canvas to test</param>
        private Component GetClicked(Point click)
        {
            Position pos = GetPosition(click);
            foreach (Component c in Board.EnumerateComponents)
                if (c.Intersects(pos)) return c;

            return null;
        }

        /// <summary>
        /// Gets the grid position on the Board of the specified canvas position
        /// </summary>
        /// <param name="click">The canvas position to convert to a grid position</param>
        private Position GetPosition(Point click)
        {
            float width = Board.TileWidth * BoardScale;
            return new Position(
                (int)Math.Floor((click.X - BoardStart.X) / width),
                (int)Math.Floor((click.Y - BoardStart.Y) / width));
        }
        /// <summary>
        /// Gets the grid position on the Board of the specified canvas position (uses rounding)
        /// </summary>
        /// <param name="click">The canvas position to convert to a rounded Board position</param>
        private Position GetClosestPosition(Point click)
        {
            float width = Board.TileWidth * BoardScale;
            return new Position(
                (int)Math.Round((click.X - BoardStart.X) / width),
                (int)Math.Round((click.Y - BoardStart.Y) / width));
        }

        /// <summary>
        /// True iff there is a simulation in progress. Controlled by Start and Stop
        /// </summary>
        private bool Running = false;

        /// <summary>
        /// The amount of time (in ms) to wait between frames
        /// </summary>
        private const int SimSleepTime = 100;
        /// <summary>
        /// The number of simulation ticks to perform between each frae
        /// </summary>
        private const int SimsBeforeRender = 999;

        /// <summary>
        /// Begins simulating the Board
        /// </summary>
        private async void Simulate()
        {
            TotalOperations = 0;
            OperationsPerSecond = 0d;

            try
            {
                Board.Initialize();

                DateTime last = DateTime.Now;
                while (Running)
                {
                    await Task.Delay(SimSleepTime);

                    for (int i = 0; i < SimsBeforeRender; i++) Board.Tick(0d);

                    DateTime now = DateTime.Now;
                    double time = (now - last).TotalSeconds;
                    last = now;

                    Board.Tick(time);

                    long lastTotalOperations = TotalOperations;
                    TotalOperations = Board.EnumerateMicroControllers.Sum(mc => mc.Operations);

                    OperationsPerSecond = (TotalOperations - lastTotalOperations) / time;

                    Invalidate();
                }
            }
            catch (Exception ex)
            {
                Invalidate();
                MessageBox.Show(ex.Message);

                Stop();
            }

            TotalOperations = 0;
            OperationsPerSecond = 0d;

            Board.Reset();
            Invalidate();
        }

        /// <summary>
        /// Begins the simulation
        /// </summary>
        public void Start()
        {
            if (Running) return;
            Running = true;

            PauseButton.Text = "Stop";

            Simulate();
        }
        /// <summary>
        /// Ends the simulation. (WARNING: Does not wait for simulation loop to end before returning)
        /// </summary>
        public void Stop()
        {
            if (!Running) return;
            Running = false;

            PauseButton.Text = "Start";
        }
        
        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (Running) Stop(); else Start();
        }

        /// <summary>
        /// Prompts the user for a file destination to save the current Board to
        /// </summary>
        private void SaveAs()
        {
            SaveFileDialog d = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = Board.FileExtension,
                Filter = Board.FileFilter
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                WorkingFile = new FileInfo(d.FileName);
                Save();
            }
            d.Dispose();
        }
        /// <summary>
        /// Saves the current Board to the current working file handle (or calls SaveAs if file handle null)
        /// </summary>
        private void Save()
        {
            if (WorkingFile == null) { SaveAs(); return; }

            try
            {
                Board.Save(WorkingFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show("There was an error saving the file");
            }
        }

        /// <summary>
        /// Prompts the user for a BreadBoard file path to load
        /// </summary>
        private void Open()
        {
            OpenFileDialog d = new OpenFileDialog()
            {
                AddExtension = true,
                DefaultExt = Board.FileExtension,
                Filter = Board.FileFilter
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileInfo file = new FileInfo(d.FileName);
                    Board board = Board.Load(file);
                    if (board != null)
                    {
                        Board = board;
                        WorkingFile = file;
                        Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show("There was an error opening the file");
                }
            }
            d.Dispose();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }
    }
}
