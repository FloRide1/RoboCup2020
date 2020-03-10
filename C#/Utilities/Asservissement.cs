using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class AsservissementPID
    {
        double Kp = 0, Ki = 0, Kd = 0;
        double error, errorT_1;
        double IntegraleErreur = 0;
        double ProportionalLimit = 0;
        double IntegralLimit = 0;
        double DerivationLimit = 0;
        double SampleFreq;

        public AsservissementPID(double fEch, double kp, double ki, double kd, double proportionalLimit, double integralLimit, double derivationLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;

            ProportionalLimit = proportionalLimit;
            IntegralLimit = integralLimit * Kp / Ki;
            DerivationLimit = derivationLimit;

            IntegraleErreur = 0;
            SampleFreq = fEch;
        }

        public void ResetPID(double error)
        {
            IntegraleErreur = 0;
            errorT_1 = error;
        }

        public double CalculatePIDoutput(double error)
        {
            double correctionP = Kp * error;
            correctionP = Toolbox.LimitToInterval(correctionP, -ProportionalLimit, ProportionalLimit);

            IntegraleErreur += error / SampleFreq;
            IntegraleErreur = Toolbox.LimitToInterval(IntegraleErreur, -IntegralLimit / Ki, IntegralLimit / Ki);
            double correctionI = Ki * IntegraleErreur;

            double correctionD = Kd * (error - errorT_1) * SampleFreq;
            errorT_1 = error;
            correctionD = Toolbox.LimitToInterval(correctionD, -DerivationLimit, DerivationLimit);

            return correctionP + correctionI + correctionD;
        }
    }
}
