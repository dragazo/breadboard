using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace BreadBoard
{
    public static class Language
    {
        /// <summary>
        /// The Maximum value allowed to be transmitted through an SBus
        /// </summary>
        public const long SBusMaxValue = 255L;

        public static readonly char[] WhiteSpace = { ' ', '\t', '\r' };

        public const char
            LineBreakChar = '\n',

            LabelChar = ':',
            CommentChar = '#',
            CharacterChar = '\'',
            HerePointerChar = '%',

            BinaryChar = 'b',
            OctalChar = 'o',
            DecimalChar = 'd',
            HexChar = 'x',

            LiteralSpaceChar = '_';

        /// <summary>
        /// A pre-processed operation function delegate to use in language interpretation
        /// </summary>
        private static readonly Func<long, long, long>
            ADD = (a, b) => a + b,
            SUB = (a, b) => a - b,
            MUL = (a, b) => a * b,
            DIV = (a, b) => a / b,
            MOD = (a, b) => a % b,
            OR = (a, b) => a | b,
            AND = (a, b) => a & b,
            XOR = (a, b) => a ^ b,
            BSL = (a, b) => a << (int)b,
            BSR = (a, b) => a >> (int)b;

        /// <summary>
        /// A pre-processed comparison fuction delegate to use in language interpretation
        /// </summary>
        private static readonly Func<long, long, bool>
            JEQ = (a, b) => a == b,
            JNE = (a, b) => a != b,
            JGT = (a, b) => a > b,
            JLT = (a, b) => a < b,
            JGE = (a, b) => a >= b,
            JLE = (a, b) => a <= b;

        /// <summary>
        /// Gets the value associated with a specified argument.
        /// Returns true if the microcontroller can advance to the next command.
        /// </summary>
        /// <param name="board">The Board being simulated</param>
        /// <param name="controller">The Microcontroller this command is coming from</param>
        /// <param name="arg">The argument to process</param>
        /// <param name="value">Destination of the resultant value</param>
        /// <exception cref="ArgumentException" />
        public static bool GetSourceValue(Board board, MicroController controller, string arg, out long value)
        {
            Register register = controller.GetRegister(arg);
            Bus bus = controller.GetBus(arg);
            
            value = 0L;
            int valueInt;
            if (register != null) value = register.Value;
            else if (bus != null)
            {
                if (bus is SBus) value = board.GetCableSignal(bus);
                else if (bus is XBus)
                {
                    XBus xbus = (XBus)bus;
                    if (xbus.State == BusState.Idle) xbus.State = BusState.Reading;
                    if (xbus.State != BusState.ReadComplete) return false;

                    value = bus.Value;
                    xbus.State = BusState.Idle;
                }
                else throw new ArgumentException("Unknown bus type");
            }

            // here reference
            else if (arg.Length == 1 && arg[0] == HerePointerChar) value = controller.Line;

            // reference label
            else if (controller.Labels.TryGetValue(arg, out valueInt)) value = valueInt;

            // literal characters
            else if (arg.Length == 3 && arg[0] == CharacterChar && arg[2] == CharacterChar) value = arg[1];

            // literal binary longs
            else if (arg.Length >= 2 && arg[arg.Length - 1] == BinaryChar)
                value = Convert.ToInt64(arg.Substring(0, arg.Length - 1).FStrip(LiteralSpaceChar), 2);

            // literal octal longs
            else if (arg.Length >= 2 && arg[arg.Length - 1] == OctalChar)
                value = Convert.ToInt64(arg.Substring(0, arg.Length - 1).FStrip(LiteralSpaceChar), 8);

            // literal decimal longs
            else if (arg.Length >= 2 && arg[arg.Length - 1] == DecimalChar)
                value = Convert.ToInt64(arg.Substring(0, arg.Length - 1).FStrip(LiteralSpaceChar), 10);

            // literal hexadecimal longs
            else if (arg.Length >= 2 && arg[arg.Length - 1] == HexChar)
                value = Convert.ToInt64(arg.Substring(0, arg.Length - 1).FStrip(LiteralSpaceChar), 16);

            // literal longs
            else if (long.TryParse(arg.FStrip(LiteralSpaceChar), out value)) return true;

            else throw new ArgumentException(string.Format("Failed to convert \"{0}\" to value", arg));

            return true;
        }

        /// <summary>
        /// Handles a line of code from a MicroController
        /// </summary>
        /// <param name="board">The Board being simulated</param>
        /// <param name="controller">The MicroController being processed</param>
        /// <exception cref="ArgumentException" />
        public static void Interpret(Board board, MicroController controller)
        {
            try
            {
                string[] args = controller.Lines[controller.Line];

                switch (args[0])
                {
                    case "mov":
                        if (args.Length != 3) throw new ArgumentException("Usage: mov <source> <destination>");

                        long source_value;
                        if (!GetSourceValue(board, controller, args[1], out source_value)) return;

                        Register dest_register = controller.GetRegister(args[2]);
                        Bus dest_bus = controller.GetBus(args[2]);

                        if (dest_register != null)
                        {
                            dest_register.Value = source_value;

                            controller.Line++;
                            controller.Operations++;
                            return;
                        }
                        else if (dest_bus != null)
                        {
                            if (dest_bus is SBus)
                            {
                                dest_bus.Value = source_value.Clamp(0L, SBusMaxValue);

                                controller.Line++;
                                controller.Operations++;
                                return;
                            }
                            else if (dest_bus is XBus)
                            {
                                XBus xdest_bus = (XBus)dest_bus;
                                if (xdest_bus.State == BusState.Idle)
                                {
                                    xdest_bus.Value = source_value;
                                    xdest_bus.State = BusState.Writing;
                                }
                                if (xdest_bus.State != BusState.WriteComplete) return;

                                xdest_bus.State = BusState.Idle;

                                controller.Line++;
                                controller.Operations++;
                                return;
                            }
                            else throw new Exception("Unknown bus type");
                        }
                        else throw new ArgumentException(
                            string.Format("Could not find register or bus with address \"{0}\"", args[2]));

                    case "add":
                        if (args.Length != 2) throw new ArgumentException("usage: add <value>");

                        InterpretMath(board, controller, args[1], ADD);
                        return;
                    case "sub":
                        if (args.Length != 2) throw new ArgumentException("usage: sub <value>");

                        InterpretMath(board, controller, args[1], SUB);
                        return;

                    case "mul":
                        if (args.Length != 2) throw new ArgumentException("usage: mul <value>");

                        InterpretMath(board, controller, args[1], MUL);
                        return;
                    case "div":
                        if (args.Length != 2) throw new ArgumentException("usage: div <value>");

                        InterpretMath(board, controller, args[1], DIV);
                        return;

                    case "mod":
                        if (args.Length != 2) throw new ArgumentException("usage: mod <value>");

                        InterpretMath(board, controller, args[1], MOD);
                        return;

                    case "or":
                        if (args.Length != 2) throw new ArgumentException("usage: or <value>");

                        InterpretMath(board, controller, args[1], OR);
                        return;
                    case "and":
                        if (args.Length != 2) throw new ArgumentException("usage: and <value>");

                        InterpretMath(board, controller, args[1], AND);
                        return;
                    case "xor":
                        if (args.Length != 2) throw new ArgumentException("usage: xor <value>");

                        InterpretMath(board, controller, args[1], XOR);
                        return;
                    case "not":
                        if (args.Length != 1) throw new ArgumentException("usage: not");

                        controller.Accumulator.Value = ~controller.Accumulator.Value;

                        controller.Line++;
                        controller.Operations++;
                        return;

                    case "bsl":
                        if (args.Length != 2) throw new ArgumentException("usage: bsl <value>");

                        InterpretMath(board, controller, args[1], BSL);
                        return;
                    case "bsr":
                        if (args.Length != 2) throw new ArgumentException("usage: bsr <value>");

                        InterpretMath(board, controller, args[1], BSR);
                        return;

                    case "slp":
                        if (args.Length != 2) throw new ArgumentException("usage: slp <value>");

                        if (!GetSourceValue(board, controller, args[1], out source_value)) return;
                        controller.SleepCycles = source_value;

                        controller.Line++;
                        controller.Operations++;
                        return;
                    case "stop":
                        if (args.Length != 1) throw new ArgumentException("usage: stop");

                        controller.Running = false;

                        controller.Line++;
                        controller.Operations++;
                        return;

                    case "jif":
                        if (args.Length != 3) throw new ArgumentException("usage: jif <value> <line>");

                        InterpretJump(board, controller, args[1], "0", args[2], JNE);
                        break;
                    case "jeq":
                        if (args.Length != 4) throw new ArgumentException("usage: jeq <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JEQ);
                        return;
                    case "jne":
                        if (args.Length != 4) throw new ArgumentException("usage: jne <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JNE);
                        return;

                    case "jgt":
                        if (args.Length != 4) throw new ArgumentException("usage: jgt <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JGT);
                        return;
                    case "jlt":
                        if (args.Length != 4) throw new ArgumentException("usage: jlt <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JLT);
                        return;

                    case "jge":
                        if (args.Length != 4) throw new ArgumentException("usage: jge <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JGE);
                        return;
                    case "jle":
                        if (args.Length != 4) throw new ArgumentException("usage: jle <value> <value> <line>");

                        InterpretJump(board, controller, args[1], args[2], args[3], JLE);
                        return;

                    case "jmp":
                        if (args.Length != 2) throw new ArgumentException("usage: jmp <line>");

                        InterpretJump(board, controller, args[1]);
                        return;
                    default:
                        throw new ArgumentException(string.Format("Unknown command \"{0}\"", args[0]));
                }
            }
            catch (Exception ex)
            {
                controller.Error = true;
                throw new Exception(string.Format("Line {0} - {1}",
                    controller.SourceLines[controller.Line] + 1, ex.Message), ex);
            }
        }

        /// <summary>
        /// Interprets a mathematical operation for the Interpret function
        /// </summary>
        /// <param name="board">The Board being simulated</param>
        /// <param name="controller">The MicroController being processed</param>
        /// <param name="val">The value to use for operation</param>
        /// <param name="operation">The operation to use for calculating the result</param>
        private static void InterpretMath(Board board, MicroController controller, string val,
            Func<long, long, long> operation)
        {
            long value;
            if (!GetSourceValue(board, controller, val, out value)) return;

            controller.Accumulator.Value = operation(controller.Accumulator.Value, value);

            controller.Line++;
            controller.Operations++;
        }

        /// <summary>
        /// Interprets a jump operation for the Interpret function
        /// </summary>
        /// <param name="board">The Board being simulated</param>
        /// <param name="controller">THe MicroController being processed</param>
        /// <param name="a">The first argument</param>
        /// <param name="b">The second argument</param>
        /// <param name="c">The label to jump to if the test returns true</param>
        /// <param name="test">The test to put a and b through to determine if we should jump</param>
        private static void InterpretJump(Board board, MicroController controller, string a, string b, string arg,
            Func<long, long, bool> test)
        {
            long value_a, value_b;
            if (!GetSourceValue(board, controller, a, out value_a)) return;
            if (!GetSourceValue(board, controller, b, out value_b)) return;

            if (test(value_a, value_b)) InterpretJump(board, controller, arg);
            else
            {
                controller.Line++;
                controller.Operations++;
            }
        }
        /// <summary>
        /// Interprets a jump operation for the Interpret function
        /// </summary>
        /// <param name="controller">The MicroController being processed</param>
        /// <param name="label">The label to jump to</param>
        /// <exception cref="ArgumentException" />
        private static void InterpretJump(Board board, MicroController controller, string arg)
        {
            long line;
            if (!GetSourceValue(board, controller, arg, out line)) return;

            controller.Line = (int)line;
            controller.Operations++;
        }

        /// <summary>
        /// Returns true iff the specified label name is a legal BreadBoard label name
        /// </summary>
        /// <param name="label">The label to test</param>
        public static bool IsLegalLabel(string label)
        {
            if ((label[0] < 'a' || label[0] > 'z') && (label[0] < 'A' || label[0] > 'Z')
                && label[0] != '_') return false;

            for (int i = 1; i < label.Length; i++)
                if((label[i] < 'a' || label[i] > 'z') && (label[i] < 'A' || label[i] > 'Z')
                    && (label[i] < '0' || label[i] > '9') && label[i] != '_') return false;

            return true;
        }
    }
    
    public sealed class MicroController : Component
    {
        /// <summary>
        /// The Address of the Accumulatior register - Required for MicroController to function
        /// </summary>
        public const string AccumulatorAddress = "acc";

        private Image Image = Utility.DefaultImage;

        private string __Image = string.Empty;
        [XmlElement("Image")]
        public string _Image
        {
            get { return __Image; }
            set { __Image = value; Image = Utility.GetImage(value); }
        }

        /// <summary>
        /// The Registers contained within this MicroController
        /// </summary>
        public Register[] Registers = { };

        /// <summary>
        /// Gets the register in this MicroController with the specified address
        /// </summary>
        /// <param name="address">The address to find</param>
        public Register GetRegister(string address)
        {
            foreach (Register register in Registers)
                if (register.Address == address) return register;

            return null;
        }

        /// <summary>
        /// Iterates through all the Registers, XBuses, and SBuses (in that order) in this Microcontroller as IDataLocation
        /// </summary>
        public IEnumerable<IDataLocation> EnumerateDataLocations
        {
            get
            {
                for (int i = 0; i < Registers.Length; i++) yield return Registers[i];
                for (int i = 0; i < XBuses.Length; i++) yield return XBuses[i];
                for (int i = 0; i < SBuses.Length; i++) yield return SBuses[i];
            }
        }

        /// <summary>
        /// The raw source code entered by the user
        /// </summary>
        public string Source = string.Empty;
        
        /// <summary>
        /// True iff an error occurred during interpretation - Managed by Interpret
        /// </summary>
        [XmlIgnore]
        public bool Error;

        /// <summary>
        /// The pseudo-compiled code to be processed
        /// </summary>
        [XmlIgnore]
        public List<string[]> Lines = new List<string[]>();

        /// <summary>
        /// Contains the source-code line of a specified compiled line (as indicies)
        /// </summary>
        [XmlIgnore]
        public List<int> SourceLines = new List<int>();

        /// <summary>
        /// The current line being processed
        /// </summary>
        private int _Line;
        /// <summary>
        /// Gets or sets the current line geing processed.
        /// If equal to length of program, wraps to beginning
        /// </summary>
        /// <exception cref="IndexOutOfRangeException" />
        [XmlIgnore]
        public int Line
        {
            get { return _Line; }
            set
            {
                if (value > Lines.Count) throw new IndexOutOfRangeException("Attempt to jump outside program range/");
                _Line = value == Lines.Count ? 0 : value;
            }
        }

        /// <summary>
        /// A dictionary containing key value pairs of labels and the line number for jumping to said label
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, int> Labels = new Dictionary<string, int>();

        /// <summary>
        /// The remaining number of cycles to yield
        /// </summary>
        [XmlIgnore]
        public long SleepCycles;
        /// <summary>
        /// The total number of operations this MicroController has performed
        /// </summary>
        [XmlIgnore]
        public long Operations;

        /// <summary>
        /// A reference to the default Accumulator register of this MicroController
        /// </summary>
        [XmlIgnore]
        public Register Accumulator = null;

        /// <summary>
        /// True iff the MicroController is still running in the simulation
        /// </summary>
        [XmlIgnore]
        public bool Running;

        public MicroController()
        {
            Reset();
        }

        /// <summary>
        /// Allows the user to edit the source code of this MicroController
        /// </summary>
        public override void Edit()
        {
            base.Edit();

            SourceEditor editor = new SourceEditor();
            editor.Source = Source;
            if (editor.ShowDialog() == DialogResult.OK)
                Source = editor.Source;
            editor.Dispose();
        }

        public override void Paint(Graphics g, PointF start, float scale)
        {
            RectangleF clip = GetClip(start, scale);

            // highlight component on error for user debugging convenience
            if (Error) g.FillRectangle(new SolidBrush(Color.Red), clip);
            g.DrawImage(Image, GetClip(start, scale));

            // draw buses - soon to be moved to board user-debugging mode
            foreach (Bus bus in EnumerateBuses) bus.Paint(g, start, scale);
        }

        public override void Tick(Board board, double time)
        {
            base.Tick(board, time);
            if (!Running) return;
            if (SleepCycles > 0) { SleepCycles--; return; }

            Language.Interpret(board, this);
        }
        
        public override void Initialize()
        {
            base.Initialize();

            // ensure no data locations share addresses
            foreach (IDataLocation here in EnumerateDataLocations)
                foreach (IDataLocation there in EnumerateDataLocations)
                {
                    if (here != there && here.GetAddress() == there.GetAddress())
                    {
                        Error = true;
                        throw new Exception(string.Format("Conflicting IDataLocation addresses: \"{0}\"", here.GetAddress()));
                    }
                }

            Labels.Clear();
            Lines.Clear();
            SourceLines.Clear();

            // compile source
            string[] lines = Source.Split(Language.LineBreakChar);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].LRStrip(Language.WhiteSpace);
                if (line == string.Empty || line[0] == Language.CommentChar) continue;

                string[] args = line.Split(Language.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 1 && line[line.Length - 1] == Language.LabelChar)
                {
                    string label = line.Substring(0, line.Length - 1);
                    if (label == string.Empty)
                    {
                        Error = true;
                        throw new ArgumentException(string.Format("Line {0} - empty label detected", i + 1));
                    }
                    if (!Language.IsLegalLabel(label))
                    {
                        Error = true;
                        throw new ArgumentException(string.Format("Line {0} - label \"{1}\" illegal", i + 1, label));
                    }

                    // make sure we don't have laels and registers/buses sharing names
                    foreach (Register register in Registers) if (register.Address == label)
                        {
                            Error = true;
                            throw new ArgumentException(string.Format("Line {0} - label \"{1}\" conflicts with register", i + 1, label));
                        }
                    foreach (Bus bus in EnumerateBuses) if (bus.Address == label)
                        {
                            Error = true;
                            throw new ArgumentException(string.Format("Line {0} - label \"{1}\" conflicts with bus", i + 1, label));
                        }

                    Labels.Add(label, Lines.Count);
                }
                else
                {
                    Lines.Add(args);
                    SourceLines.Add(i);
                }
            }

            // connect accumulator register (saves a look-up operation on math interpretation)
            if (Accumulator == null) Accumulator = GetRegister(AccumulatorAddress);
            
            // only set to running if there is code to run (avoid IndexOutOfRangeException)
            Running = Lines.Count > 0;
        }
        public override void Reset()
        {
            base.Reset();

            Line = 0;

            SleepCycles = 0L;
            Operations = 0L;

            Error = false;

            foreach (Register register in Registers) register.Reset();
        }

        public override Component Clone()
        {
            MicroController res = new MicroController()
            {
                Size = Size,
                _Image = _Image,

                Source = Source,

                Registers = Registers.Clones(),
                XBuses = XBuses.Clones(),
                SBuses = SBuses.Clones()
            };

            return res;
        }
    }
}
