using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace BreadBoard
{
    /// <summary>
    /// Represents a position on a grid. Overloaded to act as a value type for comparisons
    /// </summary>
    public struct Position
    {
        /// <summary>
        /// The X component of this Position
        /// </summary>
        [XmlAttribute]
        public int X;
        /// <summary>
        /// The Y component of this Position
        /// </summary>
        [XmlAttribute]
        public int Y;

        /// <summary>
        /// Creates a new Position with the specified X and Y components
        /// </summary>
        /// <param name="x">The X component of the new Position</param>
        /// <param name="y">The Y component of the new Position</param>
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the Position above this one
        /// </summary>
        public Position Up
        {
            get { return new Position(X, Y - 1); }
        }
        /// <summary>
        /// Gets the Position below this one
        /// </summary>
        public Position Down
        {
            get { return new Position(X, Y + 1); }
        }
        /// <summary>
        /// Gets the Position to the right of this one
        /// </summary>
        public Position Right
        {
            get { return new Position(X + 1, Y); }
        }
        /// <summary>
        /// Gets the Position to the left of this one
        /// </summary>
        public Position Left
        {
            get { return new Position(X - 1, Y); }
        }

        /// <summary>
        /// The position (0, 0)
        /// </summary>
        public static readonly Position Empty = new Position(0, 0);
        /// <summary>
        /// Represents an invalid position on a positive cartesian grid (-1, -1)
        /// </summary>
        public static readonly Position Invalid = new Position(-1, -1);

        public override string ToString()
        {
            return string.Format("<{0}, {1}>", X, Y);
        }

        /// <summary>
        /// Adds the X and Y components of the specified Positions
        /// </summary>
        /// <param name="a">The first Position</param>
        /// <param name="b">The Position to add</param>
        public static Position operator +(Position a, Position b)
        {
            return new Position(a.X + b.X, a.Y + b.Y);
        }
        /// <summary>
        /// Subtracts the X and Y components of the specified Positions
        /// </summary>
        /// <param name="a">The first Position</param>
        /// <param name="b">The Position to subtract</param>
        public static Position operator -(Position a, Position b)
        {
            return new Position(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Returns true iff the specified Positions have equal X and Y components
        /// </summary>
        /// <param name="a">The first Position</param>
        /// <param name="b">The Position to compare</param>
        public static bool operator ==(Position a, Position b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        /// <summary>
        /// Returns true iff the specified Positions have different X or Y components
        /// </summary>
        /// <param name="a">The first Position</param>
        /// <param name="b">The Position to compare</param>
        public static bool operator !=(Position a, Position b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        /// <summary>
        /// Gets a value that can be used to prove value inequality, but not prove value equality
        /// </summary>
        public override int GetHashCode()
        {
            return 17 * X + 11 * Y;
        }
        /// <summary>
        /// Returns true iff the specified object is a Position and has equal X and Y components to this Position
        /// </summary>
        /// <param name="obj">The object to compare</param>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Position)) return false;
            return this == (Position)obj;
        }

        /// <summary>
        /// Returns true iff the specified position is adjacent to this one
        /// </summary>
        /// <param name="other">The other position to be tested</param>
        public bool Adjacent(Position other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;
        }

        public static implicit operator Point(Position pos)
        {
            return new Point(pos.X, pos.Y);
        }
        public static implicit operator Position(Point point)
        {
            return new Position(point.X, point.Y);
        }

        public static implicit operator Size(Position pos)
        {
            return new Size(pos.X, pos.Y);
        }
        public static implicit operator Position(Size size)
        {
            return new Position(size.Width, size.Height);
        }
    }

    /// <summary>
    /// Represents the orientation of an object on a grid
    /// </summary>
    public enum Direction
    {
        Up, Down, Right, Left
    }

    public static class Extensions
    {
        /// <summary>
        /// Removes the specified characters from the left of the string
        /// </summary>
        /// <param name="str">The string to strip</param>
        /// <param name="chars">The characters to remove</param>
        public static string LStrip(this string str, params char[] chars)
        {
            int start = 0;
            while (start < str.Length && chars.Contains(str[start])) start++;
            return start == str.Length ? string.Empty : str.Substring(start);
        }
        /// <summary>
        /// Removes the specified characters from the right of the string
        /// </summary>
        /// <param name="str">The string to strip</param>
        /// <param name="chars">The characters to remove</param>
        public static string RStrip(this string str, params char[] chars)
        {
            int length = str.Length;
            while (length > 0 && chars.Contains(str[length - 1])) length--;
            return length == 0 ? string.Empty : str.Substring(0, length);
        }
        /// <summary>
        /// Removes the specified characters from the left and right of the string
        /// </summary>
        /// <param name="str">The string to strip</param>
        /// <param name="chars">The characters to remove</param>
        public static string LRStrip(this string str, params char[] chars)
        {
            return str.LStrip(chars).RStrip(chars);
        }

        /// <summary>
        /// Removes all occurrances of the specified characters from the string
        /// </summary>
        /// <param name="str">The string to process</param>
        /// <param name="chars">The characters to remove</param>
        public static string FStrip(this string str, params char[] chars)
        {
            StringBuilder b = new StringBuilder(str.Length);
            foreach (char ch in str) if (!chars.Contains(ch)) b.Append(ch);
            return b.ToString();
        }

        /// <summary>
        /// Clamps the value between the specified min and max values
        /// </summary>
        /// <param name="val">The value to clamp</param>
        /// <param name="min">The minimum value of the result</param>
        /// <param name="max">The maximum value of the result</param>
        /// <exception cref="ArgumentException" />
        public static short Clamp(this short val, short min, short max)
        {
            if (min > max) throw new ArgumentException("Clamp: min must be less than or equal to max");

            return val > max ? max : (val < min ? min : val);
        }
        /// <summary>
        /// Clamps the value between the specified min and max values
        /// </summary>
        /// <param name="val">The value to clamp</param>
        /// <param name="min">The minimum value of the result</param>
        /// <param name="max">The maximum value of the result</param>
        /// <exception cref="ArgumentException" />
        public static int Clamp(this int val, int min, int max)
        {
            if (min > max) throw new ArgumentException("Clamp: min must be less than or equal to max");

            return val > max ? max : (val < min ? min : val);
        }
        /// <summary>
        /// Clamps the value between the specified min and max values
        /// </summary>
        /// <param name="val">The value to clamp</param>
        /// <param name="min">The minimum value of the result</param>
        /// <param name="max">The maximum value of the result</param>
        /// <exception cref="ArgumentException" />
        public static long Clamp(this long val, long min, long max)
        {
            if (min > max) throw new ArgumentException("Clamp: min must be less than or equal to max");

            return val > max ? max : (val < min ? min : val);
        }

        /// <summary>
        /// Clears the specified Image using the specified Color
        /// </summary>
        /// <param name="img">Image to clear</param>
        /// <param name="color">Color to fill</param>
        public static Image Clear(this Image img, Color color)
        {
            Graphics g = Graphics.FromImage(img);
            g.Clear(color);
            g.Dispose();

            return img;
        }

        /// <summary>
        /// Returns true iff this object is equal to any of the other specified objects (uses Equal method)
        /// </summary>
        /// <param name="a">This object</param>
        /// <param name="others">The other objects to compare to</param>
        public static bool IsAny<T>(this T a, params T[] others)
        {
            for (int i = 0; i < others.Length; i++) if (a.Equals(others[i])) return true;
            return false;
        }

        /// <summary>
        /// Returns an array of clones of the specified array of Buses
        /// </summary>
        /// <param name="buses">The array of Buses to be cloned</param>
        public static T[] Clones<T>(this T[] buses) where T : Bus
        {
            T[] res = new T[buses.Length];
            for (int i = 0; i < buses.Length; i++) res[i] = (T)buses[i].Clone();
            return res;
        }
        /// <summary>
        /// Returns an array of clones of the specified array of Registers
        /// </summary>
        /// <param name="registers">The Array of Registers to be cloned</param>
        public static Register[] Clones(this Register[] registers)
        {
            Register[] res = new Register[registers.Length];
            for (int i = 0; i < registers.Length; i++) res[i] = registers[i].Clone();
            return res;
        }
    }

    /// <summary>
    /// Contains static utility methods to ease data access
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Initializes the static data in this class
        /// </summary>
        static Utility()
        {
            DefaultImage = new Bitmap(16, 16);
            Graphics g = Graphics.FromImage(DefaultImage);
            g.Clear(Color.Pink);
            g.Dispose();
        }

        /// <summary>
        /// Converts a Color to a space-separated string of its component rgb values
        /// </summary>
        /// <param name="color">The color to parse</param>
        public static string ColorToString(Color color)
        {
            return string.Format("{0}:{1}:{2}", color.R, color.G, color.B);
        }
        /// <summary>
        /// Converts a string of space-separated rgb values into a Color
        /// </summary>
        /// <param name="str">The string to parse</param>
        public static Color StringToColor(string str)
        {
            string[] subs = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (subs.Length != 3) throw new ArgumentException("Invalid color format");
            byte[] colors = new byte[subs.Length];
            for (int i = 0; i < subs.Length; i++)
                colors[i] = byte.Parse(subs[i]);
            return Color.FromArgb(colors[0], colors[1], colors[2]);
        }

        /// <summary>
        /// A default image that can be used if no other image is specified
        /// </summary>
        public static readonly Image DefaultImage;
        /// <summary>
        /// Contains the pre-cached Images loaded by GetImage (managed by GetImage)
        /// </summary>
        private static Dictionary<string, Image> PrecachedImages = new Dictionary<string, Image>();

        /// <summary>
        /// Loads and returns an image with the specified path.
        /// Will chache loaded images for future requests to the same image
        /// </summary>
        /// <param name="path">The file path of the image to load</param>
        /// <exception cref="FileNotFoundException" />
        public static Image GetImage(string path)
        {
            try
            {
                path = new FileInfo(path).FullName;

                Image res;
                if (PrecachedImages.TryGetValue(path, out res)) return res;

                res = Image.FromFile(path);
                PrecachedImages[path] = res;
                return res;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); return DefaultImage; }
        }
    }
}
