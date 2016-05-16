using System;
using System.IO;

namespace Dpu.Utility
{
	/// <summary>
	/// An object for showing progress using text only.  
	/// This object partially emulates the System.Windows.Forms.ProgressBar class but only
	/// allows for an increasing value as we cannot
	/// </summary>
	public class TextProgressBar
	{
        #region Fields
        private int mMin;
        private int mMax;
        private int mValue;
        private int mStep;
        private int mWidth;
        private int mProgress;
        private TextWriter mOutStream;
        private string nl;
        #endregion

        #region Properties
        public int Minimum
        {
            get { return mMin; }
        }

        public int Maximum
        {
            get { return mMax; }
        }

        public int Value
        {
            get { return mValue; }
            set 
            { 
                if(value < mValue)
                {
                    throw new ArgumentException("Value cannot decrease.");
                }
                if(value != mValue)
                {
                    mValue = value; 
                    UpdateProgress();
                }
            }
        }

        public int Step
        {
            get { return mStep; }
            set 
            { 
                if(value < 1)
                {
                    throw new ArgumentException("The step value must be 1 or greater.");
                }
                mStep = value; 
            }
        }

        public TextWriter OutStream
        {
            get {return this.mOutStream;}
        }
        #endregion

        #region Methods
		public TextProgressBar(int min, int max, int stepValue, int width, TextWriter writer)
		{
            if(min >= max)
            {
                throw new ArgumentException("Max value must be greater than the Min value.");
            }
            if(width < 10)
            {
                throw new ArgumentException("The width must be 10 or greater.");
            }

            mMin = min;
            mMax = max;
            mValue = min;
            Step = stepValue;
            mWidth = width;
            mProgress = 0;
            mOutStream = writer;
            nl = writer.NewLine;
		}

        public virtual void Show()
        {
            Redraw();
        }

        protected virtual void Redraw()
        {
            mOutStream.WriteLine(nl + "v" +new string('-', mWidth-2) + "v");
            UpdateProgress();
        }

        public void PerformStep()
        {
            mValue += mStep;
            if(mValue > this.mMax)
            {
                mValue = mMax;
            }
            UpdateProgress();

            if(mValue == mMax)
            {
                mOutStream.WriteLine("");
            }
        }

        protected virtual void UpdateProgress()
        {
            int progress = (int)(mWidth * (((double)mValue - mMin) / (mMax - mMin)) + .5);
            int display = progress - mProgress;
            while(mProgress < progress)
            {
                mOutStream.Write("*");
                ++mProgress;
                mOutStream.Flush();
            }
        }
        #endregion
    }
}
