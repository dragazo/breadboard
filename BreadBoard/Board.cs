using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace BreadBoard
{
    /// <summary>
    /// A BreadBoard simulation space
    /// </summary>
    public class Board
    {
        /// <summary>
        /// The width of one tile in a drawn render with a scale of 1.0f in pixels
        /// </summary>
        public const float TileWidth = 64f;

        /// <summary>
        /// The MicroControllers contained within this Board
        /// </summary>
        private List<MicroController> MicroControllers = new List<MicroController>();

        /// <summary>
        /// Contains all the Solders in this Board
        /// </summary>
        private List<Solder> Solders = new List<Solder>();
        /// <summary>
        /// Contains all the Bridges in this Board
        /// </summary>
        private List<Bridge> Bridges = new List<Bridge>();

        /// <summary>
        /// The Components contained within this Board
        /// </summary>
        private List<Component> Components = new List<Component>();

        /// <summary>
        /// Gets an array of clones of the Comonents in this Board
        /// </summary>
        [XmlArray("Components")]
        public Component[] _Components
        {
            get
            {
                return Components.Select(com => com.Clone()).ToArray();
            }
            set
            {
                foreach (Component com in value) AddComponent(com);
            }
        }
        /// <summary>
        /// Gets an array of clones of the Cables in this Board
        /// </summary>
        [XmlArray("Cables")]
        public Cable[] _Cables
        {
            get
            {
                return EnumerateCables.Select(cable => cable.Clone()).ToArray();
            }
            set
            {
                // bridges have odd connectivity, so do them first
                foreach (Bridge bridge in value.Where(cable => cable is Bridge)) AddCable(bridge);
                foreach (Solder solder in value.Where(cable => cable is Solder)) AddCable(solder);
            }
        }
        
        /// <summary>
        /// Contains pre-cached bus connections. Each definition contains all other busses connected
        /// </summary>
        private Dictionary<Bus, List<Bus>> Connections = new Dictionary<Bus, List<Bus>>();

        /// <summary>
        /// The width of this Board
        /// </summary>
        public int Width;
        /// <summary>
        /// The height of this Board
        /// </summary>
        public int Height;

        /// <summary>
        /// Creates an empty Board with the specified width and height
        /// </summary>
        /// <param name="width">The width of the Board to create</param>
        /// <param name="height">The height of the Board to create</param>
        public Board(int width, int height)
        {
            Width = width;
            Height = height;
        }
        /// <summary>
        /// Creates an empty Board with the default width and height
        /// </summary>
        public Board() : this(10, 10) { }

        /// <summary>
        /// Iterates through all MicroControllers in this Board
        /// </summary>
        public IEnumerable<MicroController> EnumerateMicroControllers
        {
            get
            {
                for (int i = 0; i < MicroControllers.Count; i++) yield return MicroControllers[i];
            }
        }

        /// <summary>
        /// Iterates through each Component in the Board
        /// </summary>
        public IEnumerable<Component> EnumerateComponents
        {
            get { for (int i = 0; i < Components.Count; i++) yield return Components[i]; }
        }

        /// <summary>
        /// Iterates through all the Solders in this Board
        /// </summary>
        public IEnumerable<Solder> EnumerateSolders
        {
            get
            {
                for (int i = 0; i < Solders.Count; i++) yield return Solders[i];
            }
        }
        /// <summary>
        /// Iterates through all the Bridges in this Board
        /// </summary>
        public IEnumerable<Bridge> EnumerateBridges
        {
            get
            {
                for (int i = 0; i < Bridges.Count; i++) yield return Bridges[i];
            }
        }

        /// <summary>
        /// Iterates through all the Solders and Bridges (in that order) in this Board
        /// </summary>
        public IEnumerable<Cable> EnumerateCables
        {
            get
            {
                for (int i = 0; i < Solders.Count; i++) yield return Solders[i];
                for (int i = 0; i < Bridges.Count; i++) yield return Bridges[i];
            }
        }

        /// <summary>
        /// Returns a list of all cables connected to the specified Cable (including the specified Cable)
        /// </summary>
        /// <param name="start">The cable used to begin the flood-fill process</param>
        public List<Cable> GetCableLine(Cable start)
        {
            if (start == null) return new List<Cable>();
            List<Cable> cables = new List<Cable>() { start };

            List<KeyValuePair<Cable, Position>> last = new List<KeyValuePair<Cable, Position>>();
            if (!Components.Any(com => com.Intersects(start.A)))
                last.Add(new KeyValuePair<Cable, Position>(start, start.A));
            if (!Components.Any(com => com.Intersects(start.B)))
                last.Add(new KeyValuePair<Cable, Position>(start, start.B));

            while (last.Count > 0)
            {
                List<KeyValuePair<Cable, Position>> now = new List<KeyValuePair<Cable, Position>>();

                foreach (KeyValuePair<Cable, Position> entry in last)
                {
                    int bridgeCount = EnumerateBridges.Count(b => b.Contains(entry.Value));

                    foreach (Cable cable in EnumerateCables.Where(c => c.Contains(entry.Value) && !cables.Contains(c)))
                    {
                        // can only switch cable types during floodfill if at a bridge end
                        if (entry.Key.GetType() != cable.GetType() && bridgeCount > 1) continue;

                        if (!Components.Any(com => com.Intersects(cable.A)))
                            now.Add(new KeyValuePair<Cable, Position>(cable, cable.A));
                        if (!Components.Any(com => com.Intersects(cable.B)))
                            now.Add(new KeyValuePair<Cable, Position>(cable, cable.B));

                        cables.Add(cable);
                    }
                }

                last = now;
            }

            return cables;
        }
        /// <summary>
        /// Returns a list of all busses connected to the specified Cable line
        /// </summary>
        /// <param name="cables">The Cable line to test</param>
        public List<Bus> GetConnectedBuses(List<Cable> cables)
        {
            List<Bus> buses = new List<Bus>();

            foreach (Component com in Components)
                foreach (Bus bus in com.EnumerateBuses)
                    if (cables.Any(cable => bus.GetConnectedCable(this, com) == cable)) buses.Add(bus);

            return buses;
        }

        /// <summary>
        /// Gets the (simple) signal value of the cable line connected to the specified bus.
        /// </summary>
        /// <param name="bus">The bus to check the signal of</param>
        public long GetCableSignal(Bus bus)
        {
            long highest = bus.Value;
            foreach (Bus other in Connections[bus])
                if (other.Value > highest) highest = other.Value;
            return highest;
        }

        /// <summary>
        /// The Pen used for drawing the Board grid
        /// </summary>
        private static readonly Pen GridPen = new Pen(Color.LightGray, 2f) { DashStyle = DashStyle.Dash };

        /// <summary>
        /// Draws the Board to an Image using the specified Graphics object and settings
        /// </summary>
        /// <param name="g">The Graphics object used for drawing</param>
        /// <param name="boardOffset">The Position of the top-left corner of the Board</param>
        /// <param name="scale">The scale of the Board to be drawn</param>
        public void Paint(Graphics g, PointF boardOffset, float scale)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            // draw the Board grid
            float width = TileWidth * scale;
            for (int i = 0; i <= Width; i++) g.DrawLine(GridPen,
                new PointF(boardOffset.X + i * width, boardOffset.Y),
                new PointF(boardOffset.X + i * width, boardOffset.Y + Height * width));
            for (int i = 0; i <= Height; i++) g.DrawLine(GridPen,
                new PointF(boardOffset.X, boardOffset.Y + i * width),
                new PointF(boardOffset.X + Width * width, boardOffset.Y + i * width));

            foreach (Cable c in EnumerateCables) c.Paint(g, boardOffset, scale);
            foreach (Component c in Components) c.Paint(g, c.GetBoardPoint(boardOffset, scale), scale);
        }

        /// <summary>
        /// Adds the specified component to the Board. Returns true iff successful
        /// </summary>
        /// <param name="c">The Component to add</param>
        public bool AddComponent(Component c)
        {
            if (!ValidPosition(c.Position) || !ValidPosition(c.AntiPosition)) return false;

            foreach (Component com in Components)
                if (com.Intersects(c)) return false;
            foreach (Cable cab in EnumerateCables)
                if (!c.CableValid(this, cab)) return false;

            Components.Add(c);
            if (c is MicroController) MicroControllers.Add((MicroController)c);

            return true;
        }
        /// <summary>
        /// Removes the specified Component from the Board. Returns true iff successful
        /// </summary>
        /// <param name="c">The Component to remove</param>
        public bool RemoveComponent(Component c)
        {
            if (!Components.Remove(c)) return false;

            if (c is MicroController) MicroControllers.Remove((MicroController)c);
            return true;
        }

        /// <summary>
        /// Adds the specified Cable to the Board if legal. Returns true iff successful
        /// </summary>
        /// <param name="c">The cable to add</param>
        public bool AddCable(Cable c)
        {
            if (!ValidPosition(c.A) || !ValidPosition(c.B) || !c.A.Adjacent(c.B)) return false;

            foreach (Cable cable in EnumerateCables)
                if (cable.Identical(c)) return false;
            foreach (Component com in Components)
                if (!com.CableValid(this, c)) return false;

            // make sure we don't mix bus types
            List<Bus> buses = GetConnectedBuses(GetCableLine(c));
            if (buses.Count > 0 && buses.Any(b => b.GetType() != buses[0].GetType())) return false;

            if (c is Solder) { Solders.Add((Solder)c); return true; }
            if (c is Bridge) { Bridges.Add((Bridge)c); return true; }

            return false;
        }
        /// <summary>
        /// Removes the specified Cable from the Board. Returns true iff successful
        /// </summary>
        /// <param name="c">The Cable to remove</param>
        public bool RemoveCable(Cable c)
        {
            if (c is Solder) return Solders.Remove((Solder)c);
            if (c is Bridge) return Bridges.Remove((Bridge)c);

            return false;
        }

        /// <summary>
        /// Returns true iff the specified position is within the bounds of this Board.
        /// Does not ensure this position is legal to place something into.
        /// </summary>
        /// <param name="pos">The Position to test</param>
        public bool ValidPosition(Position pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        /// <summary>
        /// Ticks each component by the specified time in seconds
        /// </summary>
        /// <param name="time">The amount of time ellapsed in seconds since last tick</param>
        public void Tick(double time)
        {
            foreach (Component com in Components) com.Tick(this, time);

            // send/receive XBus messages
            foreach (KeyValuePair<Bus, List<Bus>> entry in Connections)
            {
                if (!(entry.Key is XBus)) continue;

                XBus orig = (XBus)entry.Key;
                if (orig.State == BusState.Writing || orig.State == BusState.ReadingWriting)
                {
                    XBus dest = entry.Value.Cast<XBus>().FirstOrDefault(
                        bus => bus.State == BusState.Reading || bus.State == BusState.ReadingWriting);
                    if (dest == null) continue;

                    dest.Value = orig.Value;
                    dest.State = BusState.ReadComplete;
                    orig.State = BusState.WriteComplete;
                }
            }
        }

        /// <summary>
        /// Initialize each component and repopulate connections dictionary. Call this before starting simulation
        /// </summary>
        public void Initialize()
        {
            Connections.Clear();

            foreach (Component com in Components)
            {
                com.Initialize();

                // populate connections dictionary
                foreach (Bus bus in com.EnumerateBuses)
                    Connections.Add(bus, GetConnectedBuses(GetCableLine(bus.GetConnectedCable(this, com)))
                        .Where(b => b != bus).ToList());
            }
        }
        /// <summary>
        /// Resets each component. Call this after finishing a simulation
        /// </summary>
        public void Reset()
        {
            foreach (Component com in Components) com.Reset();
        }

        /// <summary>
        /// The default extension for BreadBoard simulation files
        /// </summary>
        public const string FileExtension = ".bbd";
        /// <summary>
        /// The default file filter for BreadBoard simulation files
        /// </summary>
        public const string FileFilter = "BreadBoard Files (.bbd)|*.bbd";

        /// <summary>
        /// The settings to use for serializing a BreadBoard Board class
        /// </summary>
        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings()
        {
            Indent = true,
            NewLineHandling = NewLineHandling.Entitize,
            CloseOutput = true
        };
        
        /// <summary>
        /// Saves this BreadBoard Board to the specified file. Can throw.
        /// </summary>
        /// <param name="file">The FileInfo object of the save destination</param>
        public void Save(FileInfo file)
        {
            XmlSerializer xml = new XmlSerializer(typeof(Board));
            using (XmlWriter writer = XmlWriter.Create(file.FullName, XmlSettings))
                xml.Serialize(writer, this);
        }
        /// <summary>
        /// Reads the contents of a file and returns the contained BreadBoard Board. Can throw
        /// </summary>
        /// <param name="file">The FileInfo object of the file to read</param>
        public static Board Load(FileInfo file)
        {
            if (!File.Exists(file.FullName)) return null;

            Board board = null;
            XmlSerializer xml = new XmlSerializer(typeof(Board));
            using (FileStream f = file.OpenRead())
                board = xml.Deserialize(f) as Board;
            
            if (board == null) return null;
            Board res = new Board(board.Width, board.Height);
            board.Transpose(res);
            return res;
        }

        /// <summary>
        /// Copies the contents of this Board into the specified Board, ensuring all placement rules.
        /// Returns true iff there were no conflicts
        /// </summary>
        /// <param name="board">The Board to copy into</param>
        public bool Transpose(Board board)
        {
            bool perfect = true;

            foreach (Component com in Components)
                if (!board.AddComponent(com)) perfect = false;

            foreach (Bridge bridge in EnumerateBridges)
                if (!board.AddCable(bridge)) perfect = false;
            foreach (Solder solder in EnumerateSolders)
                if (!board.AddCable(solder)) perfect = false;

            return perfect;
        }
    }
}
