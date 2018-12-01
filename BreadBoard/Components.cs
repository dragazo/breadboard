using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace BreadBoard
{
    /// <summary>
    /// Represents an abstraction for any Component to be simulated
    /// </summary>
    [
        XmlInclude(typeof(MicroController)), XmlInclude(typeof(Memory)),
        XmlInclude(typeof(PressButton)),     XmlInclude(typeof(ToggleButton)),
        XmlInclude(typeof(LED)),
        XmlInclude(typeof(NumericDisplay)),  XmlInclude(typeof(TextDisplay)),
        XmlInclude(typeof(BitmapDisplay))
    ]
    public abstract class Component
    {
        /// <summary>
        /// The position of the top left tile occupied by this Component
        /// </summary>
        public Position Position = new Position(0, 0);

        /// <summary>
        /// The Size of this Component in tiles
        /// </summary>
        [XmlIgnore]
        public Size Size = new Size(1, 1);
        /// <summary>
        /// The size of this Component in tiles as a Position - for XML serialization
        /// </summary>
        [XmlElement("Size")]
        public Position _Size
        {
            get { return Size; }
            set { Size = value; }
        }

        /// <summary>
        /// Gets the position of the bottom-right position occupied by this Component
        /// </summary>
        public Position AntiPosition
        {
            get { return new Position(Position.X + Size.Width - 1, Position.Y + Size.Height - 1); }
        }

        /// <summary>
        /// Contains all the SBuses in this Component
        /// </summary>
        public SBus[] SBuses = { };
        /// <summary>
        /// Contains all the XBuses in this Component
        /// </summary>
        public XBus[] XBuses = { };

        /// <summary>
        /// Iterates through all the SBuses and XBuses (in that order) in this Component
        /// </summary>
        public IEnumerable<Bus> EnumerateBuses
        {
            get
            {
                for (int i = 0; i < SBuses.Length; i++) yield return SBuses[i];
                for (int i = 0; i < XBuses.Length; i++) yield return XBuses[i];
            }
        }

        /// <summary>
        /// Gets the bus with the specified address
        /// </summary>
        /// <param name="address">the address to find</param>
        public Bus GetBus(string address)
        {
            foreach (Bus bus in EnumerateBuses)
                if (bus.Address == address) return bus;

            return null;
        }

        public abstract void Paint(Graphics g, PointF start, float scale);

        /// <summary>
        /// Gets the position of the top-left corner of this Component on the screen
        /// </summary>
        /// <param name="boardOffset">The top-left position of the Board being drawn on the screen</param>
        /// <param name="scale">The scale of the image to render</param>
        public PointF GetBoardPoint(PointF boardOffset, float scale)
        {
            float width = Board.TileWidth * scale;
            return new PointF(boardOffset.X + Position.X * width, boardOffset.Y + Position.Y * width);
        }

        /// <summary>
        /// Gets the clip of the Component on the screen with the specified settings.
        /// Offset by a small ammount to account for a Graphics.DrawImage oddity for small images
        /// </summary>
        /// <param name="start">The position of the top-left corner of this Component on the screen</param>
        /// <param name="scale">The scale used to draw the Component</param>
        public RectangleF GetClip(PointF start, float scale)
        {
            float width = Board.TileWidth * scale;
            return new RectangleF(start.X, start.Y,
                Size.Width * width, Size.Height * width);
        }
        /// <summary>
        /// Gets the clip of the Component on the Board with the specified settings.
        /// Offset by a small ammount to account for a Graphics.DrawImage oddity for small images
        /// </summary>
        /// <param name="boardOffset">The position of the top-left corner of the Board</param>
        /// <param name="scale">The scale to use for drawing the component</param>
        public RectangleF GetBoardClip(PointF boardOffset, float scale)
        {
            return GetClip(GetBoardPoint(boardOffset, scale), scale);
        }

        public virtual void MouseDown() { }
        public virtual void MouseUp() { }
        public virtual void Edit() { }

        /// <summary>
        /// Returns true iff the entity with specified position and size intersects this Component
        /// </summary>
        /// <param name="pos">The position of the entity to test against</param>
        /// <param name="size">The size of the entity to test against</param>
        public bool Intersects(Position pos, Size size)
        {
            int me_top = Position.Y, me_bottom = Position.Y + Size.Height,
                me_left = Position.X, me_right = Position.X + Size.Width;
            int other_top = pos.Y, other_bottom = pos.Y + size.Height,
                other_left = pos.X, other_right = pos.X + size.Width;

            return !(other_top >= me_bottom || other_bottom <= me_top
                || other_left >= me_right || other_right <= me_left);
        }
        /// <summary>
        /// Returns true iff the specified position is contained within this Component
        /// </summary>
        /// <param name="pos">The position to test</param>
        public bool Intersects(Position pos)
        {
            return Intersects(pos, new Size(1, 1));
        }
        /// <summary>
        /// Returns true iff the specified Component intersects this Component
        /// </summary>
        /// <param name="com">The component to test against</param>
        public bool Intersects(Component com)
        {
            return Intersects(com.Position, com.Size);
        }

        public virtual void Tick(Board board, double time) { }

        public virtual void Initialize() { }
        public virtual void Reset()
        {
            foreach (Bus bus in EnumerateBuses) bus.Reset();
        }

        /// <summary>
        /// Returns true iff the specified cable is valid relative to this Component.
        /// </summary>
        /// <param name="board">The Board this component is attached to</param>
        /// <param name="cable">The Cable to test</param>
        public bool CableValid(Board board, Cable cable)
        {
            bool a = Intersects(cable.A), b = Intersects(cable.B);
            if (!a && !b) return true;
            if (a && b) return false;

            // make sure the problem one is going into a bus port
            Position prob = a ? cable.A : cable.B;
            Position rel = prob - Position;
            List<Bus> buses = EnumerateBuses.Where(bus => bus.Position == rel).ToList();
            if (buses.Count == 0) return false;

            // make sure at least one bus is ok to plug into with this cable
            Position noProb = a ? cable.B : cable.A;
            if (!buses.Any(bus =>
            {
                // make sure if we're going into a bus port, we're going in the right way
                if (bus.Direction == Direction.Up && noProb != prob.Up) return false;
                if (bus.Direction == Direction.Down && noProb != prob.Down) return false;
                if (bus.Direction == Direction.Right && noProb != prob.Right) return false;
                if (bus.Direction == Direction.Left && noProb != prob.Left) return false;

                // make sure we're not mixing signal types
                if (board.GetConnectedBuses(board.GetCableLine(cable))
                    .Any(_b => _b.GetType() != bus.GetType())) return false;

                return true;
            })) return false;

            if (cable is Bridge) return false;

            return true;
        }

        public abstract Component Clone();
    }

    public sealed class PressButton : Component
    {
        private Image OnImage = Utility.DefaultImage, OffImage = Utility.DefaultImage;

        private string __OnImage = string.Empty;
        [XmlElement("OnImage")]
        public string _OnImage
        {
            get { return __OnImage; }
            set { __OnImage = value; OnImage = Utility.GetImage(value); }
        }

        private string __OffImage = string.Empty;
        [XmlElement("OffImage")]
        public string _OffImage
        {
            get { return __OffImage; }
            set { __OffImage = value; OffImage = Utility.GetImage(value); }
        }

        private bool State;

        public PressButton()
        {
            Reset();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            g.DrawImage(State ? OnImage : OffImage, GetClip(start, scale));
        }

        public override void MouseDown()
        {
            base.MouseDown();

            State = true;
        }
        public override void MouseUp()
        {
            base.MouseUp();

            State = false;
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            long val = State ? Language.SBusMaxValue : 0L;
            foreach (SBus bus in SBuses) bus.Value = val;
        }

        public override void Reset()
        {
            base.Reset();

            State = false;
        }

        public override Component Clone()
        {
            PressButton res = new PressButton()
            {
                Size = Size,
                _OnImage = _OnImage,
                _OffImage = _OffImage,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }
    }
    public sealed class ToggleButton : Component
    {
        private Image OnImage = Utility.DefaultImage, OffImage = Utility.DefaultImage;

        private string __OnImage = string.Empty;
        [XmlElement("OnImage")]
        public string _OnImage
        {
            get { return __OnImage; }
            set { __OnImage = value; OnImage = Utility.GetImage(value); }
        }

        private string __OffImage = string.Empty;
        [XmlElement("OffImage")]
        public string _OffImage
        {
            get { return __OffImage; }
            set { __OffImage = value; OffImage = Utility.GetImage(value); }
        }

        private bool State;

        public ToggleButton()
        {
            Reset();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            g.DrawImage(State ? OnImage : OffImage, GetClip(start, scale));
        }

        public override void MouseDown()
        {
            base.MouseDown();

            State = !State;
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            long val = State ? Language.SBusMaxValue : 0L;
            foreach (SBus bus in SBuses) bus.Value = val;
        }

        public override void Reset()
        {
            base.Reset();

            State = false;
        }

        public override Component Clone()
        {
            ToggleButton obj = new ToggleButton()
            {
                Size = Size,
                _OnImage = _OnImage,
                _OffImage = _OffImage,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return obj;
        }
    }

    public sealed class LED : Component
    {
        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        [XmlIgnore]
        public Color Color = Color.Red;
        [XmlElement("Color")]
        public string _Color
        {
            get { return Utility.ColorToString(Color); }
            set { if(!string.IsNullOrEmpty(value)) Color = Utility.StringToColor(value); }
        }

        public long Value;

        public float LeftOffset = 0f, TopOffset = 0f, RightOffset = 0f, BottomOffset = 0f;

        public LED()
        {
            Reset();
        }

        private static readonly SolidBrush LightBackBrush = new SolidBrush(Color.LightGray);

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;

            RectangleF clip = GetClip(start, scale);
            RectangleF colorClip = new RectangleF(
                clip.X + LeftOffset * width,
                clip.Y + TopOffset * width,
                clip.Width - width * (LeftOffset + RightOffset),
                clip.Height - width * (TopOffset + BottomOffset));

            g.FillRectangle(LightBackBrush, colorClip);
            if(Value > 0) g.FillRectangle(new SolidBrush(Color.FromArgb((int)Value, Color)), colorClip);
            g.DrawImage(Image, GetClip(start, scale));
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            long val = 0L;
            foreach (SBus bus in SBuses)
            {
                long _val = board.GetCableSignal(bus);
                if (_val > val) val = _val;
            }

            Value = val;
        }

        public override void Reset()
        {
            base.Reset();

            Value = 0L;
        }

        public override Component Clone()
        {
            LED res = new LED()
            {
                Size = Size,
                _Image = _Image,

                Color = Color,

                LeftOffset = LeftOffset,
                TopOffset = TopOffset,
                RightOffset = RightOffset,
                BottomOffset = BottomOffset,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }
    }

    public sealed class NumericDisplay : Component, IDisposable
    {
        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        private string Text;

        public NumericDisplay()
        {
            Reset();
        }
        ~NumericDisplay()
        {
            Dispose();
        }

        private SolidBrush TextBrush = new SolidBrush(Color.Black);
        public float FontScale = 1f;
        public float HorizontalTextOffset = 0f, VerticalTextOffset = 0f;

        [XmlIgnore]
        public Color TextColor
        {
            get { return TextBrush.Color; }
            set { TextBrush.Color = value; }
        }
        [XmlElement("TextColor")]
        public string _TextColor
        {
            get { return Utility.ColorToString(TextColor); }
            set { TextColor = Utility.StringToColor(value); }
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;

            RectangleF rect = GetClip(start, scale);
            g.DrawImage(Image, rect);

            Font font = new Font(FontFamily.GenericMonospace, width * FontScale, GraphicsUnit.Pixel);
            PointF textStart = new PointF(
                start.X + width * HorizontalTextOffset,
                start.Y + width * VerticalTextOffset);

            g.DrawString(Text, font, TextBrush, textStart);
        }

        public long MinValue = -999999999L, MaxValue = 999999999L;
        public int DisplayBase = 10;

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            foreach (XBus bus in XBuses)
            {
                if (bus.State == BusState.ReadComplete)
                    Text = Convert.ToString(bus.Value.Clamp(MinValue, MaxValue),
                        DisplayBase.IsAny(2, 8, 10, 16) ? DisplayBase : 10);

                bus.State = BusState.Reading;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Text = "0";
        }
        public override void Reset()
        {
            base.Reset();

            Text = string.Empty;
        }

        public override Component Clone()
        {
            NumericDisplay res = new NumericDisplay()
            {
                Size = Size,
                _Image = _Image,

                TextColor = TextColor,
                FontScale = FontScale,

                DisplayBase = DisplayBase,

                HorizontalTextOffset = HorizontalTextOffset,
                VerticalTextOffset = VerticalTextOffset,

                MinValue = MinValue,
                MaxValue = MaxValue,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }

        public void Dispose()
        {
            TextBrush.Dispose();
        }
    }
    public sealed class TextDisplay : Component, IDisposable
    {
        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        private SolidBrush TextBrush = new SolidBrush(Color.Black);
        public float FontScale = 1f;
        public float HorizontalTextOffset = 0f, VerticalTextOffset = 0f;

        public int MaxTextLength = 10;

        [XmlIgnore]
        public Color TextColor
        {
            get { return TextBrush.Color; }
            set { TextBrush.Color = value; }
        }
        [XmlElement("TextColor")]
        public string _TextColor
        {
            get { return Utility.ColorToString(TextColor); }
            set { TextColor = Utility.StringToColor(value); }
        }

        private string Text;

        public TextDisplay()
        {
            Reset();
        }
        ~TextDisplay()
        {
            Dispose();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;

            RectangleF rect = GetClip(start, scale);
            g.DrawImage(Image, rect);

            Font font = new Font(FontFamily.GenericMonospace, width * FontScale, GraphicsUnit.Pixel);
            PointF textStart = new PointF(
                start.X + width * HorizontalTextOffset,
                start.Y + width * VerticalTextOffset);

            g.DrawString(Text, font, TextBrush, textStart);
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            foreach (XBus bus in XBuses)
            {
                if (bus.State == BusState.ReadComplete)
                {
                    char ch = (char)bus.Value;
                    short pos = (short)(bus.Value >> 16);

                    if (pos >= 0 && pos < MaxTextLength)
                    {
                        char[] text = new char[MaxTextLength];
                        for (int i = 0; i < Text.Length; i++) text[i] = Text[i];
                        for (int i = Text.Length; i < text.Length; i++) text[i] = ' ';
                        text[pos] = ch;

                        Text = new string(text);
                    }
                }
                bus.State = BusState.Reading;
            }
        }

        public override void Reset()
        {
            base.Reset();

            Text = string.Empty;
        }

        public override Component Clone()
        {
            TextDisplay res = new TextDisplay()
            {
                Size = Size,
                _Image = _Image,

                TextColor = TextColor,
                FontScale = FontScale,

                HorizontalTextOffset = HorizontalTextOffset,
                VerticalTextOffset = VerticalTextOffset,

                MaxTextLength = MaxTextLength,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }

        public void Dispose()
        {
            TextBrush.Dispose();
        }
    }

    public sealed class BitmapDisplay : Component, IDisposable
    {
        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        [XmlIgnore]
        public Color DefaultColor = Color.White;
        [XmlElement("DefaultColor")]
        public string _DefaultColor
        {
            get { return Utility.ColorToString(DefaultColor); }
            set { DefaultColor = Utility.StringToColor(value); }
        }

        [XmlIgnore]
        public Color InactiveColor = Color.LightGray;
        [XmlElement("InactiveColor")]
        public string _InactiveColor
        {
            get { return Utility.ColorToString(InactiveColor); }
            set { InactiveColor = Utility.StringToColor(value); }
        }

        private const short MaxWidth = 1024, MaxHeight = 1024;
        private short _Width = 32, _Height = 32;

        public short Width
        {
            get { return _Width; }
            set
            {
                _Width = Math.Max(value, (short)1);

                if (Map.Width != value)
                {
                    Map.Dispose();
                    Map = new Bitmap(Width, Height);
                }

                Reset();
            }
        }
        public short Height
        {
            get { return _Height; }
            set
            {
                _Height = Math.Max(value, (short)1);

                if (Map.Height != value)
                {
                    Map.Dispose();
                    Map = new Bitmap(Width, Height);
                }

                Reset();
            }
        }

        public float LeftOffset = 0f, TopOffset = 0f, RightOffset = 0f, BottomOffset = 0f;

        private Bitmap Map = null;

        public BitmapDisplay()
        {
            Map = new Bitmap(Width, Height);

            Reset();
        }
        ~BitmapDisplay()
        {
            Dispose();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;

            RectangleF rect = GetClip(start, scale);
            RectangleF mapRect = new RectangleF(
                rect.X + width * LeftOffset,
                rect.Y + width * TopOffset,
                rect.Width - width * (RightOffset + LeftOffset),
                rect.Height - width * (TopOffset + BottomOffset));

            g.DrawImage(Image, rect);
            g.DrawImage(Map, mapRect);
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            foreach (XBus bus in XBuses)
            {
                if (bus.State == BusState.ReadComplete)
                {
                    byte b = (byte)bus.Value;
                    byte g = (byte)(bus.Value >> 8);
                    byte r = (byte)(bus.Value >> 16);

                    short y = (short)(bus.Value >> 24);
                    short x = (short)(bus.Value >> 40);

                    if (x >= 0 && x < Map.Width && y >= 0 && y < Map.Height)
                        Map.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
                bus.State = BusState.Reading;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Map.Clear(DefaultColor);
        }
        public override void Reset()
        {
            base.Reset();

            Map.Clear(InactiveColor);
        }

        public override Component Clone()
        {
            BitmapDisplay res = new BitmapDisplay()
            {
                Size = Size,
                _Image = _Image,

                DefaultColor = DefaultColor,
                InactiveColor = InactiveColor,

                Width = Width,
                Height = Height,

                LeftOffset = LeftOffset,
                TopOffset = TopOffset,
                RightOffset = RightOffset,
                BottomOffset = BottomOffset,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }

        public void Dispose()
        {
            Map.Dispose();
        }
    }

    public sealed class Memory : Component
    {
        public class BusPair
        {
            public XBus PointerBus, DataBus;

            public BusPair(XBus pointerBus, XBus dataBus)
            {
                PointerBus = pointerBus;
                DataBus = dataBus;
            }
            public BusPair()
            {
                PointerBus = DataBus = null;
            }
        }

        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        private List<BusPair> BusPairs = null;
        
        private long[] Data = { };

        private const int MaxCapacity = 2048;
        public int Capacity
        {
            get { return Data.Length; }
            set { Data = new long[value.Clamp(0, MaxCapacity)]; }
        }

        public Memory()
        {
            Reset();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            g.DrawImage(Image, GetClip(start, scale));
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);

            foreach (BusPair pair in BusPairs)
            {
                if (pair.PointerBus.Value < 0 || pair.PointerBus.Value >= Data.Length)
                    throw new IndexOutOfRangeException(string.Format(
                        "virtual pointer out of range ({0}) range [0-{1}]", pair.PointerBus.Value, Data.Length - 1));
                pair.PointerBus.State = BusState.ReadingWriting;

                if (pair.DataBus.State == BusState.ReadComplete) Data[pair.PointerBus.Value] = pair.DataBus.Value;
                pair.DataBus.State = BusState.ReadingWriting;

                pair.DataBus.Value = Data[pair.PointerBus.Value];
            }
        }

        public const string PointerBusPrefix = "xp", DataBusPrefix = "xd";
        public override void Initialize()
        {
            base.Initialize();

            if (BusPairs == null)
            {
                BusPairs = new List<BusPair>();
                foreach (XBus bus in XBuses)
                {
                    if (bus.Address.StartsWith(PointerBusPrefix))
                    {
                        string otherAddress = DataBusPrefix + bus.Address.Substring(2);
                        List<XBus> others = XBuses.Where(b => b.Address == otherAddress).ToList();

                        if (others.Count == 0) throw new ArgumentException(string.Format(
                             "could not find corresponding data bus for pointer bus \"{0}\"", bus.Address));
                        if (others.Count > 1) throw new ArgumentException(string.Format(
                             "multiple corresponding data buses forund for pointer bus \"{0}\"", bus.Address));

                        BusPairs.Add(new BusPair(bus, others[0]));
                    }
                }
            }
        }
        public override void Reset()
        {
            base.Reset();

            for (int i = 0; i < Data.Length; i++) Data[i] = 0L;
        }

        public override Component Clone()
        {
            Memory res = new Memory()
            {
                Size = Size,
                _Image = _Image,

                Capacity = Capacity,

                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }
    }
}
