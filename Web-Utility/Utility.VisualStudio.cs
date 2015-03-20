namespace Utility.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    
    /// <summary>
    /// 
    /// </summary>
    public class WebDebugger : TextWriter
    {
        private bool isOpen;
        private static UnicodeEncoding encoding;
        private readonly int level;
        private readonly string category;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDebugger"/> class.
        /// </summary>
        public WebDebugger()
            : this(0, Debugger.DefaultCategory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDebugger"/> class with the specified level and category.
        /// </summary>
        /// <param name="level">A description of the importance of the messages.</param>
        /// <param name="category">The category of the messages.</param>
        public WebDebugger(int level, string category)
            : this(level, category, CultureInfo.CurrentCulture)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDebugger"/> class with the specified level, category and format provider.
        /// </summary>
        /// <param name="level">A description of the importance of the messages.</param>
        /// <param name="category">The category of the messages.</param>
        /// <param name="formatProvider">An <see cref="IFormatProvider"/> object that controls formatting.</param>
        public WebDebugger(int level, string category, IFormatProvider formatProvider)
            : base(formatProvider)
        {
            this.level = level;
            this.category = category;
            this.isOpen = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            isOpen = false;
            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(char value)
        {
            if (!isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            Debugger.Log(level, category, value.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            if (!isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (value != null)
            {
                Debugger.Log(level, category, value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public override void Write(char[] buffer, int index, int count)
        {
            if (!isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (buffer == null || index < 0 || count < 0 || buffer.Length - index < count)
            {
                base.Write(buffer, index, count); // delegate throw exception to base class
            }
            Debugger.Log(level, category, new string(buffer, index, count));
        }

        /// <summary>
        /// 
        /// </summary>
        public override Encoding Encoding
        {
            get
            {
                if (encoding == null)
                {
                    encoding = new UnicodeEncoding(false, false);
                }
                return encoding;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Level
        {
            get { return level; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Category
        {
            get { return category; }
        }
    }
}