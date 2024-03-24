using CSharpSynth.Synthesis;
using System;
using System.Linq;

namespace CSharpSynth.Banks.Fm {
  public class Envelope {
    //--Variables
    private double[] timePoints;
    private double[] valuPoints;
    private int arraylength = 0;
    private double maxTime = 0;

    //--Public Properties
    public bool Looping { get; set; } = true;
    public double Peak { get; set; } = 1;
    //--Public Methods
    public Envelope(double[] timePoints, double[] valuPoints) {
      if (timePoints.Length != valuPoints.Length) {
        throw new IndexOutOfRangeException("Envelope params must have matching lengths.");
      }

      this.timePoints = timePoints;
      this.ClampArray(valuPoints);
      this.valuPoints = valuPoints;
      this.arraylength = timePoints.Length;
      this.Sort();
      this.RecalculateMaxTime();
    }
    public Envelope(Func<double, double> function, double time, double start, double end, int size) {
      double[] timePoints = new double[size + 1];
      double[] valuPoints = new double[size + 1];
      decimal delta = (decimal)(time / size);
      decimal start_ = (decimal)start;
      decimal end_ = (decimal)end;
      decimal inc = (end_ - start_) / size;
      decimal x;
      int indexcounter = 0;
      if (start_ < end_) {
        for (x = start_; x <= end_; x += inc) {
          timePoints[indexcounter] = (double)(indexcounter * delta);
          valuPoints[indexcounter] = function.Invoke((double)x);
          indexcounter++;
        }
      } else {
        for (x = start_; x >= end_; x += inc) {
          timePoints[indexcounter] = (double)(indexcounter * delta);
          valuPoints[indexcounter] = function.Invoke((double)x);
          indexcounter++;
        }
      }
      double maxvalue = 0;
      double minvalue = 0;
      //Move function up if parts are negative until its all in the positive.
      for (int x2 = 0; x2 < size + 1; x2++) {
        if (valuPoints[x2] < minvalue) {
          minvalue = valuPoints[x2];
        }
      }
      //Get the biggest element.
      for (int x2 = 0; x2 < size + 1; x2++) {
        valuPoints[x2] = valuPoints[x2] + (minvalue * -1);
        if (valuPoints[x2] > maxvalue) {
          maxvalue = valuPoints[x2];
        }
      }
      //Now scale the values to the time.
      if (maxvalue != 0) {
        for (int x2 = 0; x2 < size + 1; x2++) {
          valuPoints[x2] = Math.Abs((valuPoints[x2] / maxvalue) * time);
        }
      }
      this.timePoints = timePoints;
      this.ClampArray(valuPoints);
      this.valuPoints = valuPoints;
      this.arraylength = timePoints.Length;
      this.Sort();
      this.RecalculateMaxTime();
    }
    public void AddPoint(double time, double value) {
      value = SynthHelper.Clamp(value, 0.0, 1.0);
      if (Array.IndexOf(this.timePoints, time) >= 0) {
        this.Replace(time, value);
      } else {
        if (this.arraylength == this.timePoints.Length) {
          this.Resize();
        }

        this.timePoints[this.arraylength] = time;
        this.valuPoints[this.arraylength] = value;
        this.arraylength++;
        if (time > this.maxTime) {
          this.maxTime = time;
        }
      }
      this.Sort();
    }
    public float DoProcess(double time) {
      if (this.Looping == false) {
        if (time >= this.maxTime) {
          time = this.maxTime;
        }
      } else {
        if (time >= this.maxTime) {
          time %= this.maxTime;
        }
      }
      for (int x = 0; x < this.arraylength; x++) {
        if (this.timePoints[x] > time) {
          double slope = (this.valuPoints[x - 1] - this.valuPoints[x]) / (this.timePoints[x - 1] - this.timePoints[x]);
          double b = this.valuPoints[x] - (slope * this.timePoints[x]);
          return (float)(((slope * time) + b) * this.Peak);
        }
      }
      return 0;
    }
    //--Private Methods
    private bool Replace(double time, double newValue) {
      for (int x = 0; x < this.arraylength; x++) {
        if (this.timePoints[x] == time) {
          this.valuPoints[x] = newValue;
          return true;
        }
      }
      return false;
    }
    private void RecalculateMaxTime() {
      this.maxTime = 0.0;
      for (int x = 0; x < this.arraylength; x++) {
        if (this.timePoints[x] > this.maxTime) {
          this.maxTime = this.timePoints[x];
        }
      }
    }
    private void Resize() {
      const int growth = 5;
      double[] timePoints2 = new double[this.timePoints.Length + growth];
      double[] valuPoints2 = new double[this.valuPoints.Length + growth];
      for (int x = 0; x < this.arraylength; x++) {
        timePoints2[x] = this.timePoints[x];
        valuPoints2[x] = this.valuPoints[x];
      }
      this.valuPoints = valuPoints2;
      this.timePoints = timePoints2;
    }
    private void Sort() {
      /*double[] timePoints2 = new double[this.timePoints.Length];
      double[] valuPoints2 = new double[this.valuPoints.Length];

      double tmp = -1.0;
      int counter = 0;

      for (int y = 0; y < this.arraylength; y++) {
        for (int x = 0; x < this.arraylength; x++) {
          if (tmp < this.timePoints[x]) {
            timePoints2[counter] = this.timePoints[x];
            valuPoints2[counter] = this.valuPoints[x];
            tmp = this.timePoints[x];
            counter++;
            break;
          }
        }
      }
      this.timePoints = timePoints2;
      this.valuPoints = valuPoints2;*/

      (double time, double value)[] a = this.timePoints
        .Zip(this.valuPoints, (x, y) => (x, y))
        .OrderBy(x => x.x)
        .ToArray();
      this.timePoints = a.Select(x => x.time).ToArray();
      this.valuPoints = a.Select(x => x.value).ToArray();
    }
    private void ClampArray(double[] array) {
      for (int x = 0; x < array.Length; x++) {
        array[x] = SynthHelper.Clamp(array[x], 0.0, 1.0);
      }
    }
    //--Static
    public static Envelope CreateBasicFadeIn(double maxTime) => new(new double[] { 0.0, maxTime }, new double[] { 0.0, 1.0 });
    public static Envelope CreateBasicFadeOut(double maxTime) => new(new double[] { 0.0, maxTime }, new double[] { 1.0, 0.0 });
    public static Envelope CreateBasicFadeInAndOut(double fadeInTime, double fadeOutTime) => new(new double[] { 0.0, fadeInTime, fadeInTime + fadeOutTime }, new double[] { 0.0, 1.0, 0.0 });
    public static Envelope CreateBasicConstant() => new(new double[] { 0.0, 1.0 }, new double[] { 1.0, 1.0 });
  }
}
