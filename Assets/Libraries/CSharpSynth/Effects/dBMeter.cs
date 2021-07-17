using System;

namespace CSharpSynth.Effects {
  public class DBMeter : BasicAudioEffect {
    private bool useFastTest = false;
    //--Public Properties
    public float LeftPeak { get; set; } = 1.0f;
    public float RightPeak { get; set; } = 1.0f;
    public float Left_dBLevel { get; private set; } = 0.0f;
    public float Right_dBLevel { get; private set; } = 0.0f;
    public bool UseFastVersion {
      get => this.useFastTest;
      set {
        if (value) {
          this.LeftPeak = 50f;
          this.RightPeak = 50f;
        } else {
          this.LeftPeak = 1f;
          this.RightPeak = 1f;
        }
        this.useFastTest = value;
      }
    }
    //--Public Methods
    public override void DoEffect(float[,] inputBuffer) {
      if (this.useFastTest) {
        this.FastTest(inputBuffer);
      } else {
        this.SlowTest(inputBuffer);
      }
    }
    //--Private Methods
    private void FastTest(float[,] inputBuffer) {
      int channels = inputBuffer.GetLength(0);
      for (int x = 0; x < channels; x++) {
        float dB = inputBuffer[x, 0] * inputBuffer[x, 0];
        if (x == 0) {
          this.Left_dBLevel = Math.Abs((float)(20 * Math.Log10(Math.Sqrt(dB))));
          //if (L_CurrentdB > L_PeakdB && !float.IsInfinity(L_CurrentdB))
          //    L_PeakdB = L_CurrentdB;
        } else {
          this.Right_dBLevel = Math.Abs((float)(20 * Math.Log10(Math.Sqrt(dB))));
          //if (R_CurrentdB > R_PeakdB && !float.IsInfinity(R_CurrentdB))
          //    R_PeakdB = R_CurrentdB;
        }
      }
    }
    private void SlowTest(float[,] inputBuffer) {
      int channels = inputBuffer.GetLength(0);
      int samples = inputBuffer.GetLength(1);
      for (int x = 0; x < channels; x++) {
        float dB = 0.0f;
        for (int y = 0; y < samples; y++) {
          dB += (inputBuffer[x, y] * inputBuffer[x, y]);
        }
        if (x == 0) {
          this.Left_dBLevel = Math.Abs((float)(20 * Math.Log10(Math.Sqrt(dB / samples * 2))));
          if (this.Left_dBLevel > this.LeftPeak && !float.IsInfinity(this.Left_dBLevel)) {
            this.LeftPeak = this.Left_dBLevel;
          }
        } else {
          this.Right_dBLevel = Math.Abs((float)(20 * Math.Log10(Math.Sqrt(dB / samples * 2))));
          if (this.Right_dBLevel > this.RightPeak && !float.IsInfinity(this.Right_dBLevel)) {
            this.RightPeak = this.Right_dBLevel;
          }
        }
      }
      if (channels == 1) {
        this.Right_dBLevel = this.Left_dBLevel;
        this.RightPeak = this.LeftPeak;
      }
    }
  }
}
