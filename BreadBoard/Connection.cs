using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

namespace BreadBoard
{
    // ------ Interfaces ---------------------------------------------------

    public interface IDataLocation
    {
        string GetAddress();

        long GetValue();
        void SetValue(long value);
        void Reset();
    }

    // ------ Data ---------------------------------------------------------

    public sealed class Register : IDataLocation
    {
        [XmlAttribute]
        public string Address = "N/A";
        [XmlIgnore]
        public long Value = 0L;

        public Register(string address)
        {
            Address = address;
        }
        public Register() { }

        public void Reset()
        {
            Value = 0L;
        }

        public Register Clone()
        {
            return new Register(Address);
        }

        // interface-specific methods

        public string GetAddress() { return Address; }

        public long GetValue() { return Value; }
        public void SetValue(long value) { Value = value; }
    }

    // ------ Buses --------------------------------------------------------

    /// <summary>
    /// Represents the possible states of an XBus
    /// </summary>
    public enum BusState
    {
        Idle,
        Reading,
        Writing,
        ReadingWriting,
        WriteComplete,
        ReadComplete
    }

    [XmlInclude(typeof(SBus)), XmlInclude(typeof(XBus))]
    public abstract class Bus : IDataLocation
    {
        /// <summary>
        /// The address of this Bus. Used by the MicroController during interpretation
        /// </summary>
        [XmlAttribute]
        public string Address = "N/A";
        /// <summary>
        /// The value associated with this Bus
        /// </summary>
        [XmlIgnore]
        public long Value = 0L;

        /// <summary>
        /// The position of the Bus relative to the Component's top-left corner
        /// </summary>
        [XmlIgnore]
        public Position Position = Position.Invalid;

        /// <summary>
        /// Gets or sets the X value of Position. Used for serialization
        /// </summary>
        [XmlAttribute("X")]
        public int _X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }
        /// <summary>
        /// Gets or sets the Y value of Position. Used for serialization
        /// </summary>
        [XmlAttribute("Y")]
        public int _Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        /// <summary>
        /// The direction this Bus is facing
        /// </summary>
        [XmlAttribute]
        public Direction Direction = Direction.Up;

        public Bus(string address, Position position, Direction direction)
        {
            Address = address;
            Position = position;
            Direction = direction;
        }
        public Bus(Position position, Direction direction)
        {
            Position = position;
            Direction = direction;
        }
        public Bus() { }

        private static readonly SolidBrush
            TextBackBrush = new SolidBrush(Color.FromArgb(255, Color.Black)),
            TextBrush = new SolidBrush(Color.FromArgb(255, Color.White));

        /// <summary>
        /// The number of tiles to shift label text from the center of a tile
        /// </summary>
        private const float LabelCenterOffset = 0.25f;
        /// <summary>
        /// The number of tiles high the label font should be
        /// </summary>
        private const float FontScale = 0.35f;
        /// <summary>
        /// The ratio of width to height of a label character
        /// </summary>
        private const float FontFormFactor = 0.75f;

        /// <summary>
        /// Paints the Address of this Bus at the correct position
        /// </summary>
        /// <param name="g">The Graphics object to use for drawing</param>
        /// <param name="comStart">The position of the top-left corner of the component this bus is attached to</param>
        /// <param name="scale">The scale of the component being drawn</param>
        public virtual void Paint(Graphics g, PointF comStart, float scale)
        {
            float width = Board.TileWidth * scale;

            float x = comStart.X + (Position.X + 0.5f) * width;
            float y = comStart.Y + (Position.Y + 0.5f) * width;

            float delta = LabelCenterOffset * width;
            switch (Direction)
            {
                case Direction.Up:
                    y -= delta;
                    break;
                case Direction.Down:
                    y += delta;
                    break;
                case Direction.Right:
                    x += delta;
                    break;
                case Direction.Left:
                    x -= delta;
                    break;
            }

            x -= FontFormFactor * FontScale * width * Address.Length / 2f;
            y -= FontScale * width / 2f;

            Font font = new Font(FontFamily.GenericMonospace, FontScale * width, GraphicsUnit.Pixel);
            g.DrawString(Address, font, TextBrush, x, y);
        }

        /// <summary>
        /// Gets the cable connected to this bus or null if not connected
        /// </summary>
        /// <param name="board">The Board being simulated</param>
        /// <param name="com">The component this Bus is attached to</param>
        public Cable GetConnectedCable(Board board, Component com)
        {
            Position act = com.Position + Position;
            return board.EnumerateCables.FirstOrDefault(cable =>
            {
                bool a = cable.A == act, b = cable.B == act;
                if (!a && !b) return false;

                Position other = a ? cable.B : cable.A;
                if (Direction == Direction.Up && act.Up != other) return false;
                if (Direction == Direction.Down && act.Down != other) return false;
                if (Direction == Direction.Right && act.Right != other) return false;
                if (Direction == Direction.Left && act.Left != other) return false;

                return true;
            });
        }

        /// <summary>
        /// Resets this Cable to its default conditions after initialization
        /// </summary>
        public abstract void Reset();

        public abstract Bus Clone();

        // interface-specific methods

        public string GetAddress() { return Address; }

        public long GetValue() { return Value; }
        public void SetValue(long value) { Value = value; }
    }

    public sealed class SBus : Bus
    {
        public SBus(string address, Position position, Direction direction) : base(address, position, direction) { }
        public SBus(Position position, Direction direction) : base(position, direction) { }
        public SBus() : base() { }

        public override void Reset()
        {
            Value = 0L;
        }

        public override Bus Clone()
        {
            return new SBus(Address, Position, Direction);
        }
    }
    public sealed class XBus : Bus
    {
        /// <summary>
        /// The data transfer state of this Bus. Used by the Board during simulation. Only used for XBus
        /// </summary>
        [XmlIgnore]
        public BusState State = BusState.Idle;

        public XBus(string address, Position position, Direction direction) : base(address, position, direction) { }
        public XBus(Position position, Direction direction) : base(position, direction) { }
        public XBus() : base() { }

        public override void Reset()
        {
            Value = 0L;
            State = BusState.Idle;
        }

        public override Bus Clone()
        {
            return new XBus(Address, Position, Direction);
        }
    }

    // ------ Cables -------------------------------------------------------

    [XmlInclude(typeof(Solder)), XmlInclude(typeof(Bridge))]
    public abstract class Cable
    {
        [XmlIgnore]
        public Position A = Position.Invalid, B = Position.Invalid;

        /// <summary>
        /// Gets or sets the X value of A. Used for serialization
        /// </summary>
        [XmlAttribute]
        public int Ax
        {
            get { return A.X; }
            set { A.X = value; }
        }
        /// <summary>
        /// Gets or sets the Y value of A. Used for serialization
        /// </summary>
        [XmlAttribute]
        public int Ay
        {
            get { return A.Y; }
            set { A.Y = value; }
        }

        /// <summary>
        /// Gets or sets the X value of B. Used for serialization
        /// </summary>
        [XmlAttribute]
        public int Bx
        {
            get { return B.X; }
            set { B.X = value; }
        }
        /// <summary>
        /// Gets or sets the Y value of B. Used for serialization
        /// </summary>
        [XmlAttribute]
        public int By
        {
            get { return B.Y; }
            set { B.Y = value; }
        }

        public Cable(Position a, Position b)
        {
            A = a;
            B = b;
        }
        public Cable(Position p, Direction d)
        {
            A = p;

            switch (d)
            {
                case Direction.Up:
                    B = p.Up;
                    break;
                case Direction.Down:
                    B = p.Down;
                    break;
                case Direction.Left:
                    B = p.Left;
                    break;
                case Direction.Right:
                    B = p.Right;
                    break;
            }
        }
        public Cable() { }

        public abstract void Paint(Graphics g, PointF start, float scale);

        /// <summary>
        /// Returns true iff the this cable and the spcified cable contain the same positions
        /// (not necessarily same order)
        /// </summary>
        /// <param name="other">The cable to compare</param>
        public bool Identical(Cable other)
        {
            return Contains(other.A) && Contains(other.B);
        }
        /// <summary>
        /// Returns true iff this cable and the specified cable contain at least one position in common
        /// </summary>
        /// <param name="other">The cable to compare</param>
        public bool Connected(Cable other)
        {
            return Contains(other.A) || Contains(other.B);
        }
        /// <summary>
        /// Returns true iff either position in this cable is the specified position
        /// </summary>
        /// <param name="pos">The position to test</param>
        public bool Contains(Position pos)
        {
            return A == pos || B == pos;
        }

        /// <summary>
        /// Returns true iff this cable is vertical
        /// </summary>
        public bool Vertical
        {
            get { return A.Up == B || A.Down == B; }
        }
        /// <summary>
        /// Returns true iff this cable is horizontal
        /// </summary>
        public bool Horizontal
        {
            get { return A.Right == B || A.Left == B; }
        }

        public abstract Cable Clone();
    }

    public class Solder : Cable
    {
        public Solder(Position a, Position b) : base(a, b) { }
        public Solder(Position p, Direction d) : base(p, d) { }
        public Solder() : base() { }

        private static readonly Color Color = Color.LightGray;
        private const float WidthMult = 0.2f;

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;
            PointF
                a = new PointF((A.X + 0.5f) * width + start.X, (A.Y + 0.5f) * width + start.Y),
                b = new PointF((B.X + 0.5f) * width + start.X, (B.Y + 0.5f) * width + start.Y);
            Pen pen = new Pen(Color, width * WidthMult)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.DrawLine(pen, a, b);
        }

        public override Cable Clone()
        {
            return new Solder(A, B);
        }
    }
    public class Bridge : Cable
    {
        public Bridge(Position a, Position b) : base(a, b) { }
        public Bridge(Position p, Direction d) : base(p, d) { }
        public Bridge() : base() { }

        private static readonly Color Color = Color.DarkGray;
        private const float WidthMult = 0.4f;

        public override void Paint(Graphics g, PointF start, float scale)
        {
            float width = Board.TileWidth * scale;
            PointF
                a = new PointF((A.X + 0.5f) * width + start.X, (A.Y + 0.5f) * width + start.Y),
                b = new PointF((B.X + 0.5f) * width + start.X, (B.Y + 0.5f) * width + start.Y);
            Pen pen = new Pen(Color, width * WidthMult)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.DrawLine(pen, a, b);
        }

        public override Cable Clone()
        {
            return new Bridge(A, B);
        }
    }
}
