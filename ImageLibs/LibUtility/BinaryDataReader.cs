using System;
using System.Collections.Generic;
using System.Text;

namespace Dpu.Utility
{
    /// <summary>
    /// Reads the binary data file format for classifier learning
    /// 0xBADFEED5L
    /// InputLength (in bytes) for each sample
    /// TArgetLength in bytes)
    /// First trainSample (input floowed by targets as floats
    /// Second trainSample (input floowed by targets as floats
    /// </summary>
    public class BinaryDataReader
    {
        private System.IO.FileStream _stream;
        private System.IO.BinaryReader _br;
        private string _fileName;
        private UInt32 _fileLength;
        private UInt32 _magicNumber;
        private UInt32 _patternLength;
        private UInt32 _targetLength;
        private UInt32 _stride;
        private Single[,] _inputFeats;
        private Int32[] _lables;
        private UInt32 _count;

        public BinaryDataReader(String fileName)
        {
            _fileName = fileName;
        }

        public void Open()
        {
            _stream = new System.IO.FileStream(_fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
            _stream.Seek(0, System.IO.SeekOrigin.Begin);
            _br = new System.IO.BinaryReader(_stream);

        }
        public void Read()
        {
            Open();
            _magicNumber = _br.ReadUInt32();
            if (_magicNumber != 0xBADFEED5L)
                throw new Exception("Bad Magic");
            _fileLength = (UInt32)_stream.Length;
            _patternLength = _br.ReadUInt32() / 4;
            _targetLength = _br.ReadUInt32() / 4;
            _stride = 4 * (_patternLength + _targetLength);

            _count = ((_fileLength - 12) / _stride);
            _inputFeats = new float [_count, _patternLength];
            _lables = new int[_count];

            for(int iSamp = 0 ; iSamp < _count ; ++iSamp)
            {
                for (int iFeat = 0 ; iFeat < _patternLength ; ++iFeat)
                {
                    _inputFeats[iSamp, iFeat] = _br.ReadSingle();
                }

                _lables[iSamp] = (int)_br.ReadSingle();
            }

            _br.Close();
        }

        public UInt32 Count
        {
            get
            {
                return _count;
            }
        }

        public UInt32 FeatureLength
        {
            get
            {
                return _patternLength;
            }
        }

        public float[,] Features
        {
            get
            {
                return _inputFeats;
            }
        }

        public Int32[] Labels
        {
            get
            {
                return _lables;
            }
        }
    }
}
