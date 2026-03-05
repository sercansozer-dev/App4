/**
 * HandEyeCalibration.cs
 * Tsai-Lenz (1989) AX=XB el-goz kalibrasyonu cozucu.
 * GoRobot::CalibEyeOnHandSystem ve GoRobot::LocateEyeOnHandSystem C# karsiligi.
 *
 * Harici math kutuphanesi GEREKTIRMEZ - tum islemler 3x3 analitik cozumlerle yapilir.
 *
 * Referans: GoRobot/GoRobot.h (satir 62-90)
 *           "A New Technique for Fully Autonomous and Efficient 3D Robotics
 *            Hand/Eye Calibration" - Tsai & Lenz, 1989
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace App4.Utilities.GoRobotMath
{
    /// <summary>
    /// Kalibrasyon dogruluk metrikleri.
    /// GoRobot::Accuracy yapisinin C# karsiligi.
    /// </summary>
    public struct CalibrationAccuracy
    {
        /// <summary>Ortalama donusum matrisi</summary>
        public TransformMatrix Average;
        /// <summary>Aci aralik (max - min, derece)</summary>
        public double AngleRangeDeg;
        /// <summary>Aci standart sapma (derece)</summary>
        public double AngleStdDeg;
        /// <summary>Pozisyon aralik (max - min, mm)</summary>
        public double PositionRangeMm;
        /// <summary>Pozisyon standart sapma (mm)</summary>
        public double PositionStdMm;
    }

    /// <summary>
    /// Tsai-Lenz (1989) AX=XB el-goz kalibrasyonu.
    ///
    /// Kullanim:
    /// 1. Robot N farkli pozisyona hareket ettirilir (min 3, onerilir 5+)
    /// 2. Her pozisyonda robot flange pozu ve sensor olcumu kaydedilir
    /// 3. CalibrateEyeOnHand() ile hand-eye matrisi hesaplanir
    /// 4. LocateEyeOnHand() ile sensor olcumleri robot base'e donusturulur
    /// </summary>
    public static class HandEyeCalibration
    {
        #region Public API

        /// <summary>
        /// Eye-on-Hand sistemi kalibre eder (sensor robot koluna monte).
        ///
        /// AX = XB problemini cozer:
        ///   A = robot flange'inin relatif hareketi
        ///   B = sensor tarafindan gozlemlenen hedefin relatif hareketi
        ///   X = sensor-flange donusumu (hand-eye matrisi)
        /// </summary>
        /// <param name="flangeInBase">Robot flange pozlari base koordinat sisteminde (N adet)</param>
        /// <param name="targetInSensor">Hedef pozlari sensor koordinat sisteminde (N adet)</param>
        /// <returns>Hand-eye donusum matrisi (sensor → flange)</returns>
        /// <exception cref="ArgumentException">Yetersiz poz sayisi veya boyut uyumsuzlugu</exception>
        public static TransformMatrix CalibrateEyeOnHand(
            TransformMatrix[] flangeInBase,
            TransformMatrix[] targetInSensor)
        {
            if (flangeInBase == null || targetInSensor == null)
                throw new ArgumentNullException("Poz dizileri null olamaz.");
            if (flangeInBase.Length != targetInSensor.Length)
                throw new ArgumentException("Flange ve sensor poz dizileri ayni boyutta olmali.");
            if (flangeInBase.Length < 3)
                throw new ArgumentException("En az 3 poz cifti gerekli (4+ onerili).");

            int n = flangeInBase.Length;
            int pairCount = n - 1;

            // --- Adim 1: Relatif hareketleri hesapla ---
            var relA = new TransformMatrix[pairCount];  // Robot relatif hareketi
            var relB = new TransformMatrix[pairCount];  // Sensor relatif hareketi

            for (int i = 0; i < pairCount; i++)
            {
                // A_i = flange[i]^(-1) * flange[i+1]
                relA[i] = flangeInBase[i].Inverse() * flangeInBase[i + 1];
                // B_i = sensor[i] * sensor[i+1]^(-1)
                relB[i] = targetInSensor[i] * targetInSensor[i + 1].Inverse();
            }

            // --- Adim 2: Rotasyon cozumu (Tsai-Lenz) ---
            // Her A_i ve B_i icin eksen-aci temsili bul
            // Modified Rodrigues parametresi: p = 2*sin(theta/2)*v

            // Lineer sistem: skew(Pa + Pb) * Pcx = Pb - Pa
            // M * x = rhs  (3*pairCount x 3 overdetermined sistem)
            double[,] M = new double[3 * pairCount, 3];
            double[] rhs = new double[3 * pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                var (axisA, angleA) = RotationToAxisAngle(relA[i]);
                var (axisB, angleB) = RotationToAxisAngle(relB[i]);

                double[] pa = ModifiedRodriguesParam(angleA, axisA);
                double[] pb = ModifiedRodriguesParam(angleB, axisB);

                // skew(pa + pb) * x = pb - pa
                double[] sum = { pa[0] + pb[0], pa[1] + pb[1], pa[2] + pb[2] };
                double[] diff = { pb[0] - pa[0], pb[1] - pa[1], pb[2] - pa[2] };

                double[,] skew = SkewSymmetric(sum);

                for (int j = 0; j < 3; j++)
                {
                    M[i * 3 + j, 0] = skew[j, 0];
                    M[i * 3 + j, 1] = skew[j, 1];
                    M[i * 3 + j, 2] = skew[j, 2];
                    rhs[i * 3 + j] = diff[j];
                }
            }

            // Least-squares cozum: x = (M^T * M)^(-1) * M^T * rhs
            double[] pcxPrime = SolveLeastSquares(M, rhs, 3 * pairCount, 3);

            // --- Adim 3: Pcx' → Rotasyon matrisi ---
            // pcxPrime, Tsai-Lenz'in "half" parametresi: ||pcxPrime|| = tan(theta_x / 2)
            // Dogrudan pcxPrime'dan aci ve eksen cikarilir (ara pcx hesabi gereksiz).
            double pcxNorm = Math.Sqrt(pcxPrime[0] * pcxPrime[0] + pcxPrime[1] * pcxPrime[1] + pcxPrime[2] * pcxPrime[2]);

            double thetaX;
            double[] axisX;

            if (pcxNorm < 1e-12)
            {
                thetaX = 0;
                axisX = new double[] { 0, 0, 1 };
            }
            else
            {
                // ||pcxPrime|| = tan(theta_x / 2)  →  theta_x = 2 * atan(||pcxPrime||)
                thetaX = 2.0 * Math.Atan(pcxNorm);
                axisX = new double[] { pcxPrime[0] / pcxNorm, pcxPrime[1] / pcxNorm, pcxPrime[2] / pcxNorm };
            }

            double[,] Rx = RodriguesToRotation(axisX, thetaX);

            // --- Adim 4: Translasyon cozumu ---
            // (Ra_i - I) * tx = Rx * tb_i - ta_i
            double[,] Mt = new double[3 * pairCount, 3];
            double[] rhsT = new double[3 * pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                double[,] Ra = relA[i].GetRotation3x3();
                double[] ta = relA[i].GetTranslation();
                double[] tb = relB[i].GetTranslation();

                // Rx * tb
                double[] Rxtb = MultiplyMatVec3(Rx, tb);

                for (int j = 0; j < 3; j++)
                {
                    Mt[i * 3 + j, 0] = Ra[j, 0] - (j == 0 ? 1 : 0);
                    Mt[i * 3 + j, 1] = Ra[j, 1] - (j == 1 ? 1 : 0);
                    Mt[i * 3 + j, 2] = Ra[j, 2] - (j == 2 ? 1 : 0);
                    rhsT[i * 3 + j] = Rxtb[j] - ta[j];
                }
            }

            double[] tx_vec = SolveLeastSquares(Mt, rhsT, 3 * pairCount, 3);

            // --- Adim 5: Hand-eye matrisi olustur ---
            var result = new TransformMatrix();
            result.SetRotation3x3(Rx);
            result.SetTranslation(tx_vec[0], tx_vec[1], tx_vec[2]);

            return result;
        }

        /// <summary>
        /// Sensor koordinatlarini robot base koordinatlarina donusturur.
        /// T_base_target = T_base_flange * handEye * T_sensor_target
        /// </summary>
        /// <param name="flangeInBase">Robot flange pozu base'de</param>
        /// <param name="targetInSensor">Hedef pozu sensor'de</param>
        /// <param name="handEye">Kalibre edilmis hand-eye matrisi</param>
        /// <returns>Hedefin base koordinat sistemindeki pozu</returns>
        public static TransformMatrix LocateEyeOnHand(
            TransformMatrix flangeInBase,
            TransformMatrix targetInSensor,
            TransformMatrix handEye)
        {
            return flangeInBase * handEye * targetInSensor;
        }

        /// <summary>
        /// Toplu konum donusumu: birden fazla sensor olcumunu base'e donusturur.
        /// </summary>
        public static TransformMatrix[] LocateEyeOnHandBatch(
            TransformMatrix[] flangeInBase,
            TransformMatrix[] targetInSensor,
            TransformMatrix handEye)
        {
            if (flangeInBase.Length != targetInSensor.Length)
                throw new ArgumentException("Dizi boyutlari esit olmali.");

            var results = new TransformMatrix[flangeInBase.Length];
            for (int i = 0; i < flangeInBase.Length; i++)
            {
                results[i] = LocateEyeOnHand(flangeInBase[i], targetInSensor[i], handEye);
            }
            return results;
        }

        /// <summary>
        /// Kalibrasyon dogrulugunu olcer.
        /// Locate sonuclari (target in base) arasindaki tutarlilik analiz edilir.
        /// </summary>
        public static CalibrationAccuracy MeasureAccuracy(TransformMatrix[] targetInBase)
        {
            if (targetInBase == null || targetInBase.Length < 2)
                return new CalibrationAccuracy();

            int n = targetInBase.Length;

            // Pozisyon istatistikleri
            double[] posX = targetInBase.Select(m => m.Tx).ToArray();
            double[] posY = targetInBase.Select(m => m.Ty).ToArray();
            double[] posZ = targetInBase.Select(m => m.Tz).ToArray();

            double[] distances = new double[n];
            double avgX = posX.Average(), avgY = posY.Average(), avgZ = posZ.Average();
            for (int i = 0; i < n; i++)
            {
                double dx = posX[i] - avgX;
                double dy = posY[i] - avgY;
                double dz = posZ[i] - avgZ;
                distances[i] = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            double posRange = distances.Max() - distances.Min();
            double posStd = StdDev(distances);

            // Aci istatistikleri
            double[] angles = new double[n];
            for (int i = 0; i < n; i++)
            {
                var (_, angle) = RotationToAxisAngle(targetInBase[i]);
                angles[i] = angle * 180.0 / Math.PI;
            }
            double angleRange = angles.Max() - angles.Min();
            double angleStd = StdDev(angles);

            // Ortalama matris
            var avg = new TransformMatrix();
            avg.Tx = avgX; avg.Ty = avgY; avg.Tz = avgZ;
            // Rotasyon ortalamasi icin ilk matrisi kullan (basitlestirme)
            avg.SetRotation3x3(targetInBase[0].GetRotation3x3());

            return new CalibrationAccuracy
            {
                Average = avg,
                AngleRangeDeg = angleRange,
                AngleStdDeg = angleStd,
                PositionRangeMm = posRange,
                PositionStdMm = posStd
            };
        }

        /// <summary>
        /// Kubbe seklinde kalibrasyon pozisyonlari uretir.
        /// GoRobot::EyeOnHandCalibrationPoses karsiligi.
        /// Ilk poz baslangic pozisyonudur, digerler maxAngle cone icinde dagitilir.
        /// </summary>
        /// <param name="poseCount">Toplam poz sayisi</param>
        /// <param name="maxAngleDeg">Maksimum sapma acisi (derece)</param>
        /// <param name="initialPose">Baslangic pozu (merkez, asagi bakan)</param>
        /// <returns>Kalibrasyon pozisyonlari dizisi</returns>
        public static TransformMatrix[] GenerateCalibrationPoses(
            int poseCount, double maxAngleDeg, TransformMatrix initialPose)
        {
            if (poseCount < 1)
                throw new ArgumentException("En az 1 poz gerekli.", nameof(poseCount));

            var poses = new TransformMatrix[poseCount];
            poses[0] = new TransformMatrix(initialPose); // Ilk poz = baslangic

            if (poseCount == 1) return poses;

            for (int i = 1; i < poseCount; i++)
            {
                // Azimut acisini esit dagit
                double azimuth = 2.0 * Math.PI * (i - 1) / (poseCount - 1);

                // XY duzleminde dondurme ekseni
                double axX = Math.Cos(azimuth);
                double axY = Math.Sin(azimuth);
                double axZ = 0;

                // Baslangic pozuna tilt uygula
                poses[i] = initialPose.Rotate(maxAngleDeg, axX, axY, axZ);
            }

            return poses;
        }

        #endregion

        #region Internal Math Helpers

        /// <summary>
        /// Rotasyon matrisinden eksen-aci temsiline donusturur.
        /// </summary>
        internal static (double[] axis, double angle) RotationToAxisAngle(TransformMatrix m)
        {
            double[,] R = m.GetRotation3x3();
            return RotationToAxisAngle3x3(R);
        }

        /// <summary>
        /// 3x3 rotasyon matrisinden eksen-aci temsiline donusturur.
        /// </summary>
        internal static (double[] axis, double angle) RotationToAxisAngle3x3(double[,] R)
        {
            // theta = acos((trace(R) - 1) / 2)
            double trace = R[0, 0] + R[1, 1] + R[2, 2];
            double cosTheta = (trace - 1.0) / 2.0;
            cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta)); // Clamp [-1, 1]
            double theta = Math.Acos(cosTheta);

            double[] axis;

            if (Math.Abs(theta) < 1e-10)
            {
                // Donme yok - keyfi eksen
                axis = new double[] { 0, 0, 1 };
                return (axis, 0);
            }
            else if (Math.Abs(theta - Math.PI) < 1e-6)
            {
                // 180 derece donme - ozel durum
                // Ekseni rotasyon matrisinin kolon vektorlerinden bul
                double xx = (R[0, 0] + 1) / 2.0;
                double yy = (R[1, 1] + 1) / 2.0;
                double zz = (R[2, 2] + 1) / 2.0;

                if (xx > yy && xx > zz)
                {
                    double x = Math.Sqrt(xx);
                    axis = new double[] { x, R[0, 1] / (2 * x), R[0, 2] / (2 * x) };
                }
                else if (yy > zz)
                {
                    double y = Math.Sqrt(yy);
                    axis = new double[] { R[0, 1] / (2 * y), y, R[1, 2] / (2 * y) };
                }
                else
                {
                    double z = Math.Sqrt(zz);
                    axis = new double[] { R[0, 2] / (2 * z), R[1, 2] / (2 * z), z };
                }

                return (axis, theta);
            }
            else
            {
                // Normal durum: v = [R32-R23, R13-R31, R21-R12] / (2*sin(theta))
                double s = 2.0 * Math.Sin(theta);
                axis = new double[]
                {
                    (R[2, 1] - R[1, 2]) / s,
                    (R[0, 2] - R[2, 0]) / s,
                    (R[1, 0] - R[0, 1]) / s
                };

                // Normalize et
                double len = Math.Sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);
                if (len > 1e-12)
                {
                    axis[0] /= len;
                    axis[1] /= len;
                    axis[2] /= len;
                }

                return (axis, theta);
            }
        }

        /// <summary>
        /// Modified Rodrigues parametresi: p = 2 * sin(theta/2) * v
        /// Tsai-Lenz parametrelendirmesi.
        /// </summary>
        internal static double[] ModifiedRodriguesParam(double angle, double[] axis)
        {
            double s = 2.0 * Math.Sin(angle / 2.0);
            return new double[] { s * axis[0], s * axis[1], s * axis[2] };
        }

        /// <summary>
        /// 3-vektorden 3x3 skew-symmetric matris olusturur.
        /// skew(v) = [ 0, -vz, vy; vz, 0, -vx; -vy, vx, 0 ]
        /// </summary>
        internal static double[,] SkewSymmetric(double[] v)
        {
            return new double[,]
            {
                {     0, -v[2],  v[1] },
                {  v[2],     0, -v[0] },
                { -v[1],  v[0],     0 }
            };
        }

        /// <summary>
        /// Eksen-aci temsilinden 3x3 rotasyon matrisi olusturur (Rodrigues formulu).
        /// R = I + sin(theta)*K + (1-cos(theta))*K^2
        /// K = skew(axis)
        /// </summary>
        internal static double[,] RodriguesToRotation(double[] axis, double angle)
        {
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            double t = 1.0 - c;

            double x = axis[0], y = axis[1], z = axis[2];

            return new double[,]
            {
                { t*x*x + c,   t*x*y - s*z, t*x*z + s*y },
                { t*x*y + s*z, t*y*y + c,   t*y*z - s*x },
                { t*x*z - s*y, t*y*z + s*x, t*z*z + c   }
            };
        }

        /// <summary>
        /// Overdetermined lineer sistem cozer: M * x = b
        /// Normal denklemler: x = (M^T * M)^(-1) * M^T * b
        /// M: (rows x cols), x: (cols), b: (rows)
        /// </summary>
        internal static double[] SolveLeastSquares(double[,] M, double[] b, int rows, int cols)
        {
            // M^T * M (cols x cols)
            double[,] MtM = new double[cols, cols];
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < rows; k++)
                        sum += M[k, i] * M[k, j];
                    MtM[i, j] = sum;
                }

            // M^T * b (cols)
            double[] Mtb = new double[cols];
            for (int i = 0; i < cols; i++)
            {
                double sum = 0;
                for (int k = 0; k < rows; k++)
                    sum += M[k, i] * b[k];
                Mtb[i] = sum;
            }

            // (M^T * M)^(-1) * (M^T * b) — 3x3 icin analitik ters
            if (cols == 3)
            {
                double[,] inv = Invert3x3(MtM);
                return MultiplyMatVec3(inv, Mtb);
            }
            else
            {
                // Genel durum: Gauss eliminasyon
                return SolveGauss(MtM, Mtb, cols);
            }
        }

        /// <summary>
        /// 3x3 matrisin analitik tersi (kofaktor / determinant).
        /// </summary>
        internal static double[,] Invert3x3(double[,] m)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], k = m[2, 2];

            double det = a * (e * k - f * h) - b * (d * k - f * g) + c * (d * h - e * g);

            if (Math.Abs(det) < 1e-15)
                throw new InvalidOperationException("Matris tekil (singular) - kalibrasyon pozlari dejenere olabilir.");

            double invDet = 1.0 / det;

            return new double[,]
            {
                { (e*k - f*h) * invDet, (c*h - b*k) * invDet, (b*f - c*e) * invDet },
                { (f*g - d*k) * invDet, (a*k - c*g) * invDet, (c*d - a*f) * invDet },
                { (d*h - e*g) * invDet, (b*g - a*h) * invDet, (a*e - b*d) * invDet }
            };
        }

        /// <summary>
        /// 3x3 matris * 3-vektor carpimi.
        /// </summary>
        internal static double[] MultiplyMatVec3(double[,] m, double[] v)
        {
            return new double[]
            {
                m[0, 0] * v[0] + m[0, 1] * v[1] + m[0, 2] * v[2],
                m[1, 0] * v[0] + m[1, 1] * v[1] + m[1, 2] * v[2],
                m[2, 0] * v[0] + m[2, 1] * v[1] + m[2, 2] * v[2]
            };
        }

        /// <summary>
        /// Gauss eliminasyonu ile Ax = b cozer (n x n).
        /// </summary>
        internal static double[] SolveGauss(double[,] A, double[] b, int n)
        {
            // Augmented matris olustur
            double[,] aug = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    aug[i, j] = A[i, j];
                aug[i, n] = b[i];
            }

            // Forward elimination with partial pivoting
            for (int col = 0; col < n; col++)
            {
                // Pivot sec
                int maxRow = col;
                double maxVal = Math.Abs(aug[col, col]);
                for (int row = col + 1; row < n; row++)
                {
                    if (Math.Abs(aug[row, col]) > maxVal)
                    {
                        maxVal = Math.Abs(aug[row, col]);
                        maxRow = row;
                    }
                }

                // Satir degistir
                if (maxRow != col)
                {
                    for (int j = 0; j <= n; j++)
                    {
                        double tmp = aug[col, j];
                        aug[col, j] = aug[maxRow, j];
                        aug[maxRow, j] = tmp;
                    }
                }

                if (Math.Abs(aug[col, col]) < 1e-15)
                    throw new InvalidOperationException("Tekil matris - Gauss eliminasyon basarisiz.");

                // Eliminasyon
                for (int row = col + 1; row < n; row++)
                {
                    double factor = aug[row, col] / aug[col, col];
                    for (int j = col; j <= n; j++)
                        aug[row, j] -= factor * aug[col, j];
                }
            }

            // Back substitution
            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = aug[i, n];
                for (int j = i + 1; j < n; j++)
                    x[i] -= aug[i, j] * x[j];
                x[i] /= aug[i, i];
            }

            return x;
        }

        /// <summary>
        /// Standart sapma hesaplar.
        /// </summary>
        internal static double StdDev(double[] values)
        {
            if (values.Length < 2) return 0;
            double mean = values.Average();
            double sumSq = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSq / (values.Length - 1));
        }

        #endregion
    }
}
