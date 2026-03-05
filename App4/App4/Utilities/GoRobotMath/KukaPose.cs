/**
 * KukaPose.cs
 * KUKA robot pozu: X, Y, Z (mm) + A, B, C (derece).
 * GoRobot::KukaPose (Pose<XYZRPY, DEGREES, MILIMETERS>) C# karsiligi.
 *
 * KUKA Euler Konvansiyonu: ZYX intrinsik
 *   A = Rz (Z ekseni etrafinda donme)
 *   B = Ry (Y ekseni etrafinda donme)
 *   C = Rx (X ekseni etrafinda donme)
 *   Rotasyon matrisi: R = Rz(A) * Ry(B) * Rx(C)
 *
 * GoRobot C++ mapping:
 *   pose.rx = KUKA C
 *   pose.ry = KUKA B
 *   pose.rz = KUKA A
 *
 * Referans: GoRobot/GoRobotPose.h, GoRobot/GoRobotPoseUtils.h
 *           GoRobotSamples/RobotDrivers/KukaRobotDriver.cpp (satir 196-201)
 */

using System;

namespace App4.Utilities.GoRobotMath
{
    /// <summary>
    /// KUKA robot pozu: X, Y, Z (mm) + A, B, C (derece).
    /// ZYX Euler konvansiyonu ile 4x4 donusum matrisine cevrilir.
    /// </summary>
    public class KukaPose
    {
        /// <summary>X pozisyonu (mm)</summary>
        public double X { get; set; }

        /// <summary>Y pozisyonu (mm)</summary>
        public double Y { get; set; }

        /// <summary>Z pozisyonu (mm)</summary>
        public double Z { get; set; }

        /// <summary>A acisi (derece) - Z ekseni etrafinda donme (Rz)</summary>
        public double A { get; set; }

        /// <summary>B acisi (derece) - Y ekseni etrafinda donme (Ry)</summary>
        public double B { get; set; }

        /// <summary>C acisi (derece) - X ekseni etrafinda donme (Rx)</summary>
        public double C { get; set; }

        #region Constructors

        /// <summary>
        /// Varsayilan constructor (tum degerler 0).
        /// </summary>
        public KukaPose()
        {
        }

        /// <summary>
        /// KUKA XYZABC degerlerinden olusturur.
        /// </summary>
        /// <param name="x">X pozisyonu (mm)</param>
        /// <param name="y">Y pozisyonu (mm)</param>
        /// <param name="z">Z pozisyonu (mm)</param>
        /// <param name="a">A acisi (derece, Rz)</param>
        /// <param name="b">B acisi (derece, Ry)</param>
        /// <param name="c">C acisi (derece, Rx)</param>
        public KukaPose(double x, double y, double z, double a, double b, double c)
        {
            X = x; Y = y; Z = z;
            A = a; B = b; C = c;
        }

        #endregion

        #region Conversion Methods

        /// <summary>
        /// KUKA XYZABC pozunu 4x4 donusum matrisine cevirir.
        /// Rotasyon: R = Rz(A) * Ry(B) * Rx(C)
        /// </summary>
        public TransformMatrix ToMatrix()
        {
            double aRad = A * Math.PI / 180.0;   // A = Rz
            double bRad = B * Math.PI / 180.0;   // B = Ry
            double cRad = C * Math.PI / 180.0;   // C = Rx

            double ca = Math.Cos(aRad), sa = Math.Sin(aRad);
            double cb = Math.Cos(bRad), sb = Math.Sin(bRad);
            double cc = Math.Cos(cRad), sc = Math.Sin(cRad);

            // R = Rz(A) * Ry(B) * Rx(C)
            var m = new TransformMatrix();

            m.Ix = ca * cb;
            m.Jx = ca * sb * sc - sa * cc;
            m.Kx = ca * sb * cc + sa * sc;

            m.Iy = sa * cb;
            m.Jy = sa * sb * sc + ca * cc;
            m.Ky = sa * sb * cc - ca * sc;

            m.Iz = -sb;
            m.Jz = cb * sc;
            m.Kz = cb * cc;

            m.Tx = X;
            m.Ty = Y;
            m.Tz = Z;

            return m;
        }

        /// <summary>
        /// 4x4 donusum matrisinden KUKA XYZABC pozunu cikarir.
        /// Gimbal lock durumu (B = +/-90 derece) icin ozel islem yapilir.
        /// </summary>
        public static KukaPose FromMatrix(TransformMatrix m)
        {
            var pose = new KukaPose();
            pose.X = m.Tx;
            pose.Y = m.Ty;
            pose.Z = m.Tz;

            // B = atan2(-Iz, sqrt(Ix^2 + Iy^2))
            double sy = Math.Sqrt(m.Ix * m.Ix + m.Iy * m.Iy);
            bool singular = sy < 1e-6;

            if (!singular)
            {
                pose.C = Math.Atan2(m.Jz, m.Kz) * 180.0 / Math.PI;    // C = Rx
                pose.B = Math.Atan2(-m.Iz, sy) * 180.0 / Math.PI;      // B = Ry
                pose.A = Math.Atan2(m.Iy, m.Ix) * 180.0 / Math.PI;     // A = Rz
            }
            else
            {
                // Gimbal lock: B ~ +/-90 derece
                pose.C = Math.Atan2(-m.Ky, m.Jy) * 180.0 / Math.PI;
                pose.B = Math.Atan2(-m.Iz, sy) * 180.0 / Math.PI;
                pose.A = 0;
            }

            return pose;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Robot degiskenlerinden KukaPose olusturur.
        /// </summary>
        public static KukaPose FromRobotVariables(double posX, double posY, double posZ,
            double posA, double posB, double posC)
        {
            return new KukaPose(posX, posY, posZ, posA, posB, posC);
        }

        /// <summary>
        /// Insan okuyabilir format.
        /// </summary>
        public override string ToString()
        {
            return $"KukaPose(X={X:F3}, Y={Y:F3}, Z={Z:F3}, A={A:F3}, B={B:F3}, C={C:F3})";
        }

        /// <summary>
        /// KUKA format: {X 345.000, Y 0.000, Z 220.000, A 180.000, B 0.000, C 0.000}
        /// </summary>
        public string ToKukaString()
        {
            return $"{{X {X:F3}, Y {Y:F3}, Z {Z:F3}, A {A:F3}, B {B:F3}, C {C:F3}}}";
        }

        /// <summary>
        /// Iki pozun yaklasik esitligini kontrol eder.
        /// </summary>
        public bool ApproxEquals(KukaPose other, double posTol = 0.01, double angTol = 0.01)
        {
            if (other == null) return false;
            return Math.Abs(X - other.X) < posTol && Math.Abs(Y - other.Y) < posTol && Math.Abs(Z - other.Z) < posTol
                && Math.Abs(A - other.A) < angTol && Math.Abs(B - other.B) < angTol && Math.Abs(C - other.C) < angTol;
        }

        /// <summary>
        /// 6 elemanli array: [X, Y, Z, A, B, C]
        /// </summary>
        public double[] ToArray()
        {
            return new double[] { X, Y, Z, A, B, C };
        }

        /// <summary>
        /// 6 elemanli array'den olusturur.
        /// </summary>
        public static KukaPose FromArray(double[] data)
        {
            if (data == null || data.Length < 6)
                throw new ArgumentException("En az 6 eleman gerekli.", nameof(data));
            return new KukaPose(data[0], data[1], data[2], data[3], data[4], data[5]);
        }

        #endregion
    }
}
