using System;

namespace CSharpSynth.Wave.DSP {
	public class SincLowPass
    {
        //--Variables
        private readonly float[] filter;
        private readonly float[,] buffer;
        private readonly int channels;
        private readonly int buffersize;
        //--Public Methods
        public SincLowPass(int channels, int size, double cornerfrequency)
        {
            this.channels = channels;
            this.buffersize = size;
            if (size % 2 != 0) {
				size++;
			}

			double[] filter1 = new double[size];
			this.filter = new float[size];
			this.buffer = new float[channels, size];
            for (int x = 0; x < size; x++) {
				filter1[x] = ApplyBlackmanWindow(Sinc(cornerfrequency, size, x), size, x);
			}

			Normalize(filter1);
            for (int x = 0; x < filter1.Length; x++) {
				this.filter[x] = (float)filter1[x];
			}
		}
		public void ResetFilter() => Array.Clear(this.buffer, 0, this.channels * this.buffersize);
		public void ApplyFilter(float[,] inputBuffer)
        {
            int length = inputBuffer.GetLength(1);
            for (int c = 0; c < this.channels; c++)
            {
                for (int x = 0; x < length; x++)
                {
                    for (int i = 0; i < this.buffer.Length - 1; i++)
                    {
						this.buffer[c, i] = this.buffer[c, i + 1];
                    }
					this.buffer[c, this.buffersize - 1] = inputBuffer[c, x];
                    inputBuffer[c, x] = 0.0f;
                    for (int i = 0; i < this.filter.Length; i++)
                    {
                        inputBuffer[c, x] += this.buffer[c, this.buffersize - (i + 1)] * this.filter[i];
                    }
                }
            }
        }
        public float ApplyFilter(int channel, float sample)
        {
            for (int x = 0; x < this.buffer.Length - 1; x++)
            {
				this.buffer[channel, x] = this.buffer[channel, x + 1];
            }
			this.buffer[channel, this.buffersize - 1] = sample;
            sample = 0.0f;
            for (int x = 0; x < this.filter.Length; x++)
            {
                sample += this.buffer[channel, this.buffersize - (x + 1)] * this.filter[x];
            }
            return sample;
        }
        //--Public Static
        public static float[,] OfflineProcess(int size, double cornerfrequency, float[,] data)
        {
            if (size % 2 != 0) {
				size++;
			}

			double[] filter = new double[size];
            double[] buffer = new double[size];
            for (int x = 0; x < size; x++) {
				filter[x] = ApplyBlackmanWindow(Sinc(cornerfrequency, size, x), size, x);
			}

			Normalize(filter);

            float[,] data2 = new float[data.GetLength(0), data.GetLength(1)];
            for(int c = 0;c< data.GetLength(0);c++)
            {
                Array.Clear(buffer, 0, buffer.Length);
                for(int x = 0;x< data.GetLength(1);x++)
                {
                    double sample = data[c, x];
                    for (int x2 = 0; x2 < buffer.Length - 1; x2++)
                    {
                        buffer[x2] = buffer[x2 + 1];
                    }
                    buffer[buffer.Length - 1] = sample;
                    sample = 0.0;
                    for (int x2 = 0; x2 < filter.Length; x2++)
                    {
                        sample += buffer[buffer.Length - (x2 + 1)] * filter[x2];
                    }
                    data2[c, x] = (float)sample;
                }
            }
            return data2;
        }
        public static double Sinc(double x)
        {
            if (x == 0.0) {
				return 1.0;
			}

			return Math.Sin(x) / x;
        }
        public static double Sinc(double FC, double M, int I)
        {//FC = 0.0 & 0.5 , M  = any even number , I  = 0 to M+1
            if (I - M / 2.0 == 0) {
				return 2.0 * Math.PI * FC;
			}

			return Math.Sin(2.0 * Math.PI * FC * (I - M / 2.0)) / (I - M / 2.0);
        }
		public static double ApplyBlackmanWindow(double sample, double M, int I) => sample * (0.42 - 0.5 * Math.Cos((2 * Math.PI * I) / M) + 0.08 * Math.Cos((4 * Math.PI * I) / M));
		public static void Normalize(double[] data)
        {
            double sum = 0; // 'Normalize the low-pass filter kernel for
            for (int x = 0; x < data.Length; x++)
            {
                sum += data[x];
            }
            for (int x = 0; x < data.Length; x++)
            {
                data[x] = data[x] / sum;
            }
        }
    }
}
